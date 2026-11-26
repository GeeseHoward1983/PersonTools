using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.Globalization;

namespace PersonalTools.ELFAnalyzer.Presentation
{
    internal static class ELFRelocationPresentation
    {
        internal static List<ELFRelocationInfo> GetRelocationInfoForSpecificSection(ELFParser parser, string name)
        {
            return FormatSection(parser, ELFRelocationReader.ReadSection(parser, name));
        }

        private static List<ELFRelocationInfo> FormatSection(ELFParser parser, ELFRelocationSectionData? section)
        {
            return section == null ? [] : FormatEntries(parser, section);
        }

        private static List<ELFRelocationInfo> FormatEntries(ELFParser parser, ELFRelocationSectionData section)
        {
            return section.Entries.Select(entry => CreateInfo(parser, section, entry)).ToList();
        }

        private static ELFRelocationInfo CreateInfo(ELFParser parser, ELFRelocationSectionData section, ELFRelocationEntryData entry)
        {
            return CreateInfo(section, entry, ResolveSymbol(parser, section, entry.SymbolIndex));
        }

        private static ELFRelocationInfo CreateInfo(ELFRelocationSectionData section, ELFRelocationEntryData entry,
            (string Name, string Value) symbol)
        {
            return new ELFRelocationInfo
            {
                Offset = ELFValueFormatting.Hex(entry.Offset, section.Is64Bit),
                Info = ELFValueFormatting.Hex(entry.Info, section.Is64Bit),
                Type = ELFRelocation.GetRelocationTypeName(entry.Type, section.Machine) ?? "",
                SymbolValue = symbol.Value.PadLeft(16),
                Symbol = symbol.Name,
                Addend = entry.Addend.HasValue ? entry.Addend.Value.ToString(CultureInfo.InvariantCulture) : "",
                SectionName = section.Name
            };
        }

        internal static (string Name, string Value) ResolveSymbol(ELFParser parser, ELFRelocationSectionData section, uint index)
        {
            return index < section.Symbols.Count ? GetSymbol(parser, section.Symbols[(int)index], (int)index, section.SymbolType)
                : (string.Empty, ELFValueFormatting.Hex(0, section.Is64Bit));
        }

        private static (string Name, string Value) GetSymbol(ELFParser parser, ELFSymbol symbol, int index, SectionType type)
        {
            return (ELFSymbolDisplayNameResolver.Resolve(parser, symbol, type, index), ELFValueFormatting.Hex(symbol.StValue, parser.Is64Bit));
        }
    }
}
