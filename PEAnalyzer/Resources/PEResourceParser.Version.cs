using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器版本信息解析模块
    /// 专门负责解析PE文件中的版本信息资源
    /// </summary>
    public static class PEResourceParserVersion
    {
        /// <summary>
        /// 解析版本信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        public static void ParseVersionInfo(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            try
            {
                // 版本信息通常在资源节中，数据目录索引为#2 (IMAGE_DIRECTORY_ENTRY_RESOURCE)
                const int RESOURCE_DIRECTORY_INDEX = 2; // IMAGE_DIRECTORY_ENTRY_RESOURCE

                if (peInfo.OptionalHeader.DataDirectory.Length > RESOURCE_DIRECTORY_INDEX &&
                    peInfo.OptionalHeader.DataDirectory[RESOURCE_DIRECTORY_INDEX].VirtualAddress != 0)
                {
                    uint resourceRVA = peInfo.OptionalHeader.DataDirectory[RESOURCE_DIRECTORY_INDEX].VirtualAddress;
                    long resourceOffset = PEResourceParserCore.RvaToOffset(resourceRVA, peInfo.SectionHeaders);

                    if (resourceOffset != -1 && resourceOffset < fs.Length)
                    {
                        // 解析资源目录以找到版本信息
                        ParseResourceDirectoryForVersionInfo(fs, reader, peInfo, resourceOffset);
                    }
                    else
                    {
                        peInfo.AdditionalInfo.FileVersion = $"无效的资源偏移: 0x{resourceRVA:X8}";
                    }
                }
                else
                {
                    peInfo.AdditionalInfo.FileVersion = "文件不包含版本资源";
                }
            }
            catch (Exception ex)
            {
                // 解析版本信息时出现异常，记录日志但不中断程序执行
                peInfo.AdditionalInfo.FileVersion = $"解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析资源目录以查找版本信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="resourceOffset">资源节偏移</param>
        private static void ParseResourceDirectoryForVersionInfo(FileStream fs, BinaryReader reader, PEInfo peInfo, long resourceOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = resourceOffset;

                // 读取根资源目录
                var rootDirectory = new IMAGE_RESOURCE_DIRECTORY
                {
                    Characteristics = reader.ReadUInt32(),
                    TimeDateStamp = reader.ReadUInt32(),
                    MajorVersion = reader.ReadUInt16(),
                    MinorVersion = reader.ReadUInt16(),
                    NumberOfNamedEntries = reader.ReadUInt16(),
                    NumberOfIdEntries = reader.ReadUInt16()
                };

                int totalEntries = rootDirectory.NumberOfNamedEntries + rootDirectory.NumberOfIdEntries;

                // 遍历资源目录项
                for (int i = 0; i < totalEntries; i++)
                {
                    fs.Position = resourceOffset + 16 + i * 8; // 16是IMAGE_RESOURCE_DIRECTORY大小，每项8字节

                    var entry = new IMAGE_RESOURCE_DIRECTORY_ENTRY
                    {
                        NameOrId = reader.ReadUInt32(),
                        OffsetToData = reader.ReadUInt32()
                    };

                    // 检查是否是RT_VERSION资源类型 (ID = 16)
                    if ((entry.NameOrId & 0xFFFF) == 16) // RT_VERSION = 16
                    {
                        long nextLevelOffset = resourceOffset + (entry.OffsetToData & 0x7FFFFFFF);
                        ParseVersionResource(fs, reader, peInfo, nextLevelOffset, resourceOffset);
                        break;
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                peInfo.AdditionalInfo.FileVersion = $"资源目录解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析版本资源
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="directoryOffset">目录偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        private static void ParseVersionResource(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = directoryOffset;

                // 读取资源目录
                var directory = new IMAGE_RESOURCE_DIRECTORY
                {
                    Characteristics = reader.ReadUInt32(),
                    TimeDateStamp = reader.ReadUInt32(),
                    MajorVersion = reader.ReadUInt16(),
                    MinorVersion = reader.ReadUInt16(),
                    NumberOfNamedEntries = reader.ReadUInt16(),
                    NumberOfIdEntries = reader.ReadUInt16()
                };

                // 遍历子项查找语言节点
                int totalEntries = directory.NumberOfNamedEntries + directory.NumberOfIdEntries;
                for (int i = 0; i < totalEntries; i++)
                {
                    fs.Position = directoryOffset + 16 + i * 8; // 跳过目录头(16字节)，每项8字节

                    var entry = new IMAGE_RESOURCE_DIRECTORY_ENTRY
                    {
                        NameOrId = reader.ReadUInt32(),
                        OffsetToData = reader.ReadUInt32()
                    };

                    // 检查是否是叶子节点
                    // 最高位为1表示指向下一级目录，为0表示指向数据条目
                    // 我们需要处理两种情况
                    if ((entry.OffsetToData & 0x80000000) != 0)
                    {
                        // 最高位为1，表示指向下一级目录
                        // 清除最高位得到实际偏移
                        long nextLevelOffset = resourceBaseOffset + (entry.OffsetToData & 0x7FFFFFFF);
                        // 递归处理下一级目录
                        ParseVersionResource(fs, reader, peInfo, nextLevelOffset, resourceBaseOffset);
                    }
                    else
                    {
                        // 最高位为0，表示指向数据条目
                        long dataEntryOffset = resourceBaseOffset + entry.OffsetToData;
                        ParseVersionDataEntry(fs, reader, peInfo, dataEntryOffset);
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                peInfo.AdditionalInfo.FileVersion = $"版本资源解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析资源数据项
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="dataEntryOffset">数据项偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        private static void ParseVersionDataEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = dataEntryOffset;

                // 检查是否有足够的数据读取IMAGE_RESOURCE_DATA_ENTRY
                if (fs.Position + 16 > fs.Length)
                {
                    peInfo.AdditionalInfo.FileVersion = "资源数据条目不完整";
                    return;
                }

                // 读取资源数据项
                var dataEntry = new IMAGE_RESOURCE_DATA_ENTRY
                {
                    OffsetToData = reader.ReadUInt32(),
                    Size = reader.ReadUInt32(),
                    CodePage = reader.ReadUInt32(),
                    Reserved = reader.ReadUInt32()
                };

                // 计算实际数据偏移（注意：资源数据的OffsetToData是RVA）
                long dataOffset = PEResourceParserCore.RvaToOffset(dataEntry.OffsetToData, peInfo.SectionHeaders);
                if (dataOffset != -1 && dataOffset < fs.Length)
                {
                    fs.Position = dataOffset;

                    // 读取版本信息结构
                    PEResourceParserVersionHelpers.ParseVersionInfoStructure(fs, reader, peInfo);
                }
                else
                {
                    peInfo.AdditionalInfo.FileVersion = $"无效的数据偏移: 0x{dataEntry.OffsetToData:X8}";
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                peInfo.AdditionalInfo.FileVersion = $"数据项解析错误: {ex.Message}";
            }
        }
    }
}