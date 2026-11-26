using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Models
{
    // 原始重定位数据，不携带 UI 列宽、十六进制文本或符号显示标签。
    internal readonly record struct ELFRelocationEntryData(ulong Offset, ulong Info, uint SymbolIndex, uint Type, long? Addend);

    internal sealed record ELFRelocationSectionData(string Name, bool Is64Bit, ushort Machine,
        SectionType SymbolType, List<ELFSymbol> Symbols, List<ELFRelocationEntryData> Entries);

    internal readonly record struct ELFRelocationReference(ELFRelocationEntryData Entry, ELFRelocationSectionData Section);
}
