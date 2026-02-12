using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static partial class VersionSymbleTable
    {
        private static void ParseVersionDependencies(ELFParser parser)
        {
            // 查找版本依赖 (DT_VERNEED)
            long verneedAddr = 0;
            long verneedNum = 0;

            if (parser.DynamicEntries != null)
            {
                foreach (ELFDynamic entry in parser.DynamicEntries)
                {
                    if (entry.d_tag == (long)DynamicTag.DT_VERNEED)
                    {
                        verneedAddr = (long)entry.d_val;
                    }
                    else if (entry.d_tag == (long)DynamicTag.DT_VERNEEDNUM)
                    {
                        verneedNum = (long)entry.d_val;
                    }
                }
            }

            if (verneedAddr > 0 && verneedNum > 0)
            {
                parser.VersionDependencies = [];

                Models.ELFSectionHeader? verneedSection = FindSectionByAddress(parser, (ulong)verneedAddr);
                if (verneedSection != null)
                {
                    ParseVerNeedEntries(parser, verneedSection.Value, (int)verneedNum);
                }
            }
        }

        private static Models.ELFSectionHeader? FindSectionByAddress(ELFParser parser, ulong address)
        {
            if (parser.SectionHeaders == null)
            {
                return null;
            }

            foreach (Models.ELFSectionHeader section in parser.SectionHeaders)
            {
                if (section.sh_addr == address)
                {
                    return section;
                }
            }
            return null;
        }

        private static void ParseVerNeedEntries(ELFParser parser, Models.ELFSectionHeader section, int count)
        {
            if (parser.SectionHeaders == null || parser.VersionDependencies == null)
            {
                return;
            }

            // 找到版本需求字符串表
            int strTabIdx = (int)section.sh_link;
            if (strTabIdx >= parser.SectionHeaders.Count)
            {
                return;
            }

            Models.ELFSectionHeader strTabSection = parser.SectionHeaders[strTabIdx];
            byte[] strTabData = new byte[strTabSection.sh_size];
            Array.Copy(parser.FileData, (long)strTabSection.sh_offset, strTabData, 0, (int)strTabSection.sh_size);

            long offset = (long)section.sh_offset;
            int processed = 0;

            while (processed < count && offset < parser.FileData.Length)
            {
                if (!parser.Header.IsLittleEndian()) // 如果不是小端序
                {
                    Array.Reverse(parser.FileData, (int)offset, 2);
                    Array.Reverse(parser.FileData, (int)offset + 2, 2);
                    Array.Reverse(parser.FileData, (int)offset + 4, 4);
                    Array.Reverse(parser.FileData, (int)offset + 8, 4);
                    Array.Reverse(parser.FileData, (int)offset + 12, 4);
                }

                // 读取版本需求结构
                _ = BitConverter.ToUInt16(parser.FileData, (int)offset);
                ushort vn_cnt = BitConverter.ToUInt16(parser.FileData, (int)offset + 2);
                uint vn_file = BitConverter.ToUInt32(parser.FileData, (int)offset + 4);
                uint vn_aux = BitConverter.ToUInt32(parser.FileData, (int)offset + 8);
                uint vn_next = BitConverter.ToUInt32(parser.FileData, (int)offset + 12);

                // 获取库名称
                _ = ELFParserUtils.ExtractStringFromBytes(strTabData, (int)vn_file);

                long auxOffset = offset + vn_aux;
                int auxProcessed = 0;

                // 遍历辅助条目
                while (auxProcessed < vn_cnt && auxOffset < parser.FileData.Length)
                {
                    if (!parser.Header.IsLittleEndian()) // 如果不是小端序
                    {
                        Array.Reverse(parser.FileData, (int)auxOffset + 8, 4);
                        Array.Reverse(parser.FileData, (int)auxOffset + 6, 2);
                        Array.Reverse(parser.FileData, (int)auxOffset + 12, 4);
                    }

                    uint nameOffset = BitConverter.ToUInt32(parser.FileData, (int)auxOffset + 8);
                    ushort flags = BitConverter.ToUInt16(parser.FileData, (int)auxOffset + 6);
                    uint auxNext = BitConverter.ToUInt32(parser.FileData, (int)auxOffset + 12);
                    string versionName = ELFParserUtils.ExtractStringFromBytes(strTabData, (int)nameOffset);

                    // 使用版本索引作为键，而不是顺序
                    ushort verIndex = (ushort)(flags & 0x7fff); // 去除隐藏标志
                    parser.VersionDependencies.TryAdd(verIndex, versionName);
                    auxProcessed++;
                    if (auxNext == 0)
                    {
                        break;
                    }

                    auxOffset += auxNext;
                }

                offset += vn_next; // 移动到下一个版本需求
                processed++;

                if (vn_next == 0)
                {
                    break; // 没有更多版本需求
                }
            }
        }
    }
}