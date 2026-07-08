using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static partial class VersionSymbolParser
    {
        private static void ParseVersionDependencies(ELFParser parser)
        {
            // 查找版本依赖 (DT_VERNEED)
            long verneedAddr = FindDynamicValue(parser, DynamicTag.DT_VERNEED);
            long verneedNum = FindDynamicValue(parser, DynamicTag.DT_VERNEEDNUM);

            if (verneedAddr > 0 && verneedNum > 0)
            {
                parser.VersionDependencies = [];

                Models.ELFSectionHeader? verneedSection = ELFParserUtils.FindSectionByAddress(parser, (ulong)verneedAddr);
                if (verneedSection != null)
                {
                    // verneedNum 来自不可信 DT_VERNEEDNUM；夹到 [0, int.MaxValue] 而非直接 (int) 截断，
                    // 与 ParseVersionDefinitions 对 verdefNum 的处理一致，否则 >int.MaxValue 时截成负值会被
                    // WalkVerneed 当作"不限项数"。ParseVerNeedEntries 内另有节边界兜底。
                    int count = verneedNum > int.MaxValue ? int.MaxValue : (int)verneedNum;
                    ParseVerNeedEntries(parser, verneedSection.Value, count);
                }
            }
        }

        private static void ParseVerNeedEntries(ELFParser parser, Models.ELFSectionHeader section, int count)
        {
            if (parser.VersionDependencies == null ||
                !ELFParserUtils.TryGetLinkedStringTable(parser, section, out byte[] strTabData, out bool isLittleEndian))
            {
                return;
            }

            WalkVerneed(parser, (long)section.sh_offset, GetSectionEndOffset(parser, section), count, isLittleEndian,
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

        // verneed/verdef 节的文件内结束偏移，夹到文件长度；sh_offset+sh_size 回绕或越界时退回文件末尾。
        // 供版本遍历把 offset/auxOffset 硬夹在本节内，防止链表小步进跨越整份文件。
        internal static long GetSectionEndOffset(ELFParser parser, Models.ELFSectionHeader section)
        {
            ulong end = section.sh_offset + section.sh_size;
            if (end < section.sh_offset || end > (ulong)parser.FileData.Length)
            {
                return parser.FileData.Length;
            }

            return (long)end;
        }

        // 遍历 verneed(.gnu.version_r) 链表：对每个 verneed 项回调 onVerneed(项文件偏移, vn_cnt)，
        // 再对其下每个 vernaux 项回调 onVernaux(辅助项文件偏移)。maxCount<=0 表示不限项数（仅靠 vn_next==0/越界停止）。
        // 返回已处理的 verneed 项数。链表步进(+2 vn_cnt / +8 vn_aux / +12 vn_next / 辅助项 +12 vna_next)与边界在此单点维护，
        // 供版本依赖的“解析填表”与“格式化输出”两路复用。
        internal static int WalkVerneed(ELFParser parser, long sectionStart, long sectionEnd, int maxCount, bool isLittleEndian,
            Action<long, ushort> onVerneed, Action<long> onVernaux)
        {
            // 把遍历硬夹在 verneed 节 [sectionStart, sectionEnd) 内，并对 vernaux 总步数设全局硬上界，
            // 防畸形 DT_VERNEEDNUM / vn_cnt(≤65535) / 小步进 vn_next、vna_next 构成 O(N×M) CPU/内存耗尽
            // （与 exidx 未命中回扫的双上界修复同款）。
            const int MaxTotalVernaux = 1_000_000;
            // 外层 verneed 项数硬上界（与 ParseVerDefEntries 的 MaxVerDefEntries 同款）：
            // maxCount<=0（格式化路径）且 vn_cnt=0 时 MaxTotalVernaux 不生效，畸形 vn_next=1 可小步进迭代至节尾
            const int MaxVerneedEntries = 1_000_000;

            if (sectionEnd > parser.FileData.Length)
            {
                sectionEnd = parser.FileData.Length;
            }

            long offset = sectionStart;
            int processed = 0;
            int totalAux = 0;

            while ((maxCount <= 0 || processed < maxCount) && processed < MaxVerneedEntries && offset + 16 <= sectionEnd)
            {
                ushort vn_cnt = ELFParserUtils.ReadUInt16(parser.FileData, (int)offset + 2, isLittleEndian);
                uint vn_aux = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset + 8, isLittleEndian);
                uint vn_next = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset + 12, isLittleEndian);

                onVerneed(offset, vn_cnt);

                long auxOffset = offset + vn_aux;
                int auxProcessed = 0;
                while (auxProcessed < vn_cnt && auxOffset + 16 <= sectionEnd)
                {
                    if (totalAux >= MaxTotalVernaux)
                    {
                        return processed; // 达全局 vernaux 步数上界，畸形输入下提前收尾，避免挂死
                    }

                    uint auxNext = ELFParserUtils.ReadUInt32(parser.FileData, (int)auxOffset + 12, isLittleEndian);
                    onVernaux(auxOffset);
                    auxProcessed++;
                    totalAux++;
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