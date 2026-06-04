using PersonalTools.PEAnalyzer.Parsers;
using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器图标信息解析模块
    /// 专门负责解析PE文件中的图标资源
    /// </summary>
    internal static class PEResourceParserIcon
    {
        /// <summary>
        /// 解析图标信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        internal static void ParseIconInfo(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            try
            {
                // 图标信息在资源目录中，数据目录索引为#2 (IMAGE_DIRECTORY_ENTRY_RESOURCE)

                if (peInfo.OptionalHeader.DataDirectory.Length > PEConstants.DirectoryResource &&
                    peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryResource].VirtualAddress != 0)
                {
                    uint resourceRVA = peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryResource].VirtualAddress;
                    long resourceOffset = Utilities.RvaToOffset(resourceRVA, peInfo.SectionHeaders);

                    if (resourceOffset != -1 && resourceOffset < fs.Length)
                    {
                        // 解析资源目录以找到图标信息
                        ParseResourceDirectoryForIcons(fs, reader, peInfo, resourceOffset);
                    }
                }
                else if (peInfo.CLRInfo != null)
                {
                    // 对于.NET程序集，尝试从资源中查找图标
                    ParseDotNetIcons(fs, reader, peInfo);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"图标信息解析错误: {ex.Message}");
                // 图标信息解析错误不中断程序执行
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"图标信息解析错误: {ex.Message}");
                // 图标信息解析错误不中断程序执行
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($"图标信息解析错误: {ex.Message}");
                // 图标信息解析错误不中断程序执行
            }
        }

        /// <summary>
        /// 解析.NET程序集中的图标
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        private static void ParseDotNetIcons(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            try
            {
                // 检查CLR资源数据目录
                if (peInfo.CLRInfo != null && peInfo.CLRInfo.HasResources)
                {
                    // 尝试解析.NET资源中的图标
                    ParseDotNetResourcesForIcons(fs, reader, peInfo);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($".NET图标解析错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($".NET图标解析错误: {ex.Message}");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($".NET图标解析错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析.NET资源中的图标
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        private static void ParseDotNetResourcesForIcons(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            try
            {
                // 获取资源RVA并转换为文件偏移
                if (peInfo.OptionalHeader.DataDirectory.Length <= PEConstants.DirectoryResource ||
                    peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryResource].VirtualAddress == 0)
                {
                    return;
                }

                uint resourceRVA = peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryResource].VirtualAddress;
                long resourceOffset = Utilities.RvaToOffset(resourceRVA, peInfo.SectionHeaders);

                if (resourceOffset != -1 && resourceOffset < fs.Length)
                {
                    // 尝试查找命名资源（WPF程序通常将图标存储为命名资源）
                    PEResourceParserIconNamed.ParseResourceDirectoryForNamedIcons(fs, reader, peInfo, resourceOffset);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($".NET资源图标解析错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($".NET资源图标解析错误: {ex.Message}");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($".NET资源图标解析错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析资源目录以查找图标信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="resourceOffset">资源节偏移</param>
        private static void ParseResourceDirectoryForIcons(FileStream fs, BinaryReader reader, PEInfo peInfo, long resourceOffset)
        {
            try
            {
                if (resourceOffset < 0 || resourceOffset + ResourceDirectoryReader.DirectoryHeaderSize > fs.Length)
                {
                    return;
                }

                long originalPosition = fs.Position;
                fs.Position = resourceOffset;

                IMAGERESOURCEDIRECTORY rootDirectory = ResourceDirectoryReader.ReadDirectory(reader);
                int totalEntries = rootDirectory.NumberOfNamedEntries + rootDirectory.NumberOfIdEntries;

                // 优先查找 RT_GROUP_ICON (ID=14)
                bool foundGroupIcon = ScanTypeEntries(fs, reader, resourceOffset, totalEntries, 14,
                    nextLevelOffset => PEResourceParserIconGroup.ParseGroupIconResource(fs, reader, peInfo, nextLevelOffset, resourceOffset));

                // 未找到时回退到 RT_ICON (ID=3) 与命名资源
                if (!foundGroupIcon)
                {
                    ScanTypeEntries(fs, reader, resourceOffset, totalEntries, 3,
                        nextLevelOffset => PEResourceParserIconDirect.ParseDirectIconResource(fs, reader, peInfo, nextLevelOffset, resourceOffset));

                    ScanNamedEntries(fs, reader, resourceOffset, rootDirectory.NumberOfNamedEntries,
                        nextLevelOffset => PEResourceParserIconNamed.ParseResourceDirectoryForNamedIcons(fs, reader, peInfo, nextLevelOffset));
                }

                fs.Position = originalPosition;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"解析资源目录以查找图标信息错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"解析资源目录以查找图标信息错误: {ex.Message}");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($"解析资源目录以查找图标信息错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 扫描根目录中指定类型ID的资源条目，命中即对其下一级偏移回调；返回是否命中过。
        /// </summary>
        private static bool ScanTypeEntries(FileStream fs, BinaryReader reader, long resourceOffset, int totalEntries, uint typeId, Action<long> onMatch)
        {
            bool found = false;
            for (int i = 0; i < totalEntries; i++)
            {
                if (!ResourceDirectoryReader.TryReadEntry(fs, reader, resourceOffset, i, out IMAGERESOURCEDIRECTORYENTRY entry))
                {
                    break;
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
            }

            return found;
        }

        /// <summary>
        /// 扫描根目录中的命名资源条目（最高位为1），对其下一级偏移回调。
        /// </summary>
        private static void ScanNamedEntries(FileStream fs, BinaryReader reader, long resourceOffset, int namedEntries, Action<long> onMatch)
        {
            for (int i = 0; i < namedEntries; i++)
            {
                if (!ResourceDirectoryReader.TryReadEntry(fs, reader, resourceOffset, i, out IMAGERESOURCEDIRECTORYENTRY entry))
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
    }
}