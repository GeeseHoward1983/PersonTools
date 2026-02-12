using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器直接图标资源解析模块
    /// 专门负责解析PE文件中的直接图标资源
    /// </summary>
    internal static class PEResourceParserIconDirect
    {
        /// <summary>
        /// 解析直接的RT_ICON资源（非GROUP_ICON中的）
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="directoryOffset">目录偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        public static void ParseDirectIconResource(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset)
        {
            try
            {
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

                // 遍历子项查找语言节点
                int totalEntries = directory.NumberOfNamedEntries + directory.NumberOfIdEntries;
                for (int i = 0; i < totalEntries; i++)
                {
                    fs.Position = directoryOffset + 16 + i * 8; // 跳过目录头(16字节)，每项8字节

                    IMAGERESOURCEDIRECTORYENTRY entry = new()
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
                        ParseDirectIconResource(fs, reader, peInfo, nextLevelOffset, resourceBaseOffset);
                    }
                    else
                    {
                        // 最高位为0，表示指向数据条目
                        long dataEntryOffset = resourceBaseOffset + entry.OffsetToData;
                        ParseDirectIconDataEntry(fs, reader, peInfo, dataEntryOffset);
                    }
                }

                fs.Position = originalPosition;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"直接图标资源解析错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"直接图标资源解析错误: {ex.Message}");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($"直接图标资源解析错误: {ex.Message}");
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 解析直接图标资源数据项
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="dataEntryOffset">数据项偏移</param>
        private static void ParseDirectIconDataEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset)
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
                Console.WriteLine($"直接图标资源数据项解析错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"直接图标资源数据项解析错误: {ex.Message}");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($"直接图标资源数据项解析错误: {ex.Message}");
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
            }
        }
    }
}