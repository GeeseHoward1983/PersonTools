using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.ELFAnalyzer.Presentation;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    // 兼容界面调用入口；解析由 Core 承担，显示转换由 Presentation 承担。
    internal static class ELFDisplayFormatting
    {
        internal static string Number(ulong value) => ELFValueFormatting.Number(value);

        internal static string Hex(ulong value, bool is64Bit) => ELFValueFormatting.Hex(value, is64Bit);

        internal static string Address(ulong value, bool is64Bit) => ELFValueFormatting.Address(value, is64Bit);

        internal static string DynamicTagValue(long value, bool is64Bit) => ELFValueFormatting.DynamicTagValue(value, is64Bit);

        internal static string SymbolName(ELFParser parser, ELFSymbol symbol, SectionType type, int index) => ELFSymbolDisplayNameResolver.Resolve(parser, symbol, type, index);

        internal static int FindSectionIndex(ELFParser parser, Func<int, bool> matches) => ELFSectionLocator.FindIndex(parser, matches);
    }
}
