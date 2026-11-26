using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Presentation
{
    internal static class ELFSymbolPresentation
    {
        private static readonly string[] SymbolPrefixes = ["STT_", "STB_", "STV_"];

        internal static List<ELFSymbolTableInfo> GetSymbolTableInfoList(ELFParser parser, SectionType sectionType)
        {
            return parser.Symbols?.GetValueOrDefault(sectionType)
                ?.Select((symbol, index) => CreateSymbolInfo(parser, symbol, sectionType, index)).ToList() ?? [];
        }

        private static ELFSymbolTableInfo CreateSymbolInfo(ELFParser parser, ELFSymbol symbol, SectionType sectionType, int index)
        {
            return new ELFSymbolTableInfo
            {
                Number = index,
                Value = ELFValueFormatting.Address(symbol.StValue, parser.Is64Bit),
                Size = $"{symbol.StSize}",
                Type = StripSymbolPrefix(ELFSymbolInfo.GetSymbolType(symbol.StInfo)),
                Bind = StripSymbolPrefix(ELFSymbolInfo.GetSymbolBinding(symbol.StInfo)),
                Vis = StripSymbolPrefix(ELFSymbolInfo.GetSymbolVisibility(symbol.StOther)),
                Ndx = GetSectionIndexName(symbol.StShndx),
                Name = ELFSymbolDisplayNameResolver.Resolve(parser, symbol, sectionType, index)
            };
        }

        private static string GetSectionIndexName(ushort index)
        {
            return index switch
            {
                ELFConstants.SHN_UNDEF => "UND",
                ELFConstants.SHN_ABS => "ABS",
                ELFConstants.SHN_COMMON => "COM",
                _ => $"{index}"
            };
        }

        // 去掉 STT_/STB_/STV_ 前缀，与 readelf 输出一致。
        private static string StripSymbolPrefix(string value)
        {
            return HasSymbolPrefix(value) ? value[4..] : value;
        }

        private static bool HasSymbolPrefix(string value)
        {
            return value.Length > 4 && SymbolPrefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));
        }
    }
}
