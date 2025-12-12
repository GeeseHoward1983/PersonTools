using System;
using System.IO;

namespace MyTool
{
    /// <summary>
    /// PE资源解析器组图标资源解析模块
    /// 专门负责解析PE文件中的组图标资源
    /// </summary>
    internal static class PEResourceParserIconGroup
    {
        /// <summary>
        /// 解析组图标资源
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="directoryOffset">目录偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        public static void ParseGroupIconResource(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset)
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
                        ParseGroupIconResource(fs, reader, peInfo, nextLevelOffset, resourceBaseOffset);
                    }
                    else
                    {
                        // 最高位为0，表示指向数据条目
                        long dataEntryOffset = resourceBaseOffset + entry.OffsetToData;
                        ParseGroupIconDataEntry(fs, reader, peInfo, dataEntryOffset, resourceBaseOffset);
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"组图标资源解析错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析组图标资源数据项
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="dataEntryOffset">数据项偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        public static void ParseGroupIconDataEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset, long resourceBaseOffset)
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
                var dataEntry = new IMAGE_RESOURCE_DATA_ENTRY
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
                    fs.Position = dataOffset;

                    // 读取图标目录头
                    if (fs.Position + 6 > fs.Length)
                        return;

                    var iconDirHeader = new ICON_DIR_HEADER
                    {
                        Reserved = reader.ReadUInt16(),
                        Type = reader.ReadUInt16(),
                        Count = reader.ReadUInt16()
                    };

                    // 检查是否为有效的图标资源
                    if (iconDirHeader.Type != 1 || iconDirHeader.Count == 0)
                        return;

                    // 读取图标目录项
                    var iconDirEntries = new ICON_DIR_ENTRY[iconDirHeader.Count];
                    for (int i = 0; i < iconDirHeader.Count; i++)
                    {
                        if (fs.Position + 16 > fs.Length)
                            break;

                        iconDirEntries[i] = new ICON_DIR_ENTRY
                        {
                            Width = reader.ReadByte(),
                            Height = reader.ReadByte(),
                            ColorCount = reader.ReadByte(),
                            Reserved = reader.ReadByte(),
                            Planes = reader.ReadUInt16(),
                            BitCount = reader.ReadUInt16(),
                            BytesInRes = reader.ReadUInt32(),
                            ImageOffset = reader.ReadUInt32()
                        };
                    }

                    // 为每个图标目录项解析实际的图标数据
                    foreach (var entry in iconDirEntries)
                    {
                        // 查找对应ID的RT_ICON资源
                        long iconDataOffset = PEResourceParserIconHelpers.FindIconDataByResourceId(fs, reader, peInfo, entry.ImageOffset, resourceBaseOffset);
                        if (iconDataOffset != -1)
                        {
                            // 验证图标尺寸，避免添加无效图标
                            int width = entry.Width == 0 ? 256 : entry.Width;
                            int height = entry.Height == 0 ? 256 : entry.Height;
                            
                            if (width <= 0 || height <= 0)
                                continue;
                                
                            var iconInfo = new IconInfo
                            {
                                Width = width,
                                Height = height,
                                BitsPerPixel = entry.BitCount,
                                Size = 6 + 16 + (int)entry.BytesInRes  // ICO文件头(6字节) + 目录项(16字节) + 图像数据
                            };

                            // 构建完整的ICO文件数据
                            // ICO文件结构: 6字节文件头 + 16字节目录项 + 图像数据
                            int fullIconDataSize = 6 + 16 + (int)entry.BytesInRes;
                            if (fullIconDataSize > 0 && fullIconDataSize < 10 * 1024 * 1024) // 限制最大10MB
                            {
                                byte[] fullIconData = new byte[fullIconDataSize];
                                
                                // 写入ICO文件头 (6字节)
                                fullIconData[0] = 0x00; // Reserved
                                fullIconData[1] = 0x00; // Reserved
                                fullIconData[2] = 0x01; // Type (1 = ICO)
                                fullIconData[3] = 0x00; // Type
                                fullIconData[4] = 0x01; // Count (1个图标)
                                fullIconData[5] = 0x00; // Count
                            
                                // 写入目录项 (16字节)
                                fullIconData[6] = entry.Width;
                                fullIconData[7] = entry.Height;
                                fullIconData[8] = entry.ColorCount;
                                fullIconData[9] = entry.Reserved;
                                BitConverter.GetBytes(entry.Planes).CopyTo(fullIconData, 10);
                                BitConverter.GetBytes(entry.BitCount).CopyTo(fullIconData, 12);
                                BitConverter.GetBytes(entry.BytesInRes).CopyTo(fullIconData, 14);
                                // 图像数据偏移量 (从文件开始到图像数据的偏移量)
                                uint imageDataOffset = 6 + 16; // 文件头 + 目录项
                                BitConverter.GetBytes(imageDataOffset).CopyTo(fullIconData, 18);
                            
                                // 读取并写入图像数据
                                if (entry.BytesInRes > 0 && entry.BytesInRes <= fs.Length && iconDataOffset + entry.BytesInRes <= fs.Length)
                                {
                                    fs.Position = iconDataOffset;
                                    byte[] imageData = reader.ReadBytes((int)entry.BytesInRes);
                                    Array.Copy(imageData, 0, fullIconData, 6 + 16, imageData.Length);
                                    iconInfo.Data = fullIconData;
                                }
                                else if (entry.BytesInRes > 0 && entry.BytesInRes <= fs.Length)
                                {
                                    // 如果计算出的大小超过了文件长度，尝试读取从iconDataOffset到文件末尾的所有数据
                                    long remainingBytes = fs.Length - iconDataOffset;
                                    if (remainingBytes > 0 && iconDataOffset < fs.Length)
                                    {
                                        fs.Position = iconDataOffset;
                                        byte[] imageData = reader.ReadBytes((int)Math.Min(remainingBytes, entry.BytesInRes));
                                        Array.Copy(imageData, 0, fullIconData, 6 + 16, imageData.Length);
                                        iconInfo.Data = fullIconData;
                                    }
                                }

                                peInfo.Icons.Add(iconInfo);
                            }
                        }
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析组图标数据项错误: {ex.Message}");
            }
        }
    }
}