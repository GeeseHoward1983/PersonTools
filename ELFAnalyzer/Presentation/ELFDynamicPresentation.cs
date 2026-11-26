using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Presentation
{
    internal static class ELFDynamicPresentation
    {
        internal static List<ELFDynamicSectionInfo> GetDynamicSectionInfoList(ELFParser parser)
        {
            return ELFDynamicTableReader.GetEntries(parser).Select(entry => CreateDynamicInfo(parser, entry)).ToList();
        }

        private static ELFDynamicSectionInfo CreateDynamicInfo(ELFParser parser, ELFDynamic entry)
        {
            return new ELFDynamicSectionInfo
            {
                Tag = ELFValueFormatting.DynamicTagValue(entry.d_tag, parser.Is64Bit),
                Type = ELFDynamicInfo.GetDynamicTagDescription((ulong)entry.d_tag),
                Value = GetValue(parser, entry)
            };
        }

        private static string GetValue(ELFParser parser, ELFDynamic entry)
        {
            return entry.d_tag switch
            {
                (long)DynamicTag.DT_NEEDED or (long)DynamicTag.DT_SONAME or (long)DynamicTag.DT_RPATH or (long)DynamicTag.DT_RUNPATH => GetStringValue(parser, entry),
                (long)DynamicTag.DT_FLAGS => ELFDynamicInfo.GetDynamicFlagDescription((uint)entry.d_val),
                (long)DynamicTag.DT_FLAGS_1 => ELFDynamicInfo.GetDynamicFlag1Description((uint)entry.d_val),
                _ => entry.d_tag == (long)DynamicTag.DT_PLTREL ? ELFParserUtils.GetTypeName(typeof(DynamicTag), entry.d_val, "") : $"0x{entry.d_val:x}"
            };
        }

        private static string GetStringValue(ELFParser parser, ELFDynamic entry)
        {
            return FormatStringValue(ELFDynamicTableReader.ReadString(parser, entry.d_val), entry.d_val);
        }

        private static string FormatStringValue(ELFDynamicStringData value, ulong offset)
        {
            return value.HasTable ? StringOrOffset(value.Value, offset) : string.Empty;
        }

        private static string StringOrOffset(string value, ulong offset) => string.IsNullOrEmpty(value) ? $"0x{offset:x}" : value;
    }
}
