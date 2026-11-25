using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class DynamicHelper
    {
        private static string GetDynamicSectionInfoTableEntryValue(ELFParser parser, ELFDynamic entry)
        {
            return HasDynamicStringTable(parser)
                ? ReadDynamicString(parser, ELFParserUtils.FindSectionByAddress(parser, GetStringValueFromDynamicEntries(parser, DynamicTag.DT_STRTAB)), entry.d_val)
                : string.Empty;
        }

        private static bool HasDynamicStringTable(ELFParser parser)
        {
            return parser.SectionHeaders != null
                && GetStringValueFromDynamicEntries(parser, DynamicTag.DT_STRTAB) != 0
                && GetStringValueFromDynamicEntries(parser, DynamicTag.DT_STRSZ) != 0;
        }

        private static string ReadDynamicString(ELFParser parser, ELFSectionHeader? section, ulong offset)
        {
            return section.HasValue ? ReadStringFromSection(parser, section.Value, offset) : string.Empty;
        }

        private static string GetDynamicSectionInfoValue(ELFParser parser, ELFDynamic entry)
        {
            return entry.d_tag switch
            {
                (long)DynamicTag.DT_NEEDED or (long)DynamicTag.DT_SONAME or (long)DynamicTag.DT_RPATH or (long)DynamicTag.DT_RUNPATH => GetDynamicSectionInfoTableEntryValue(parser, entry),
                // DT_FLAGS 和 DT_FLAGS_1 分别解码 DF_* 与 DF_1_*，不能混用。
                (long)DynamicTag.DT_FLAGS => ELFDynamicInfo.GetDynamicFlagDescription((uint)entry.d_val),
                (long)DynamicTag.DT_FLAGS_1 => ELFDynamicInfo.GetDynamicFlag1Description((uint)entry.d_val),
                _ => entry.d_tag == (long)DynamicTag.DT_PLTREL ? ELFParserUtils.GetTypeName(typeof(DynamicTag), entry.d_val, "") : $"0x{entry.d_val:x}"
            };
        }

        internal static List<ELFDynamicSectionInfo> GetDynamicSectionInfoList(ELFParser parser)
        {
            return parser.DynamicEntries?.Take(GetDynamicEntryCount(parser.DynamicEntries))
                .Select(entry => CreateDynamicInfo(parser, entry)).ToList() ?? [];
        }

        private static int GetDynamicEntryCount(List<ELFDynamic> entries)
        {
            return GetCountThroughTerminator(entries, entries.FindIndex(entry => entry.d_tag == (long)DynamicTag.DT_NULL));
        }

        private static int GetCountThroughTerminator(List<ELFDynamic> entries, int index)
        {
            return index >= 0 ? index + 1 : entries.Count;
        }

        private static ELFDynamicSectionInfo CreateDynamicInfo(ELFParser parser, ELFDynamic entry)
        {
            return new ELFDynamicSectionInfo
            {
                Tag = ELFDisplayFormatting.DynamicTagValue(entry.d_tag, parser.Is64Bit),
                Type = ELFDynamicInfo.GetDynamicTagDescription((ulong)entry.d_tag),
                Value = GetDynamicSectionInfoValue(parser, entry)
            };
        }

        private static ulong GetStringValueFromDynamicEntries(ELFParser parser, DynamicTag tag)
        {
            return GetDynamicEntryValue(parser.DynamicEntries?.FirstOrDefault(entry => entry.d_tag == (long)tag) ?? default);
        }

        private static ulong GetDynamicEntryValue(ELFDynamic entry)
        {
            return entry.d_tag != 0 ? entry.d_val : 0;
        }

        private static string ReadStringFromSection(ELFParser parser, ELFSectionHeader section, ulong offset)
        {
            return IsReadableStringOffset(parser, section, offset)
                ? StringOrOffset(ELFParserUtils.ExtractStringFromBytes(parser.FileData, (int)(section.sh_offset + offset)), offset)
                : $"0x{offset:x}";
        }

        private static bool IsReadableStringOffset(ELFParser parser, ELFSectionHeader section, ulong offset)
        {
            // 减法式边界校验，避免不可信的 sh_offset + d_val 发生 ulong 回绕。
            return parser.FileData != null && section.sh_offset < (ulong)parser.FileData.Length
                && offset < (ulong)parser.FileData.Length - section.sh_offset;
        }

        private static string StringOrOffset(string value, ulong offset)
        {
            return string.IsNullOrEmpty(value) ? $"0x{offset:x}" : value;
        }
    }
}
