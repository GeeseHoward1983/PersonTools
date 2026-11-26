using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;

namespace PersonalTools.ELFAnalyzer.Presentation
{
    internal static class ELFGotPresentation
    {
        internal static List<ELFGotInfo> GetGotInfoList(ELFParser parser, string name)
        {
            return ELFGotReader.ReadSection(parser, name).Select(entry => CreateInfo(parser, name, entry)).ToList();
        }

        private static ELFGotInfo CreateInfo(ELFParser parser, string name, ELFGotEntryData entry)
        {
            return CreateInfo(parser, name, entry, ResolveRelocation(parser, entry));
        }

        private static ELFGotInfo CreateInfo(ELFParser parser, string name, ELFGotEntryData entry, (string Type, string Symbol) relocation)
        {
            return new ELFGotInfo
            {
                Number = ELFValueFormatting.Number((ulong)entry.Index),
                Offset = ELFValueFormatting.Address(entry.Address, parser.Is64Bit),
                Value = FormatValue(entry.Value, parser.Is64Bit),
                Type = relocation.Type,
                Symbol = relocation.Symbol,
                SectionName = name
            };
        }

        private static string FormatValue(ulong? value, bool is64Bit)
        {
            return value.HasValue ? ELFValueFormatting.Address(value.Value, is64Bit) : string.Empty;
        }

        private static (string Type, string Symbol) ResolveRelocation(ELFParser parser, ELFGotEntryData entry)
        {
            return entry.Relocation.HasValue ? GetRelocation(parser, entry.Relocation.Value)
                : (entry.IsReserved ? "保留" : string.Empty, string.Empty);
        }

        private static (string Type, string Symbol) GetRelocation(ELFParser parser, ELFRelocationReference reference)
        {
            return (ELFRelocation.GetRelocationTypeName(reference.Entry.Type, reference.Section.Machine) ?? "",
                ELFRelocationPresentation.ResolveSymbol(parser, reference.Section, reference.Entry.SymbolIndex).Name);
        }
    }
}
