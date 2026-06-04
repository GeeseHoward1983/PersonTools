using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
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
                if (directoryOffset < 0 || directoryOffset + 16 > fs.Length)
                {
                    return;
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

                // 遍历子项查找语言节点
                int totalEntries = directory.NumberOfNamedEntries + directory.NumberOfIdEntries;
                for (int i = 0; i < totalEntries; i++)
                {
                    long entryOffset = directoryOffset + 16 + i * 8;
                    if (entryOffset + 8 > fs.Length)
                    {
                        break;
                    }

                    fs.Position = entryOffset; // 跳过目录头(16字节)，每项8字节

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
                        long nextLevelOffset = resourceBaseOffset + (entry.OffsetToData & 0x7FFFFFFF);
                        if (nextLevelOffset >= 0 && nextLevelOffset + 16 <= fs.Length)
                        {
                            ParseGroupIconResource(fs, reader, peInfo, nextLevelOffset, resourceBaseOffset);
                        }
                    }
                    else
                    {
                        long dataEntryOffset = resourceBaseOffset + entry.OffsetToData;
                        ParseGroupIconDataEntry(fs, reader, peInfo, dataEntryOffset, resourceBaseOffset);
                    }
                }

                fs.Position = originalPosition;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"组图标资源解析错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"组图标资源解析错误: {ex.Message}");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($"组图标资源解析错误: {ex.Message}");
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
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
                if (dataEntryOffset < 0 || dataEntryOffset + 16 > fs.Length)
                {
                    return;
                }

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
                if (dataOffset != -1 && dataOffset >= 0 && dataEntry.Size > 0 && dataEntry.Size <= int.MaxValue && dataOffset + dataEntry.Size <= fs.Length && dataOffset + 6 <= fs.Length)
                {
                    fs.Position = dataOffset;

                    // 读取图标目录头
                    if (fs.Position + 6 > fs.Length)
                    {
                        return;
                    }

                    ICONDIRHEADER iconDirHeader = new()
                    {
                        Reserved = reader.ReadUInt16(),
                        Type = reader.ReadUInt16(),
                        Count = reader.ReadUInt16()
                    };

                    // 检查是否为有效的图标资源
                    if (iconDirHeader.Type != 1 || iconDirHeader.Count == 0 || iconDirHeader.Count > 256)
                    {
                        return;
                    }

                    if (!TryReadIconDirEntries(fs, reader, iconDirHeader.Count, out ICONDIRENTRY[] iconDirEntries))
                    {
                        return;
                    }

                    ProcessGroupIconEntries(fs, reader, peInfo, iconDirEntries, resourceBaseOffset);
                }

                fs.Position = originalPosition;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"解析组图标数据项错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"解析组图标数据项错误: {ex.Message}");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Console.WriteLine($"解析组图标数据项错误: {ex.Message}");
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
            }
        }

        private static bool TryReadIconDirEntries(FileStream fs, BinaryReader reader, ushort count, out ICONDIRENTRY[] iconDirEntries)
        {
            iconDirEntries = Array.Empty<ICONDIRENTRY>();
            if (count == 0 || count > 256)
            {
                return false;
            }

            iconDirEntries = new ICONDIRENTRY[count];
            for (int i = 0; i < count; i++)
            {
                if (fs.Position + 16 > fs.Length)
                {
                    iconDirEntries = iconDirEntries[0..i];
                    return false;
                }

                iconDirEntries[i] = new ICONDIRENTRY
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

            return true;
        }

        private static void ProcessGroupIconEntries(FileStream fs, BinaryReader reader, PEInfo peInfo, ICONDIRENTRY[] iconDirEntries, long resourceBaseOffset)
        {
            foreach (ICONDIRENTRY entry in iconDirEntries)
            {
                long iconDataOffset = PEResourceParserIconHelpers.FindIconDataByResourceId(fs, reader, peInfo, entry.ImageOffset, resourceBaseOffset);
                if (iconDataOffset == -1)
                {
                    continue;
                }

                int width = entry.Width == 0 ? 256 : entry.Width;
                int height = entry.Height == 0 ? 256 : entry.Height;
                if (width <= 0 || height <= 0)
                {
                    continue;
                }

                if (entry.BytesInRes == 0 || entry.BytesInRes > int.MaxValue)
                {
                    continue;
                }

                int fullIconDataSize = 6 + 16 + (int)entry.BytesInRes;
                if (fullIconDataSize <= 0 || fullIconDataSize >= 10 * 1024 * 1024)
                {
                    continue;
                }

                IconInfo iconInfo = new()
                {
                    Width = width,
                    Height = height,
                    BitsPerPixel = entry.BitCount,
                    Size = fullIconDataSize
                };

                byte[] fullIconData = new byte[fullIconDataSize];
                fullIconData[0] = 0x00;
                fullIconData[1] = 0x00;
                fullIconData[2] = 0x01;
                fullIconData[3] = 0x00;
                fullIconData[4] = 0x01;
                fullIconData[5] = 0x00;
                fullIconData[6] = entry.Width;
                fullIconData[7] = entry.Height;
                fullIconData[8] = entry.ColorCount;
                fullIconData[9] = entry.Reserved;
                BitConverter.GetBytes(entry.Planes).CopyTo(fullIconData, 10);
                BitConverter.GetBytes(entry.BitCount).CopyTo(fullIconData, 12);
                BitConverter.GetBytes(entry.BytesInRes).CopyTo(fullIconData, 14);
                uint imageDataOffset = 6 + 16;
                BitConverter.GetBytes(imageDataOffset).CopyTo(fullIconData, 18);

                if (iconDataOffset >= 0 && iconDataOffset + entry.BytesInRes <= fs.Length)
                {
                    fs.Position = iconDataOffset;
                    byte[] imageData = reader.ReadBytes((int)entry.BytesInRes);
                    if (imageData.Length == entry.BytesInRes)
                    {
                        Array.Copy(imageData, 0, fullIconData, 6 + 16, imageData.Length);
                        iconInfo.Data = fullIconData;
                        peInfo.Icons.Add(iconInfo);
                    }
                }
                else
                {
                    long remainingBytes = fs.Length - iconDataOffset;
                    if (remainingBytes > 0 && iconDataOffset < fs.Length)
                    {
                        fs.Position = iconDataOffset;
                        byte[] imageData = reader.ReadBytes((int)Math.Min(remainingBytes, entry.BytesInRes));
                        if (imageData.Length == remainingBytes && imageData.Length > 0)
                        {
                            Array.Copy(imageData, 0, fullIconData, 6 + 16, imageData.Length);
                            iconInfo.Data = fullIconData;
                            peInfo.Icons.Add(iconInfo);
                        }
                    }
                }
            }
        }
    }
}