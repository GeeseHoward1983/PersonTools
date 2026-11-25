using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.Globalization;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    // 统一显示规则，供程序头、节头、符号、重定位和 GOT 的行转换复用。
    internal static class ELFDisplayFormatting
    {
        internal static string Number(ulong value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        internal static string Hex(ulong value, bool is64Bit)
        {
            return value.ToString(is64Bit ? "x16" : "x8", CultureInfo.InvariantCulture);
        }

        internal static string Address(ulong value, bool is64Bit)
        {
            return $"0x{Hex(value, is64Bit)}";
        }

        internal static string DynamicTagValue(long value, bool is64Bit)
        {
            return "0x" + value.ToString(is64Bit ? "x16" : "x8", CultureInfo.InvariantCulture);
        }

        internal static string SymbolName(ELFParser parser, ELFSymbol symbol, SectionType type, int index)
        {
            return ResolveSectionSymbolName(parser, symbol, ELFSymbolNameResolver.GetSymbolName(parser, symbol, type, index));
        }

        private static string ResolveSectionSymbolName(ELFParser parser, ELFSymbol symbol, string name)
        {
            return string.IsNullOrEmpty(name) && IsOrdinarySectionSymbol(symbol)
                ? ELFSymbolNameResolver.GetSectionName(parser, symbol.StShndx) : name;
        }

        private static bool IsOrdinarySectionSymbol(ELFSymbol symbol)
        {
            return (symbol.StInfo & ELFConstants.ST_TYPE_MASK) == (byte)SymbolType.STT_SECTION
                && symbol.StShndx > 0 && symbol.StShndx < ELFConstants.SHN_LORESERVE;
        }

        internal static int FindSectionIndex(ELFParser parser, Func<int, bool> matches)
        {
            return Enumerable.Range(0, parser.SectionHeaders?.Count ?? 0).FirstOrDefault(matches, -1);
        }
    }
}
