using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器命名图标资源解析模块
    /// 专门负责解析PE文件中的命名图标资源
    /// </summary>
    internal static class PEResourceParserIconNamed
    {
        /// <summary>
        /// 解析资源目录以查找命名图标资源
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="resourceOffset">资源节偏移</param>
        public static void ParseResourceDirectoryForNamedIcons(FileStream fs, BinaryReader reader, PEInfo peInfo, long resourceOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = resourceOffset;

                // 读取根资源目录
                IMAGERESOURCEDIRECTORY rootDirectory = new()
                {
                    Characteristics = reader.ReadUInt32(),
                    TimeDateStamp = reader.ReadUInt32(),
                    MajorVersion = reader.ReadUInt16(),
                    MinorVersion = reader.ReadUInt16(),
                    NumberOfNamedEntries = reader.ReadUInt16(),
                    NumberOfIdEntries = reader.ReadUInt16()
                };

                // 遍历命名资源项
                for (int i = 0; i < rootDirectory.NumberOfNamedEntries; i++)
                {
                    fs.Position = resourceOffset + 16 + i * 8; // 16是IMAGE_RESOURCE_DIRECTORY大小，每项8字节

                    IMAGERESOURCEDIRECTORYENTRY entry = new()
                    {
                        NameOrId = reader.ReadUInt32(),
                        OffsetToData = reader.ReadUInt32()
                    };

                    // 检查是否是命名资源（最高位为1）
                    if ((entry.NameOrId & 0x80000000) != 0)
                    {
                        long nextLevelOffset = resourceOffset + (entry.OffsetToData & 0x7FFFFFFF);
                        ParseNamedResourceDirectory(fs, reader, peInfo, nextLevelOffset, resourceOffset, entry.NameOrId & 0x7FFFFFFF);
                    }
                }

                fs.Position = originalPosition;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"解析资源目录以查找命名图标错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"解析资源目录以查找命名图标错误: {ex.Message}");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($"解析资源目录以查找命名图标错误: {ex.Message}");
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 解析命名资源目录
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="directoryOffset">目录偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        /// <param name="nameOffset">名称偏移</param>
        private static void ParseNamedResourceDirectory(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset, uint nameOffset)
        {
            try
            {
                long originalPosition = fs.Position;

                // 读取资源名称
                string resourceName = PEResourceParserIconHelpers.ReadResourceName(fs, reader, resourceBaseOffset + nameOffset);

                // 检查资源名称是否可能包含图标（如包含"icon"、".ico"等关键字）
                if (!string.IsNullOrEmpty(resourceName) &&
                    (resourceName.Contains("icon", StringComparison.OrdinalIgnoreCase) ||
                     resourceName.Contains(".ico", StringComparison.OrdinalIgnoreCase) ||
                     resourceName.Contains("app", StringComparison.OrdinalIgnoreCase)))
                {
                    fs.Position = directoryOffset;

                    // 读取资源目录
                    IMAGERESOURCEDIRECTORY directory = new()
                    {
                        Characteristics = reader.ReadUInt32(),
                        TimeDateStamp = reader.ReadUInt32(),
                        MajorVersion = reader.ReadUInt16(),
                        MinorVersion = reader.ReadUInt16(),
                        NumberOfNamedEntries = reader.ReadUInt16(),
                        NumberOfIdEntries = reader.ReadUInt16()
                    };

                    // 遍历子项
                    int totalEntries = directory.NumberOfNamedEntries + directory.NumberOfIdEntries;
                    for (int i = 0; i < totalEntries; i++)
                    {
                        fs.Position = directoryOffset + 16 + i * 8;

                        IMAGERESOURCEDIRECTORYENTRY entry = new()
                        {
                            NameOrId = reader.ReadUInt32(),
                            OffsetToData = reader.ReadUInt32()
                        };

                        // 检查是否是叶子节点
                        if ((entry.OffsetToData & 0x80000000) != 0)
                        {
                            // 继续下一级目录
                            long nextLevelOffset = resourceBaseOffset + (entry.OffsetToData & 0x7FFFFFFF);
                            ParseNamedResourceDirectory(fs, reader, peInfo, nextLevelOffset, resourceBaseOffset, 0);
                        }
                        else
                        {
                            // 处理数据条目
                            long dataEntryOffset = resourceBaseOffset + entry.OffsetToData;
                            ParseNamedResourceDataEntry(fs, reader, peInfo, dataEntryOffset);
                        }
                    }
                }

                fs.Position = originalPosition;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"解析命名资源目录错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"解析命名资源目录错误: {ex.Message}");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($"解析命名资源目录错误: {ex.Message}");
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 解析命名资源数据项
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="dataEntryOffset">数据项偏移</param>
        private static void ParseNamedResourceDataEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = dataEntryOffset;

                // 检查是否有足够的数据读取IMAGE_RESOURCE_DATA_ENTRY
                if (fs.Position + 16 > fs.Length)
                {
                    return;
                }

                // 读取资源数据项
                IMAGERESOURCEDATAENTRY dataEntry = new()
                {
                    OffsetToData = reader.ReadUInt32(),
                    Size = reader.ReadUInt32(),
                    CodePage = reader.ReadUInt32(),
                    Reserved = reader.ReadUInt32()
                };

                // 计算实际数据偏移（注意：资源数据的OffsetToData是RVA）
                long dataOffset = PEResourceParserCore.RvaToOffset(dataEntry.OffsetToData, peInfo.SectionHeaders);
                if (dataOffset != -1 && dataOffset < fs.Length && dataEntry.Size > 0)
                {
                    // 读取资源数据
                    fs.Position = dataOffset;
                    if (dataEntry.Size <= fs.Length && dataOffset + dataEntry.Size <= fs.Length)
                    {
                        byte[] resourceData = reader.ReadBytes((int)dataEntry.Size);

                        // 检查数据是否可能是图标数据
                        if (PEResourceParserIconData.IsIconData(resourceData))
                        {
                            // 处理图标数据
                            PEResourceParserIconData.ProcessIconData(peInfo, resourceData);
                        }
                    }
                }

                fs.Position = originalPosition;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"解析命名资源数据项错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"解析命名资源数据项错误: {ex.Message}");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($"解析命名资源数据项错误: {ex.Message}");
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
            }
        }
    }
}