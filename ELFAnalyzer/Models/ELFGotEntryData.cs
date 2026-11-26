
namespace PersonalTools.ELFAnalyzer.Models
{
    internal readonly record struct ELFGotEntryData(int Index, ulong Address, ulong? Value, bool IsReserved,
        ELFRelocationReference? Relocation);
}
