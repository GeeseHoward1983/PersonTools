using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.Globalization;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class RelocationHelper
    {
        private sealed record RelocationContext(ELFParser Parser, ELFSectionHeader Section, string Name,
            byte[] Data, List<ELFSymbol> Symbols, SectionType SymbolType);

        internal static List<ELFRelocationInfo> GetRelocationInfoForSpecificSection(ELFParser parser, string sectionName)
        {
            return ReadRelocationSection(parser, FindRelocationSection(parser, sectionName), sectionName);
        }

        private static int FindRelocationSection(ELFParser parser, string sectionName)
        {
            // 同名但非 REL/RELA 的节不能遮挡后面的真正重定位节。
            return ELFDisplayFormatting.FindSectionIndex(parser,
                index => ELFSymbolNameResolver.GetSectionName(parser, index) == sectionName && IsRelocationSection(parser.SectionHeaders[index]));
        }

        private static bool IsRelocationSection(ELFSectionHeader section)
        {
            return section.sh_type is ((uint)SectionType.SHT_RELA) or ((uint)SectionType.SHT_REL);
        }

        private static List<ELFRelocationInfo> ReadRelocationSection(ELFParser parser, int index, string name)
        {
            // 先校验声明步长，保留零 sh_entsize 返回空列表的行为。
            return index >= 0 && parser.SectionHeaders[index].sh_entsize != 0
                ? ReadRelocationRows(CreateContext(parser, index, name)) : [];
        }

        private static RelocationContext CreateContext(ELFParser parser, int index, string name)
        {
            return new RelocationContext(parser, parser.SectionHeaders[index], name, parser.GetSectionData(index),
                ReadRelocationSymbols(parser, (int)parser.SectionHeaders[index].sh_link),
                ResolveSymbolTableType(parser, (int)parser.SectionHeaders[index].sh_link));
        }

        private static List<ELFRelocationInfo> ReadRelocationRows(RelocationContext context)
        {
            return Enumerable.Range(0, GetRelocationEntryCount(context))
                .Select(index => CreateRelocationInfo(context, ReadRelocationEntry(context, index))).ToList();
        }

        private static int GetRelocationEntryCount(RelocationContext context)
        {
            // 同时受声明条目数和实际字节数约束，步长按真实 sh_type 决定。
            return (int)Math.Min(context.Section.sh_size / context.Section.sh_entsize,
                (ulong)(context.Data.Length / GetRelocationEntrySize(context.Parser.Is64Bit, IsRela(context))));
        }

        private static bool IsRela(RelocationContext context)
        {
            return context.Section.sh_type == (uint)SectionType.SHT_RELA;
        }

        private static int GetRelocationEntrySize(bool is64Bit, bool isRela)
        {
            return is64Bit ? (isRela ? 24 : 16) : (isRela ? 12 : 8);
        }

        private static ELFRelocationInfo CreateRelocationInfo(RelocationContext context, (ulong Offset, ulong Info, long Addend) entry)
        {
            return ResolveRelocationInfo(context, entry, SplitRelocInfo(context.Parser, entry.Info));
        }

        private static ELFRelocationInfo ResolveRelocationInfo(RelocationContext context,
            (ulong Offset, ulong Info, long Addend) entry, (uint Symbol, uint Type) info)
        {
            return FormatRelocationInfo(context, entry, info.Type,
                ResolveRelocSymbol(context.Parser, context.Symbols, info.Symbol, context.SymbolType));
        }

        private static ELFRelocationInfo FormatRelocationInfo(RelocationContext context,
            (ulong Offset, ulong Info, long Addend) entry, uint type, (string Name, string Value) symbol)
        {
            return new ELFRelocationInfo
            {
                Offset = ELFDisplayFormatting.Hex(entry.Offset, context.Parser.Is64Bit),
                Info = ELFDisplayFormatting.Hex(entry.Info, context.Parser.Is64Bit),
                Type = ELFRelocation.GetRelocationTypeName(type, context.Parser.Header.e_machine) ?? "",
                SymbolValue = symbol.Value.PadLeft(16),
                Symbol = symbol.Name,
                Addend = IsRela(context) ? entry.Addend.ToString(CultureInfo.InvariantCulture) : "",
                SectionName = context.Name
            };
        }

        private static List<ELFSymbol> ReadRelocationSymbols(ELFParser parser, int index)
        {
            // 复用 Parser 的符号缓存；GOT 和多个重定位节不重复解析关联符号表。
            return parser.GetOrParseSymbols(index, () => ParseRelocationSymbols(parser, parser.GetSectionData(index)));
        }

        private static List<ELFSymbol> ParseRelocationSymbols(ELFParser parser, byte[] data)
        {
            return Enumerable.Range(0, data.Length / GetSymbolEntrySize(parser.Is64Bit))
                .Select(index => ReadSymbolAtIndex(parser, data, index)).ToList();
        }

        private static ELFSymbol ReadSymbolAtIndex(ELFParser parser, byte[] data, int index)
        {
            return ReadSymbol(parser, data, index * GetSymbolEntrySize(parser.Is64Bit));
        }

        private static int GetSymbolEntrySize(bool is64Bit)
        {
            return is64Bit ? 24 : 16;
        }

        private static ELFSymbol ReadSymbol(ELFParser parser, byte[] data, int offset)
        {
            return parser.Is64Bit ? ReadSymbol64(data, offset, parser.Header.IsLittleEndian())
                : ReadSymbol32(data, offset, parser.Header.IsLittleEndian());
        }

        private static ELFSymbol ReadSymbol64(byte[] d, int b, bool little) => new()
        {
            StName = ELFParserUtils.ReadUInt32(d, b, little),
            StInfo = d[b + 4],
            StOther = d[b + 5],
            StShndx = ELFParserUtils.ReadUInt16(d, b + 6, little),
            StValue = ELFParserUtils.ReadUInt64(d, b + 8, little),
            StSize = ELFParserUtils.ReadUInt64(d, b + 16, little)
        };

        private static ELFSymbol ReadSymbol32(byte[] d, int b, bool little) => new()
        {
            StName = ELFParserUtils.ReadUInt32(d, b, little),
            StValue = ELFParserUtils.ReadUInt32(d, b + 4, little),
            StSize = ELFParserUtils.ReadUInt32(d, b + 8, little),
            StInfo = d[b + 12],
            StOther = d[b + 13],
            StShndx = ELFParserUtils.ReadUInt16(d, b + 14, little)
        };

        private static (ulong Offset, ulong Info, long Addend) ReadRelocationEntry(RelocationContext context, int index)
        {
            return ReadRelocationEntryAtOffset(context,
                index * GetRelocationEntrySize(context.Parser.Is64Bit, IsRela(context)));
        }

        private static (ulong Offset, ulong Info, long Addend) ReadRelocationEntryAtOffset(RelocationContext context, int offset)
        {
            return context.Parser.Is64Bit ? ReadRelocation64(context, offset) : ReadRelocation32(context, offset);
        }

        private static (ulong Offset, ulong Info, long Addend) ReadRelocation64(RelocationContext context, int offset)
        {
            return (ELFParserUtils.ReadUInt64(context.Data, offset, context.Parser.Header.IsLittleEndian()),
                ELFParserUtils.ReadUInt64(context.Data, offset + 8, context.Parser.Header.IsLittleEndian()),
                IsRela(context) ? ELFParserUtils.ReadInt64(context.Data, offset + 16, context.Parser.Header.IsLittleEndian()) : -1);
        }

        private static (ulong Offset, ulong Info, long Addend) ReadRelocation32(RelocationContext context, int offset)
        {
            return (ELFParserUtils.ReadUInt32(context.Data, offset, context.Parser.Header.IsLittleEndian()),
                ELFParserUtils.ReadUInt32(context.Data, offset + 4, context.Parser.Header.IsLittleEndian()),
                IsRela(context) ? ELFParserUtils.ReadInt32(context.Data, offset + 8, context.Parser.Header.IsLittleEndian()) : -1);
        }

        private static (uint Symbol, uint Type) SplitRelocInfo(ELFParser parser, ulong info)
        {
            return parser.Is64Bit ? SplitRelocInfo64(parser, info) : ((uint)(info >> 8), (uint)(info & 0xff));
        }

        private static (uint Symbol, uint Type) SplitRelocInfo64(ELFParser parser, ulong info)
        {
            return parser.Header.e_machine is ((ushort)EMachine.EM_MIPS) or ((ushort)EMachine.EM_MIPS_RS3_LE)
                ? SplitMipsRelocInfo(parser.Header.IsLittleEndian(), info) : ((uint)(info >> 32), (uint)(info & 0xffffffff));
        }

        private static (uint Symbol, uint Type) SplitMipsRelocInfo(bool littleEndian, ulong info)
        {
            // N64 r_info 是 {r_sym, r_ssym, r_type3, r_type2, r_type}，主类型随字节序取不同字节。
            return littleEndian ? ((uint)(info & 0xffffffff), (uint)((info >> 56) & 0xff))
                : ((uint)(info >> 32), (uint)(info & 0xff));
        }

        private static (string Name, string Value) ResolveRelocSymbol(ELFParser parser, List<ELFSymbol> symbols, uint index, SectionType type)
        {
            return index < symbols.Count ? GetRelocSymbol(parser, symbols[(int)index], (int)index, type)
                : (string.Empty, ELFDisplayFormatting.Hex(0, parser.Is64Bit));
        }

        private static (string Name, string Value) GetRelocSymbol(ELFParser parser, ELFSymbol symbol, int index, SectionType type)
        {
            return (ELFDisplayFormatting.SymbolName(parser, symbol, type, index), ELFDisplayFormatting.Hex(symbol.StValue, parser.Is64Bit));
        }

        private static SectionType ResolveSymbolTableType(ELFParser parser, int index)
        {
            return IsStaticSymbolTable(parser, index) ? SectionType.SHT_SYMTAB : SectionType.SHT_DYNSYM;
        }

        private static bool IsStaticSymbolTable(ELFParser parser, int index)
        {
            return index >= 0 && index < (parser.SectionHeaders?.Count ?? 0)
                && parser.SectionHeaders![index].sh_type == (uint)SectionType.SHT_SYMTAB;
        }
    }
}
