using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFSymbolDisplayNameResolver
    {
        internal static string Resolve(ELFParser parser, ELFSymbol symbol, SectionType type, int index)
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

    }
}
