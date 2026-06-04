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
                if (resourceOffset < 0 || resourceOffset + 16 > fs.Length)
                {
                    return;
                }

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

                int totalEntries = rootDirectory.NumberOfNamedEntries + rootDirectory.NumberOfIdEntries;

                // 遍历资源目录项查找RT_GROUP_ICON资源类型 (ID = 14)
                bool foundGroupIcon = false;
                for (int i = 0; i < totalEntries; i++)
                {
                    long entryOffset = resourceOffset + 16 + i * 8;
                    if (entryOffset + 8 > fs.Length)
                    {
                        break;
                    }

                    fs.Position = entryOffset;
                    IMAGERESOURCEDIRECTORYENTRY entry = new()
                    {
                        NameOrId = reader.ReadUInt32(),
                        OffsetToData = reader.ReadUInt32()
                    };

                    // 检查是否是RT_GROUP_ICON资源类型 (ID = 14)
                    if ((entry.NameOrId & 0xFFFF) == 14) // RT_GROUP_ICON = 14
                    {
                        long nextLevelOffset = resourceOffset + (entry.OffsetToData & 0x7FFFFFFF);
                        if (nextLevelOffset >= 0 && nextLevelOffset < fs.Length)
                        {
                            PEResourceParserIconGroup.ParseGroupIconResource(fs, reader, peInfo, nextLevelOffset, resourceOffset);
                        }
                        foundGroupIcon = true;
                    }
                }

                // 如果没有找到RT_GROUP_ICON，尝试其他方法
                if (!foundGroupIcon)
                {
                    // 尝试查找RT_ICON资源类型 (ID = 3)
                    for (int i = 0; i < totalEntries; i++)
                    {
                        long entryOffset = resourceOffset + 16 + i * 8;
                        if (entryOffset + 8 > fs.Length)
                        {
                            break;
                        }

                        fs.Position = entryOffset;
                        IMAGERESOURCEDIRECTORYENTRY entry = new()
                        {
                            NameOrId = reader.ReadUInt32(),
                            OffsetToData = reader.ReadUInt32()
                        };

                        // 检查是否是RT_ICON资源类型 (ID = 3)
                        if ((entry.NameOrId & 0xFFFF) == 3) // RT_ICON = 3
                        {
                            // 处理直接的RT_ICON资源
                            long nextLevelOffset = resourceOffset + (entry.OffsetToData & 0x7FFFFFFF);
                            if (nextLevelOffset >= 0 && nextLevelOffset < fs.Length)
                            {
                                PEResourceParserIconDirect.ParseDirectIconResource(fs, reader, peInfo, nextLevelOffset, resourceOffset);
                            }
                        }
                    }

                    // 尝试查找命名资源（WPF和其他.NET程序可能将图标存储为命名资源）
                    for (int i = 0; i < rootDirectory.NumberOfNamedEntries; i++)
                    {
                        long entryOffset = resourceOffset + 16 + i * 8;
                        if (entryOffset + 8 > fs.Length)
                        {
                            break;
                        }

                        fs.Position = entryOffset;
                        IMAGERESOURCEDIRECTORYENTRY entry = new()
                        {
                            NameOrId = reader.ReadUInt32(),
                            OffsetToData = reader.ReadUInt32()
                        };

                        // 检查是否是命名资源（最高位为1）
                        if ((entry.NameOrId & 0x80000000) != 0)
                        {
                            // 这是一个命名资源，需要进一步检查
                            long nextLevelOffset = resourceOffset + (entry.OffsetToData & 0x7FFFFFFF);
                            if (nextLevelOffset >= 0 && nextLevelOffset < fs.Length)
                            {
                                PEResourceParserIconNamed.ParseResourceDirectoryForNamedIcons(fs, reader, peInfo, nextLevelOffset);
                            }
                        }
                    }
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
    }
}