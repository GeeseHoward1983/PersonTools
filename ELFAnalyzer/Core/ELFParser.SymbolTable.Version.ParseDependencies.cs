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

            WalkVerneed(parser, (long)section.sh_offset, count, isLittleEndian,
                onVerneed: (_, _) => { },
                onVernaux: auxOffset =>
                {
                    ushort flags = ELFParserUtils.ReadUInt16(parser.FileData, (int)auxOffset + 6, isLittleEndian);
                    uint nameOffset = ELFParserUtils.ReadUInt32(parser.FileData, (int)auxOffset + 8, isLittleEndian);
                    string versionName = ELFParserUtils.ExtractStringFromBytes(strTabData, (int)nameOffset);

                    // 使用版本索引作为键，而不是顺序
                    ushort verIndex = (ushort)(flags & 0x7fff); // 去除隐藏标志
                    parser.VersionDependencies.TryAdd(verIndex, versionName);
                });
        }

        // 遍历 verneed(.gnu.version_r) 链表：对每个 verneed 项回调 onVerneed(项文件偏移, vn_cnt)，
        // 再对其下每个 vernaux 项回调 onVernaux(辅助项文件偏移)。maxCount<=0 表示不限项数（仅靠 vn_next==0/越界停止）。
        // 返回已处理的 verneed 项数。链表步进(+2 vn_cnt / +8 vn_aux / +12 vn_next / 辅助项 +12 vna_next)与边界在此单点维护，
        // 供版本依赖的“解析填表”与“格式化输出”两路复用。
        private static int WalkVerneed(ELFParser parser, long sectionStart, int maxCount, bool isLittleEndian,
            Action<long, ushort> onVerneed, Action<long> onVernaux)
        {
            long offset = sectionStart;
            int processed = 0;

            while ((maxCount <= 0 || processed < maxCount) && offset < parser.FileData.Length)
            {
                ushort vn_cnt = ELFParserUtils.ReadUInt16(parser.FileData, (int)offset + 2, isLittleEndian);
                uint vn_aux = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset + 8, isLittleEndian);
                uint vn_next = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset + 12, isLittleEndian);

                onVerneed(offset, vn_cnt);

                long auxOffset = offset + vn_aux;
                int auxProcessed = 0;
                while (auxProcessed < vn_cnt && auxOffset < parser.FileData.Length)
                {
                    uint auxNext = ELFParserUtils.ReadUInt32(parser.FileData, (int)auxOffset + 12, isLittleEndian);
                    onVernaux(auxOffset);
                    auxProcessed++;
                    if (auxNext == 0)
                    {
                        break;
                    }

                    auxOffset += auxNext;
                }

                processed++;
                if (vn_next == 0)
                {
                    break; // 没有更多版本需求
                }

                offset += vn_next; // 移动到下一个版本需求
            }

            return processed;
        }
    }
}