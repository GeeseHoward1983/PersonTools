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

                Models.ELFSectionHeader? verneedSection = ELFParserUtils.FindSectionByAddress(parser, (ulong)verneedAddr);
                if (verneedSection != null)
                {
                    ParseVerNeedEntries(parser, verneedSection.Value, (int)verneedNum);
                }
            }
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

            byte[] strTabData = parser.GetSectionData(strTabIdx);
            bool isLittleEndian = parser.Header.IsLittleEndian();

            long offset = (long)section.sh_offset;
            int processed = 0;

            while (processed < count && offset < parser.FileData.Length)
            {
                // 读取版本需求结构（vn_cnt、vn_aux、vn_next）
                ushort vn_cnt = ELFParserUtils.ReadUInt16(parser.FileData, (int)offset + 2, isLittleEndian);
                uint vn_aux = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset + 8, isLittleEndian);
                uint vn_next = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset + 12, isLittleEndian);

                long auxOffset = offset + vn_aux;
                int auxProcessed = 0;

                // 遍历辅助条目
                while (auxProcessed < vn_cnt && auxOffset < parser.FileData.Length)
                {
                    ushort flags = ELFParserUtils.ReadUInt16(parser.FileData, (int)auxOffset + 6, isLittleEndian);
                    uint nameOffset = ELFParserUtils.ReadUInt32(parser.FileData, (int)auxOffset + 8, isLittleEndian);
                    uint auxNext = ELFParserUtils.ReadUInt32(parser.FileData, (int)auxOffset + 12, isLittleEndian);
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