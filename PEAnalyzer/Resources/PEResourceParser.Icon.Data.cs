using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Parsers;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器图标数据处理模块
    /// 专门负责处理PE文件中的图标数据
    /// </summary>
    internal static class PEResourceParserIconData
    {
        /// <summary>
        /// 读取一个资源数据项（IMAGE_RESOURCE_DATA_ENTRY），若其内容像图标数据则处理之。
        /// 供直接图标与命名图标解析器共用（消除两处重复逻辑）。
        /// </summary>
        public static void ReadAndProcessIconDataEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset)
        {
            ResourceDirectoryReader.RunAtOffset(fs, dataEntryOffset, 16, "图标资源数据项解析错误", () =>
            {
                IMAGE_RESOURCE_DATA_ENTRY dataEntry = ResourceDirectoryReader.ReadDataEntry(reader);

                // OffsetToData 为 RVA
                long dataOffset = PEParserUtils.RvaToOffset(dataEntry.OffsetToData, peInfo.SectionHeaders);
                if (ResourceDirectoryReader.IsReadableData(dataOffset, dataEntry.Size, fs))
                {
                    fs.Position = dataOffset;
                    byte[] resourceData = reader.ReadBytes((int)dataEntry.Size);
                    if (IsIconData(resourceData))
                    {
                        ProcessIconData(peInfo, resourceData);
                    }
                }
            });
        }

        /// <summary>
        /// 处理图标数据
        /// </summary>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="iconData">图标数据</param>
        public static void ProcessIconData(PEInfo peInfo, byte[] iconData)
        {
            try
            {
                // 检查是否已经是完整的ICO文件
                if (IsCompleteIcoHeader(iconData))
                {
                    // 已经是完整的ICO文件：从 ICONDIR 头与各 ICONDIRENTRY 正确提取每个尺寸的宽/高/位深，
                    // 修复旧实现"宽高位深恒 0 或仅取首项 wBitCount(@12)"且多尺寸 ICO 只记首个的问题。
                    AddIconsFromCompleteIco(peInfo, iconData);
                }
                else
                {
                    // 是DIB数据，需要转换为ICO格式
                    ConvertDibToIco(peInfo, iconData);
                }
            }
            catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException)
            {
                PersonalTools.Utils.AppLogger.Log($"处理图标数据错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 从完整 ICO 文件字节中按 ICONDIR/ICONDIRENTRY 解析所有图标条目，逐个加入 PE 信息。
        /// 每条目从 ICONDIRENTRY 读取宽(@0)、高(@1)（字段值 0 表示 256）、位深 wBitCount(@6)。
        /// 多尺寸 ICO 会记录全部条目而非仅首个；任一字段缺失/越界则保守降级。
        /// </summary>
        private static void AddIconsFromCompleteIco(PEInfo peInfo, byte[] iconData)
        {
            const int IconDirHeaderSize = 6;   // ICONDIR: Reserved(2)+Type(2)+Count(2)
            const int IconDirEntrySize = 16;   // ICONDIRENTRY 固定 16 字节

            // 目录头不完整时退回保守行为：仅登记整段数据、宽高位深保持 0，不抛异常。
            if (iconData.Length < IconDirHeaderSize)
            {
                peInfo.Icons.Add(new IconInfo { Size = iconData.Length, Data = iconData });
                return;
            }

            // Count 字段在偏移 4（ICONDIRHEADER.Count）；为 0 视为无有效条目。
            // 对 count 施加 256 上限（与组图标 ParseIconDirectory 的 Count<=256 一致），
            // 防止畸形文件用最高 65535 的 count 膨胀出大量条目。
            ushort count = BitConverter.ToUInt16(iconData, 4);
            int maxCount = Math.Min((int)count, 256);
            bool anyAdded = false;
            for (int i = 0; i < maxCount; i++)
            {
                int entryOffset = IconDirHeaderSize + (i * IconDirEntrySize);
                // 目录项越界即停止（畸形/截断 ICO 不抛异常，已读到的条目保留）。
                if (entryOffset + IconDirEntrySize > iconData.Length)
                {
                    break;
                }

                // ICONDIRENTRY：Width(@0,字节,0=256)、Height(@1,字节,0=256)、wBitCount(@6,ushort)。
                byte widthByte = iconData[entryOffset];
                byte heightByte = iconData[entryOffset + 1];
                ushort bitCount = BitConverter.ToUInt16(iconData, entryOffset + 6);

                peInfo.Icons.Add(new IconInfo
                {
                    Width = widthByte == 0 ? 256 : widthByte,
                    Height = heightByte == 0 ? 256 : heightByte,
                    BitsPerPixel = bitCount,
                    Size = iconData.Length, // 完整 ICO 各条目共享同一份文件字节，故大小记整段长度
                    Data = iconData
                });
                anyAdded = true;
            }

            // 无任何有效条目（Count=0 或首项即越界）时保留旧的保守降级：登记整段、宽高位深为 0。
            if (!anyAdded)
            {
                peInfo.Icons.Add(new IconInfo { Size = iconData.Length, Data = iconData });
            }
        }

        /// <summary>
        /// 将DIB数据转换为ICO格式
        /// </summary>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="dibData">DIB数据</param>
        /// <param name="resourceName">资源名称</param>
        public static void ConvertDibToIco(PEInfo peInfo, byte[] dibData)
        {
            try
            {
                // 接受 BITMAPINFOHEADER(40) 及向后兼容的 V4(108)/V5(124) 头：
                // 三者 width(+4)/height(+8)/bitCount(+14) 字段偏移一致，biSize>=40 即可正确读取，避免静默丢弃 V4/V5 图标
                if (dibData.Length < 40 || BitConverter.ToUInt32(dibData, 0) < 40)
                {
                    return;
                }

                int width = BitConverter.ToInt32(dibData, 4);
                // 图标 DIB 高度为实际高度的两倍（XOR+AND 掩码）；负数表示自顶向下，先取绝对值。
                // 用 long 取绝对值：Math.Abs(int.MinValue) 会抛 OverflowException 逃逸出本方法致整轮图标解析中止。
                long rawHeight = BitConverter.ToInt32(dibData, 8);
                int height = (int)(Math.Abs(rawHeight) / 2);
                ushort bitCount = BitConverter.ToUInt16(dibData, 14);
                if (width <= 0 || height <= 0 || bitCount == 0)
                {
                    return;
                }

                int fullIconDataSize = 6 + 16 + dibData.Length;
                if (fullIconDataSize is <= 0 or >= (10 * 1024 * 1024)) // 限制最大10MB
                {
                    return;
                }

                peInfo.Icons.Add(new IconInfo
                {
                    Width = width,
                    Height = height,
                    BitsPerPixel = bitCount,
                    Size = fullIconDataSize,
                    Data = BuildIcoFile(dibData, width, height, bitCount)
                });
            }
            catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException or OverflowException)
            {
                PersonalTools.Utils.AppLogger.Log($"转换DIB到ICO错误: {ex.Message}");
            }
        }

        // 将单张 DIB 包装为 .ico 文件字节（6字节文件头 + 16字节目录项 + DIB 数据）
        private static byte[] BuildIcoFile(byte[] dibData, int width, int height, ushort bitCount)
        {
            byte[] fullIconData = new byte[6 + 16 + dibData.Length];

            // ICO 文件头 (6字节)
            fullIconData[0] = 0x00; // Reserved
            fullIconData[1] = 0x00; // Reserved
            fullIconData[2] = 0x01; // Type (1 = ICO)
            fullIconData[3] = 0x00; // Type
            fullIconData[4] = 0x01; // Count (1个图标)
            fullIconData[5] = 0x00; // Count

            // 目录项 (16字节)
            fullIconData[6] = (byte)(width & 0xFF);  // Width
            fullIconData[7] = (byte)(height & 0xFF); // Height
            fullIconData[8] = 0; // ColorCount
            fullIconData[9] = 0; // Reserved
            BitConverter.GetBytes((ushort)1).CopyTo(fullIconData, 10);            // Planes
            BitConverter.GetBytes(bitCount).CopyTo(fullIconData, 12);             // BitCount
            BitConverter.GetBytes((uint)dibData.Length).CopyTo(fullIconData, 14); // BytesInRes
            BitConverter.GetBytes((uint)(6 + 16)).CopyTo(fullIconData, 18);       // 图像数据偏移（文件头+目录项）

            // 复制图像数据
            Array.Copy(dibData, 0, fullIconData, 6 + 16, dibData.Length);

            return fullIconData;
        }

        /// <summary>
        /// 检查数据是否可能是图标数据（ICO 文件头或 BITMAPINFOHEADER）。
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns>是否可能是图标数据</returns>
        public static bool IsIconData(byte[] data)
        {
            if (data == null || data.Length < 4)
            {
                return false;
            }

            // ICO 文件头: 00 00 01 00（Reserved=0, Type=1）
            if (IsCompleteIcoHeader(data))
            {
                return true;
            }

            // DIB 数据：以 BITMAPINFOHEADER 开始（biSize == 40）
            return data.Length >= 16 && BitConverter.ToUInt32(data, 0) == 40;
        }

        // ICO 文件头: 00 00 01 00（Reserved=0, Type=1=ICO）
        private static bool IsCompleteIcoHeader(byte[] data)
        {
            return data.Length >= 4 && data[0] == 0x00 && data[1] == 0x00 && data[2] == 0x01 && data[3] == 0x00;
        }
    }
}