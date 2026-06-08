using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.Globalization;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static partial class VersionSymbleTable
    {
        private static void ParseVersionDefinitions(ELFParser parser)
        {
            // 查找版本定义 (DT_VERDEF)
            long verdefAddr = 0;
            long verdefNum = 0;

            if (parser.DynamicEntries != null)
            {
                foreach (ELFDynamic entry in parser.DynamicEntries)
                {
                    if (entry.d_tag == (long)DynamicTag.DT_VERDEF)
                    {
                        verdefAddr = (long)entry.d_val;
                    }
                    else if (entry.d_tag == (long)DynamicTag.DT_VERDEFNUM)
                    {
                        verdefNum = (long)entry.d_val;
                    }
                }
            }

            if (verdefAddr > 0 && verdefNum > 0)
            {
                parser.VersionDefinitions = [];

                Models.ELFSectionHeader? verdefSection = FindSectionByAddress(parser, (ulong)verdefAddr);
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

        private static int CalculateVerDefEntryCount(Models.ELFSectionHeader section)
        {
            return section.sh_entsize == 0 ? 0 : (int)(section.sh_size / section.sh_entsize);
        }

        private static void ParseVerDefEntries(ELFParser parser, Models.ELFSectionHeader section, int count)
        {
            if (parser.SectionHeaders == null || parser.VersionDefinitions == null)
            {
                return;
            }

            // 找到版本定义字符串表
            int strTabIdx = (int)section.sh_link;
            if (strTabIdx >= parser.SectionHeaders.Count)
            {
                return;
            }

            byte[] strTabData = parser.GetSectionData(strTabIdx);

            bool isLittleEndian = parser.Header.IsLittleEndian();
            ulong offset = section.sh_offset;
            int processed = 0;

            while (processed < count && offset < (ulong)parser.FileData.Length)
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

        internal static string GetFormattedVersionDefinitionInfo(ELFParser parser)
        {
            if (parser.VersionDefinitions == null || parser.VersionDefinitions.Count == 0)
            {
                return "";
            }

            // 获取.gnu.version_d节的信息
            Models.ELFSectionHeader? verdefSection = parser.SectionHeaders?.Find(sh => sh.sh_type == (uint)SectionType.SHT_GNU_verdef);
            if (verdefSection == null)
            {
                return "";
            }

            Models.ELFSectionHeader vd = verdefSection.Value;
            StringBuilder sb = new();
            sb.AppendLine(CultureInfo.InvariantCulture, $"Version definition section '.gnu.version_d' contains {vd.sh_info} entries:");

            int entryIndex = 0;
            foreach (KeyValuePair<ushort, string> kvp in parser.VersionDefinitions.OrderBy(k => k.Key))
            {
                bool isBase = kvp.Key == 1;
                string flags = isBase ? "BASE" : "";
                ulong entryDelta = isBase ? 0UL : (ulong)entryIndex * vd.sh_entsize;
                ulong addr = vd.sh_addr + entryDelta;
                ulong fileOffset = vd.sh_offset + entryDelta;
                sb.AppendLine(CultureInfo.InvariantCulture, $"  地址：0x{addr:x8}  Offset: 0x{fileOffset:x6}  Link: {vd.sh_link} (.dynstr)  {entryIndex:D4}: Rev: 1  Flags: {flags,-6}   Index: {kvp.Key}  Cnt: 1  名称：{kvp.Value}");
                entryIndex++;
            }

            // 检查是否超出范围（参考 readelf："Version definition past end of section"）
            if (parser.VersionDefinitions.Count > CalculateVerDefEntryCount(vd))
            {
                sb.AppendLine("  Version definition past end of section");
            }

            return sb.ToString();
        }
    }
}