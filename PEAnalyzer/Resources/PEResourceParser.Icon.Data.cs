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
                    // 已经是完整的ICO文件，直接使用
                    IconInfo iconInfo = new()
                    {
                        Width = 0, // 从ICO文件头中提取
                        Height = 0, // 从ICO文件头中提取
                        BitsPerPixel = 0, // 从ICO文件头中提取
                        Size = iconData.Length,
                        Data = iconData
                    };

                    // 尝试从ICO文件头中提取信息
                    if (iconData.Length >= 22)
                    {
                        // 读取第一项目录信息
                        byte width = iconData[6];
                        byte height = iconData[7];

                        ushort bitCount = BitConverter.ToUInt16(iconData, 12);

                        iconInfo.Width = width == 0 ? 256 : width;
                        iconInfo.Height = height == 0 ? 256 : height;
                        iconInfo.BitsPerPixel = bitCount;
                    }

                    peInfo.Icons.Add(iconInfo);
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