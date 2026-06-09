using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static partial class VersionSymbolParser
    {
        private static void ParseVersionDefinitions(ELFParser parser)
        {
            // 查找版本定义 (DT_VERDEF)
            long verdefAddr = FindDynamicValue(parser, DynamicTag.DT_VERDEF);
            long verdefNum = FindDynamicValue(parser, DynamicTag.DT_VERDEFNUM);

            if (verdefAddr > 0 && verdefNum > 0)
            {
                parser.VersionDefinitions = [];

                Models.ELFSectionHeader? verdefSection = ELFParserUtils.FindSectionByAddress(parser, (ulong)verdefAddr);
                if (verdefSection != null)
                {
                    ParseVerDefEntries(parser, verdefSection.Value, (int)verdefNum);
                }
            }
            else
            {
                // 如果动态段中没有找到版本定义信息，则直接查找SHT_GNU_VERDEF类型的节
                FindAndParseVersionDefinitionSection(parser);
            }
        }

        private static void FindAndParseVersionDefinitionSection(ELFParser parser)
        {
            if (parser.SectionHeaders == null)
            {
                return;
            }

            // 遍历所有节头查找SHT_GNU_VERDEF类型的节（即.gnu.version_d）
            for (int i = 0; i < parser.SectionHeaders.Count; i++)
            {
                Models.ELFSectionHeader section = parser.SectionHeaders[i];
                if (section.sh_type == (uint)SectionType.SHT_GNU_verdef)
                {
                    ParseVerDefEntries(parser, section, CalculateVerDefEntryCount(section));
                }
            }
        }

        internal static int CalculateVerDefEntryCount(Models.ELFSectionHeader section)
        {
            return section.sh_entsize == 0 ? 0 : (int)(section.sh_size / section.sh_entsize);
        }

        private static void ParseVerDefEntries(ELFParser parser, Models.ELFSectionHeader section, int count)
        {
            if (parser.VersionDefinitions == null ||
                !ELFParserUtils.TryGetLinkedStringTable(parser, section, out byte[] strTabData, out bool isLittleEndian))
            {
                return;
            }

            ulong offset = section.sh_offset;
            int processed = 0;

            while (processed < count && offset + 20 <= (ulong)parser.FileData.Length)
            {
                ushort vd_ndx = ELFParserUtils.ReadUInt16(parser.FileData, (int)offset + 4, isLittleEndian);
                // Verdef 结构与位宽无关：vd_aux(+12)、vd_next(+16) 均为 4 字节
                uint vd_aux = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset + 12, isLittleEndian);
                uint vd_next = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset + 16, isLittleEndian);

                // Verdaux 紧随其后：vda_name(+0) 为版本名在字符串表中的偏移
                ulong nameOffset = offset + vd_aux;
                uint nameOffsetInStrTab = ELFParserUtils.ReadUInt32(parser.FileData, (int)nameOffset, isLittleEndian);
                string versionName = ELFParserUtils.ExtractStringFromBytes(strTabData, (int)nameOffsetInStrTab);

                // 存储版本定义
                ushort index = (ushort)(vd_ndx & 0x7fff); // 去除隐藏标志
                parser.VersionDefinitions.TryAdd(index, versionName);

                // 如果没有更多版本定义或者偏移量为0，则退出循环
                if (vd_next == 0 || offset + vd_next >= (ulong)parser.FileData.Length)
                {
                    break;
                }

                offset += vd_next; // 移动到下一个版本定义
                processed++;
            }
        }
    }
}