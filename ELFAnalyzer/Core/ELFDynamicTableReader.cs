using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFDynamicTableReader
    {
        internal static IEnumerable<ELFDynamic> GetEntries(ELFParser parser)
        {
            return parser.DynamicEntries?.Take(GetEntryCount(parser.DynamicEntries)) ?? [];
        }

        private static int GetEntryCount(List<ELFDynamic> entries)
        {
            return GetCountThroughTerminator(entries, entries.FindIndex(entry => entry.d_tag == (long)DynamicTag.DT_NULL));
        }

        private static int GetCountThroughTerminator(List<ELFDynamic> entries, int index) => index >= 0 ? index + 1 : entries.Count;

        internal static ELFDynamicStringData ReadString(ELFParser parser, ulong offset)
        {
            return HasStringTable(parser) ? ReadStringTable(parser, offset) : new ELFDynamicStringData(false, string.Empty);
        }

        private static bool HasStringTable(ELFParser parser)
        {
            return parser.SectionHeaders != null && GetEntryValue(parser, DynamicTag.DT_STRTAB) != 0
                && GetEntryValue(parser, DynamicTag.DT_STRSZ) != 0;
        }

        private static ulong GetEntryValue(ELFParser parser, DynamicTag tag)
        {
            return GetValue(parser.DynamicEntries?.FirstOrDefault(entry => entry.d_tag == (long)tag) ?? default);
        }

        private static ulong GetValue(ELFDynamic entry) => entry.d_tag != 0 ? entry.d_val : 0;

        private static ELFDynamicStringData ReadStringTable(ELFParser parser, ulong offset)
        {
            return ReadStringSection(parser, ELFParserUtils.FindSectionByAddress(parser, GetEntryValue(parser, DynamicTag.DT_STRTAB)), offset);
        }

        private static ELFDynamicStringData ReadStringSection(ELFParser parser, ELFSectionHeader? section, ulong offset)
        {
            return section.HasValue ? new ELFDynamicStringData(true, ReadStringAtOffset(parser, section.Value, offset))
                : new ELFDynamicStringData(false, string.Empty);
        }

        private static string ReadStringAtOffset(ELFParser parser, ELFSectionHeader section, ulong offset)
        {
            return IsReadableStringOffset(parser, section, offset)
                ? ELFParserUtils.ExtractStringFromBytes(parser.FileData, (int)(section.sh_offset + offset)) : string.Empty;
        }

        private static bool IsReadableStringOffset(ELFParser parser, ELFSectionHeader section, ulong offset)
        {
            // 减法式范围检查，避免 sh_offset + d_val 发生 ulong 回绕。
            return parser.FileData != null && section.sh_offset < (ulong)parser.FileData.Length
                && offset < (ulong)parser.FileData.Length - section.sh_offset;
        }
    }
}
