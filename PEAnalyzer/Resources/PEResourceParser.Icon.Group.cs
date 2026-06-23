using PersonalTools.PEAnalyzer.Parsers;
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
        /// 解析组图标资源（递归遍历目录，叶子节点交给数据项解析）。
        /// </summary>
        public static void ParseGroupIconResource(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset)
        {
            ResourceDirectoryReader.RunAtOffset(fs, directoryOffset, ResourceDirectoryReader.DirectoryHeaderSize, "组图标资源解析错误", () =>
                ResourceDirectoryReader.WalkEntries(fs, reader, directoryOffset, resourceBaseOffset,
                    subdirectoryOffset => ParseGroupIconResource(fs, reader, peInfo, subdirectoryOffset, resourceBaseOffset),
                    dataEntryOffset => ParseGroupIconDataEntry(fs, reader, peInfo, dataEntryOffset, resourceBaseOffset)));
        }

        /// <summary>
        /// 解析组图标资源数据项。
        /// </summary>
        public static void ParseGroupIconDataEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset, long resourceBaseOffset)
        {
            ResourceDirectoryReader.RunAtOffset(fs, dataEntryOffset, 16, "解析组图标数据项错误", () =>
            {
                IMAGE_RESOURCE_DATA_ENTRY dataEntry = ResourceDirectoryReader.ReadDataEntry(reader);

                // OffsetToData 为 RVA，需转换为文件偏移
                long dataOffset = PEParserUtils.RvaToOffset(dataEntry.OffsetToData, peInfo.SectionHeaders);
                if (ResourceDirectoryReader.IsReadableData(dataOffset, dataEntry.Size, fs) && dataOffset + 6 <= fs.Length)
                {
                    fs.Position = dataOffset;
                    ParseIconDirectory(fs, reader, peInfo, resourceBaseOffset);
                }
            });
        }

        /// <summary>
        /// 从当前位置读取 ICONDIR 头与各 ICONDIRENTRY，校验后处理每个图标条目。
        /// </summary>
        private static void ParseIconDirectory(FileStream fs, BinaryReader reader, PEInfo peInfo, long resourceBaseOffset)
        {
            ICONDIRHEADER iconDirHeader = new()
            {
                Reserved = reader.ReadUInt16(),
                Type = reader.ReadUInt16(),
                Count = reader.ReadUInt16()
            };

            // 校验为有效图标资源 (Type==1, 1<=Count<=256)
            if (iconDirHeader.Type != 1 || iconDirHeader.Count is 0 or > 256)
            {
                return;
            }

            if (!TryReadIconDirEntries(fs, reader, iconDirHeader.Count, out ICONDIRENTRY[] iconDirEntries))
            {
                return;
            }

            ProcessGroupIconEntries(fs, reader, peInfo, iconDirEntries, resourceBaseOffset);
        }

        private static bool TryReadIconDirEntries(FileStream fs, BinaryReader reader, ushort count, out ICONDIRENTRY[] iconDirEntries)
        {
            iconDirEntries = [];
            if (count is 0 or > 256)
            {
                return false;
            }

            iconDirEntries = new ICONDIRENTRY[count];
            for (int i = 0; i < count; i++)
            {
                if (fs.Position + 16 > fs.Length)
                {
                    // 数据截断：保留已成功解析的前 i 个条目并按成功返回(降级)，
                    // 而非整组丢弃，使损坏图标资源仍能展示已读到的部分。
                    iconDirEntries = iconDirEntries[0..i];
                    return i > 0;
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
                IconInfo? icon = TryBuildIcon(fs, reader, peInfo, entry, resourceBaseOffset);
                if (icon != null)
                {
                    peInfo.Icons.Add(icon);
                }
            }
        }

        /// <summary>
        /// 为单个 ICONDIRENTRY 定位并读取图标数据，构造可独立保存的 .ico 字节；失败返回 null。
        /// </summary>
        private static IconInfo? TryBuildIcon(FileStream fs, BinaryReader reader, PEInfo peInfo, ICONDIRENTRY entry, long resourceBaseOffset)
        {
            long iconDataOffset = PEResourceParserIconHelpers.FindIconDataByResourceId(fs, reader, peInfo, entry.ImageOffset, resourceBaseOffset);
            if (iconDataOffset == -1)
            {
                return null;
            }

            int width = entry.Width == 0 ? 256 : entry.Width;
            int height = entry.Height == 0 ? 256 : entry.Height;
            if (width <= 0 || height <= 0 || entry.BytesInRes == 0 || entry.BytesInRes > int.MaxValue)
            {
                return null;
            }

            // 在 long 上计算以避免 6+16+BytesInRes 的 int 溢出；据声明的 BytesInRes 做上限拦截（拒绝过大图标）
            long fullIconDataSizeLong = 6L + 16 + entry.BytesInRes;
            if (fullIconDataSizeLong <= 0 || fullIconDataSizeLong >= 10 * 1024 * 1024)
            {
                return null;
            }

            byte[]? imageData = ReadIconImageData(fs, reader, iconDataOffset, entry.BytesInRes);
            if (imageData == null)
            {
                return null;
            }

            // 以实际读到的 imageData.Length 重算尺寸：截断兜底时数据可能短于声明的 BytesInRes，
            // 保证 .ico 缓冲大小与目录项 BytesInRes 三者一致，避免下游解码读到尾部填充零
            int actualFullSize = 6 + 16 + imageData.Length;

            return new IconInfo
            {
                Width = width,
                Height = height,
                BitsPerPixel = entry.BitCount,
                Size = actualFullSize,
                Data = BuildSingleIconFile(entry, imageData, actualFullSize)
            };
        }

        /// <summary>
        /// 读取图标位图数据；完整可读时读取 BytesInRes，否则按剩余字节兜底。失败返回 null。
        /// </summary>
        private static byte[]? ReadIconImageData(FileStream fs, BinaryReader reader, long iconDataOffset, uint bytesInRes)
        {
            if (iconDataOffset < 0 || iconDataOffset >= fs.Length)
            {
                return null;
            }

            if (iconDataOffset + bytesInRes <= fs.Length)
            {
                fs.Position = iconDataOffset;
                byte[] data = reader.ReadBytes((int)bytesInRes);
                return data.Length == bytesInRes ? data : null;
            }

            // 截断兜底：读取剩余字节
            long remainingBytes = fs.Length - iconDataOffset;
            if (remainingBytes <= 0)
            {
                return null;
            }

            fs.Position = iconDataOffset;
            byte[] partial = reader.ReadBytes((int)Math.Min(remainingBytes, bytesInRes));
            // 已按 Min(remaining, bytesInRes) 限制读取量，只要读到非空数据即可用（不与 remainingBytes 做脆弱的等值比较）
            return partial.Length > 0 ? partial : null;
        }

        /// <summary>
        /// 用单个图标条目 + 位图数据拼装成一个独立 .ico 文件的字节数组
        /// （ICONDIR(6) + 1 个 ICONDIRENTRY(16) + 位图数据）。
        /// </summary>
        private static byte[] BuildSingleIconFile(ICONDIRENTRY entry, byte[] imageData, int fullIconDataSize)
        {
            byte[] fullIconData = new byte[fullIconDataSize];

            // ICONDIR: Reserved=0, Type=1, Count=1
            fullIconData[2] = 0x01;
            fullIconData[4] = 0x01;

            // ICONDIRENTRY @ offset 6
            fullIconData[6] = entry.Width;
            fullIconData[7] = entry.Height;
            fullIconData[8] = entry.ColorCount;
            fullIconData[9] = entry.Reserved;
            BitConverter.GetBytes(entry.Planes).CopyTo(fullIconData, 10);
            BitConverter.GetBytes(entry.BitCount).CopyTo(fullIconData, 12);
            // 目录项 BytesInRes 写实际数据长度（而非声明的 entry.BytesInRes），与缓冲及位图数据保持一致
            BitConverter.GetBytes((uint)imageData.Length).CopyTo(fullIconData, 14);
            BitConverter.GetBytes((uint)(6 + 16)).CopyTo(fullIconData, 18);

            // 位图数据 @ offset 22
            Array.Copy(imageData, 0, fullIconData, 6 + 16, imageData.Length);
            return fullIconData;
        }
    }
}
