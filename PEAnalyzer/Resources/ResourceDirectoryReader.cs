using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// 资源目录（IMAGE_RESOURCE_DIRECTORY / _ENTRY）的共享读取原语。
    /// 供图标与版本资源解析器复用，避免各处重复手写目录遍历逻辑。
    /// </summary>
    internal static class ResourceDirectoryReader
    {
        /// <summary>资源目录头大小（字节）。</summary>
        public const int DirectoryHeaderSize = 16;

        /// <summary>资源目录项大小（字节）。</summary>
        public const int DirectoryEntrySize = 8;

        /// <summary>资源目录树最大递归深度（正常 PE 仅 3 层：类型/名称/语言）。</summary>
        private const int MaxDirectoryDepth = 32;

        // 当前遍历路径上的目录偏移集合与深度，用于防止畸形/循环资源树导致的无限递归与 StackOverflow。
        // 所有递归都经由 WalkEntries 分派回调，故在此单点防护即可覆盖全部调用方。
        [ThreadStatic] private static HashSet<long>? _walkPath;
        [ThreadStatic] private static int _walkDepth;

        /// <summary>
        /// 从当前位置读取一个 IMAGE_RESOURCE_DIRECTORY（16 字节）。
        /// </summary>
        public static IMAGE_RESOURCE_DIRECTORY ReadDirectory(BinaryReader reader)
        {
            return new IMAGE_RESOURCE_DIRECTORY
            {
                Characteristics = reader.ReadUInt32(),
                TimeDateStamp = reader.ReadUInt32(),
                MajorVersion = reader.ReadUInt16(),
                MinorVersion = reader.ReadUInt16(),
                NumberOfNamedEntries = reader.ReadUInt16(),
                NumberOfIdEntries = reader.ReadUInt16()
            };
        }

        /// <summary>
        /// 读取目录头之后第 <paramref name="index"/> 项目录条目（8 字节）。
        /// 越界返回 false。
        /// </summary>
        public static bool TryReadEntry(FileStream fs, BinaryReader reader, long directoryOffset, int index, out IMAGE_RESOURCE_DIRECTORY_ENTRY entry)
        {
            entry = default;
            long entryOffset = directoryOffset + DirectoryHeaderSize + ((long)index * DirectoryEntrySize);
            if (entryOffset + DirectoryEntrySize > fs.Length)
            {
                return false;
            }

            fs.Position = entryOffset;
            entry = new IMAGE_RESOURCE_DIRECTORY_ENTRY
            {
                NameOrId = reader.ReadUInt32(),
                OffsetToData = reader.ReadUInt32()
            };
            return true;
        }

        /// <summary>
        /// 从当前位置读取一个 IMAGE_RESOURCE_DATA_ENTRY（16 字节）。
        /// </summary>
        public static IMAGE_RESOURCE_DATA_ENTRY ReadDataEntry(BinaryReader reader)
        {
            return new IMAGE_RESOURCE_DATA_ENTRY
            {
                OffsetToData = reader.ReadUInt32(),
                Size = reader.ReadUInt32(),
                CodePage = reader.ReadUInt32(),
                Reserved = reader.ReadUInt32()
            };
        }

        /// <summary>
        /// 判断由 RVA 解析得到的数据偏移与大小是否落在文件范围内、可安全读取。
        /// </summary>
        public static bool IsReadableData(long dataOffset, uint size, FileStream fs)
        {
            return dataOffset >= 0 && size > 0 && size <= int.MaxValue && dataOffset + size <= fs.Length;
        }

        /// <summary>
        /// 遍历目录的所有条目，按"子目录 / 数据条目"分派回调。
        /// 子目录偏移使用低 31 位（相对资源基址），数据条目偏移使用完整 32 位。
        /// 越界由各回调自行校验。
        /// </summary>
        public static void WalkEntries(FileStream fs, BinaryReader reader, long directoryOffset, long resourceBaseOffset, Action<long> onSubdirectory, Action<long> onDataEntry)
        {
            if (directoryOffset < 0 || directoryOffset + DirectoryHeaderSize > fs.Length)
            {
                return;
            }

            // 循环/深度防护：超过最大深度，或该目录偏移已在当前递归路径上（成环）则直接返回。
            _walkPath ??= [];
            if (_walkDepth >= MaxDirectoryDepth || !_walkPath.Add(directoryOffset))
            {
                return;
            }

            _walkDepth++;
            try
            {
                fs.Position = directoryOffset;
                IMAGE_RESOURCE_DIRECTORY directory = ReadDirectory(reader);
                int totalEntries = directory.NumberOfNamedEntries + directory.NumberOfIdEntries;

                for (int i = 0; i < totalEntries; i++)
                {
                    if (!TryReadEntry(fs, reader, directoryOffset, i, out IMAGE_RESOURCE_DIRECTORY_ENTRY entry))
                    {
                        break;
                    }

                    if ((entry.OffsetToData & 0x80000000) != 0)
                    {
                        onSubdirectory(resourceBaseOffset + (entry.OffsetToData & 0x7FFFFFFF));
                    }
                    else
                    {
                        onDataEntry(resourceBaseOffset + entry.OffsetToData);
                    }
                }
            }
            finally
            {
                _walkDepth--;
                if (_walkDepth == 0)
                {
                    _walkPath = null; // 顶层遍历结束，释放路径集合
                }
                else
                {
                    _walkPath.Remove(directoryOffset); // 离开该层，允许兄弟分支合法地再次访问相同偏移(DAG)
                }
            }
        }
        /// <summary>
        /// 扫描根目录中指定类型ID的资源条目，命中即对其下一级(子目录)偏移回调；返回是否命中过。
        /// 偏移取低 31 位（相对资源基址）。下一级越界由各回调自行再次校验。
        /// </summary>
        /// <param name="stopAtFirst">为 true 时命中首个匹配即停止（如版本资源只取第一个 RT_VERSION）。</param>
        public static bool ScanTypeEntries(FileStream fs, BinaryReader reader, long resourceOffset, int totalEntries, uint typeId, Action<long> onMatch, bool stopAtFirst = false)
        {
            bool found = false;
            for (int i = 0; i < totalEntries; i++)
            {
                if (!TryReadEntry(fs, reader, resourceOffset, i, out IMAGE_RESOURCE_DIRECTORY_ENTRY entry))
                {
                    break;
                }

                // 跳过命名条目（最高位为1），仅匹配 ID 类型条目——否则命名条目低16位（名称偏移）恰为 typeId 时会误判
                if ((entry.NameOrId & 0x80000000) != 0)
                {
                    continue;
                }

                if ((entry.NameOrId & 0xFFFF) != typeId)
                {
                    continue;
                }

                long nextLevelOffset = resourceOffset + (entry.OffsetToData & 0x7FFFFFFF);
                if (nextLevelOffset >= 0 && nextLevelOffset < fs.Length)
                {
                    onMatch(nextLevelOffset);
                }

                found = true;
                if (stopAtFirst)
                {
                    break;
                }
            }

            return found;
        }

        /// <summary>
        /// 扫描根目录中的命名资源条目（NameOrId 最高位为1），对其下一级偏移回调。
        /// </summary>
        public static void ScanNamedEntries(FileStream fs, BinaryReader reader, long resourceOffset, int namedEntries, Action<long> onMatch)
        {
            for (int i = 0; i < namedEntries; i++)
            {
                if (!TryReadEntry(fs, reader, resourceOffset, i, out IMAGE_RESOURCE_DIRECTORY_ENTRY entry))
                {
                    break;
                }

                if ((entry.NameOrId & 0x80000000) == 0)
                {
                    continue;
                }

                long nextLevelOffset = resourceOffset + (entry.OffsetToData & 0x7FFFFFFF);
                if (nextLevelOffset >= 0 && nextLevelOffset < fs.Length)
                {
                    onMatch(nextLevelOffset);
                }
            }
        }

        /// <summary>
        /// 临时定位到 <paramref name="offset"/> 执行 <paramref name="read"/>，成功后恢复原流位置并返回其结果。
        /// 若 offset 越界（小于 0 或加上 <paramref name="minBytes"/> 超出文件）直接返回 <paramref name="fallback"/> 且不移动流；
        /// 若 read 抛出可恢复异常，返回 fallback 且不恢复流位置（与各图标查找方法原有语义一致）。
        /// </summary>
        public static T ReadAtOffset<T>(FileStream fs, long offset, int minBytes, T fallback, Func<T> read)
        {
            if (offset < 0 || offset + minBytes > fs.Length)
            {
                return fallback;
            }

            long originalPosition = fs.Position;
            try
            {
                fs.Position = offset;
                T result = read();
                fs.Position = originalPosition;
                return result;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                return fallback;
            }
        }

        /// <summary>
        /// 临时定位到 <paramref name="offset"/> 执行 <paramref name="action"/>，成功后恢复原流位置。
        /// 若 offset 越界（小于 0 或加上 <paramref name="minBytes"/> 超出文件）直接返回且不移动流；
        /// 若 action 抛出可恢复异常，则以 <paramref name="errorContext"/> 记录日志且不恢复流位置
        /// （与各 void 图标解析方法原有 try/catch + Console.WriteLine 语义一致）。
        /// </summary>
        public static void RunAtOffset(FileStream fs, long offset, int minBytes, string errorContext, Action action)
        {
            if (offset < 0 || offset + minBytes > fs.Length)
            {
                return;
            }

            long originalPosition = fs.Position;
            try
            {
                fs.Position = offset;
                action();
                fs.Position = originalPosition;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                Console.WriteLine($"{errorContext}: {ex.Message}");
            }
        }
    }
}
