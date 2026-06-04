using PersonalTools.PEAnalyzer.Parsers;
using PersonalTools.PEAnalyzer.Models;
using System.IO;
using System.Text;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器图标信息解析辅助模块
    /// 包含图标信息解析的辅助函数
    /// </summary>
    internal static class PEResourceParserIconHelpers
    {
        /// <summary>
        /// 查找特定ID的图标数据
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="directoryOffset">目录偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        /// <param name="resourceId">资源ID</param>
        /// <returns>图标数据偏移量</returns>
        internal static long FindSpecificIconData(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset, uint resourceId)
        {
            try
            {
                if (directoryOffset < 0 || directoryOffset + 16 > fs.Length)
                {
                    return -1;
                }

                long originalPosition = fs.Position;
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

                int totalEntries = directory.NumberOfNamedEntries + directory.NumberOfIdEntries;

                // 查找指定ID的资源
                for (int i = 0; i < totalEntries; i++)
                {
                    long entryOffset = directoryOffset + 16 + i * 8;
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

                    // 检查资源ID是否匹配
                    if ((entry.NameOrId & 0xFFFF) == (resourceId & 0xFFFF))
                    {
                        // 处理语言子目录
                        if ((entry.OffsetToData & 0x80000000) != 0)
                        {
                            long nextLevelOffset = resourceBaseOffset + (entry.OffsetToData & 0x7FFFFFFF);
                            long dataOffset = FindIconDataInLanguageDirectory(fs, reader, peInfo, nextLevelOffset, resourceBaseOffset);
                            fs.Position = originalPosition;
                            return dataOffset;
                        }
                        else
                        {
                            long dataEntryOffset = resourceBaseOffset + entry.OffsetToData;
                            long dataOffset = GetIconDataFromEntry(fs, reader, peInfo, dataEntryOffset);
                            fs.Position = originalPosition;
                            return dataOffset;
                        }
                    }
                }

                fs.Position = originalPosition;
                return -1;
            }
            catch (IOException)
            {
                return -1;
            }
            catch (UnauthorizedAccessException)
            {
                return -1;
            }
            catch (ArgumentOutOfRangeException)
            {
                return -1;
            }
        }

        /// <summary>
        /// 在语言目录中查找图标数据
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="directoryOffset">目录偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        /// <returns>图标数据偏移量</returns>
        internal static long FindIconDataInLanguageDirectory(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset)
        {
            try
            {
                if (directoryOffset < 0 || directoryOffset + 16 > fs.Length)
                {
                    return -1;
                }

                long originalPosition = fs.Position;
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

                // 通常第一个条目就是我们需要的
                if (directory.NumberOfNamedEntries + directory.NumberOfIdEntries > 0)
                {
                    fs.Position = directoryOffset + 16; // 第一个条目位置

                    IMAGERESOURCEDIRECTORYENTRY entry = new()
                    {
                        NameOrId = reader.ReadUInt32(),
                        OffsetToData = reader.ReadUInt32()
                    };

                    if ((entry.OffsetToData & 0x80000000) == 0)
                    {
                        long dataEntryOffset = resourceBaseOffset + entry.OffsetToData;
                        if (dataEntryOffset < 0 || dataEntryOffset + 16 > fs.Length)
                        {
                            fs.Position = originalPosition;
                            return -1;
                        }

                        long dataOffset = GetIconDataFromEntry(fs, reader, peInfo, dataEntryOffset);
                        fs.Position = originalPosition;
                        return dataOffset;
                    }
                }

                fs.Position = originalPosition;
                return -1;
            }
            catch (IOException)
            {
                return -1;
            }
            catch (UnauthorizedAccessException)
            {
                return -1;
            }
            catch (ArgumentOutOfRangeException)
            {
                return -1;
            }
        }

        /// <summary>
        /// 从数据条目获取图标数据
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="dataEntryOffset">数据条目偏移</param>
        /// <returns>图标数据偏移量</returns>
        internal static long GetIconDataFromEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset)
        {
            try
            {
                if (dataEntryOffset < 0 || dataEntryOffset + 16 > fs.Length)
                {
                    return -1;
                }

                long originalPosition = fs.Position;
                fs.Position = dataEntryOffset;

                // 读取资源数据项
                IMAGERESOURCEDATAENTRY dataEntry = new()
                {
                    OffsetToData = reader.ReadUInt32(),
                    Size = reader.ReadUInt32(),
                    CodePage = reader.ReadUInt32(),
                    Reserved = reader.ReadUInt32()
                };

                // 计算实际数据偏移
                long dataOffset = Utilities.RvaToOffset(dataEntry.OffsetToData, peInfo.SectionHeaders);

                // 验证数据偏移和大小的有效性
                long endOffset = dataOffset + (long)dataEntry.Size;
                if (dataOffset == -1 || dataOffset < 0 || dataEntry.Size == 0 || dataEntry.Size > int.MaxValue || endOffset > fs.Length)
                {
                    fs.Position = originalPosition;
                    return -1;
                }

                fs.Position = originalPosition;
                return dataOffset;
            }
            catch (IOException)
            {
                return -1;
            }
            catch (UnauthorizedAccessException)
            {
                return -1;
            }
            catch (ArgumentOutOfRangeException)
            {
                return -1;
            }
        }

        /// <summary>
        /// 根据资源ID查找图标数据
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="resourceId">资源ID</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        /// <returns>图标数据偏移量</returns>
        internal static long FindIconDataByResourceId(FileStream fs, BinaryReader reader, PEInfo peInfo, uint resourceId, long resourceBaseOffset)
        {
            try
            {
                // 图标数据在RT_ICON资源类型中 (ID = 3)
                const int RT_ICON_TYPE = 3;

                // 获取资源目录的RVA
                if (peInfo.OptionalHeader.DataDirectory.Length <= PEConstants.DirectoryResource ||
                    peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryResource].VirtualAddress == 0)
                {
                    return -1;
                }

                uint resourceRVA = peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryResource].VirtualAddress;
                long resourceOffset = Utilities.RvaToOffset(resourceRVA, peInfo.SectionHeaders);
                if (resourceOffset == -1 || resourceOffset + 16 > fs.Length)
                {
                    return -1;
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

                // 查找RT_ICON资源类型
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

                    if ((entry.NameOrId & 0xFFFF) == RT_ICON_TYPE)
                    {
                        long nextLevelOffset = resourceOffset + (entry.OffsetToData & 0x7FFFFFFF);
                        if (nextLevelOffset < 0 || nextLevelOffset + 16 > fs.Length)
                        {
                            continue;
                        }

                        long iconDataOffset = FindSpecificIconData(fs, reader, peInfo, nextLevelOffset, resourceBaseOffset, resourceId);
                        fs.Position = originalPosition;
                        return iconDataOffset;
                    }
                }

                fs.Position = originalPosition;
                return -1;
            }
            catch (IOException)
            {
                return -1;
            }
            catch (UnauthorizedAccessException)
            {
                return -1;
            }
            catch (ArgumentOutOfRangeException)
            {
                return -1;
            }
        }

        /// <summary>
        /// 读取资源名称
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="nameOffset">名称偏移</param>
        /// <returns>资源名称</returns>
        internal static string ReadResourceName(FileStream fs, BinaryReader reader, long nameOffset)
        {
            try
            {
                if (nameOffset < 0 || nameOffset + 2 > fs.Length)
                {
                    return string.Empty;
                }

                long originalPosition = fs.Position;
                fs.Position = nameOffset;

                // 读取名称长度（Unicode字符数）
                if (fs.Position + 2 > fs.Length)
                {
                    fs.Position = originalPosition;
                    return string.Empty;
                }

                ushort nameLength = reader.ReadUInt16();
                if (nameLength == 0 || fs.Position + nameLength * 2 > fs.Length)
                {
                    fs.Position = originalPosition;
                    return string.Empty;
                }

                // 读取名称（Unicode字符串）
                byte[] nameBytes = reader.ReadBytes(nameLength * 2);
                string resourceName = Encoding.Unicode.GetString(nameBytes);

                fs.Position = originalPosition;
                return resourceName;
            }
            catch (IOException)
            {
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return string.Empty;
            }
            catch (ArgumentOutOfRangeException)
            {
                return string.Empty;
            }
        }
    }
}