using PersonalTools.PEAnalyzer.Models;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器图标数据处理模块
    /// 专门负责处理PE文件中的图标数据
    /// </summary>
    internal static class PEResourceParserIconData
    {
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
                if (iconData.Length >= 4 &&
                    iconData[0] == 0x00 && iconData[1] == 0x00 && // Reserved
                    iconData[2] == 0x01 && iconData[3] == 0x00)   // Type (1 = ICO)
                {
                    // 已经是完整的ICO文件，直接使用
                    var iconInfo = new IconInfo
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
            catch (Exception ex)
            {
                Console.WriteLine($"处理图标数据错误: {ex.Message}");
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
                // 检查BITMAPINFOHEADER
                if (dibData.Length < 40)
                    return;

                uint biSize = BitConverter.ToUInt32(dibData, 0);
                if (biSize != 40)
                    return;

                // 从BITMAPINFOHEADER中提取宽度和高度
                int width = BitConverter.ToInt32(dibData, 4);
                int height = BitConverter.ToInt32(dibData, 8);
                // 高度是实际高度的两倍（包含遮罩）
                height /= 2;

                // 从BITMAPINFOHEADER中提取色深
                ushort bitCount = BitConverter.ToUInt16(dibData, 14);

                // 构建完整的ICO文件数据
                int fullIconDataSize = 6 + 16 + dibData.Length;
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
                    fullIconData[6] = (byte)(width & 0xFF);  // Width
                    fullIconData[7] = (byte)(height & 0xFF); // Height
                    fullIconData[8] = 0; // ColorCount
                    fullIconData[9] = 0; // Reserved
                    BitConverter.GetBytes((ushort)1).CopyTo(fullIconData, 10); // Planes
                    BitConverter.GetBytes(bitCount).CopyTo(fullIconData, 12); // BitCount
                    BitConverter.GetBytes((uint)dibData.Length).CopyTo(fullIconData, 14); // BytesInRes
                    // 图像数据偏移量 (从文件开始到图像数据的偏移量)
                    uint imageDataOffset = 6 + 16; // 文件头 + 目录项
                    BitConverter.GetBytes(imageDataOffset).CopyTo(fullIconData, 18);

                    // 复制图像数据
                    Array.Copy(dibData, 0, fullIconData, 6 + 16, dibData.Length);

                    var iconInfo = new IconInfo
                    {
                        Width = width,
                        Height = height,
                        BitsPerPixel = bitCount,
                        Size = fullIconDataSize,
                        Data = fullIconData
                    };

                    peInfo.Icons.Add(iconInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转换DIB到ICO错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查数据是否可能是图标数据
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns>是否可能是图标数据</returns>
        public static bool IsIconData(byte[] data)
        {
            try
            {
                if (data == null || data.Length < 4)
                    return false;

                // 检查是否是ICO文件头
                if (data.Length >= 4 &&
                    data[0] == 0x00 && data[1] == 0x00 && // Reserved
                    data[2] == 0x01 && data[3] == 0x00)   // Type (1 = ICO)
                {
                    return true;
                }

                // 检查是否是有效的DIB数据（以BITMAPINFOHEADER开始）
                if (data.Length >= 4)
                {
                    // BITMAPINFOHEADER的biSize字段应该是40字节
                    uint biSize = BitConverter.ToUInt32(data, 0);
                    if (biSize == 40 && data.Length >= 16)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}