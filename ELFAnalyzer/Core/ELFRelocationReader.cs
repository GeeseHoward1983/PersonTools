using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFRelocationReader
    {
        private sealed record RelocationContext(ELFParser Parser, ELFSectionHeader Section, string Name,
            byte[] Data, List<ELFSymbol> Symbols, SectionType SymbolType);

        internal static ELFRelocationSectionData? ReadSection(ELFParser parser, string name)
        {
            return ReadSectionAtIndex(parser, FindRelocationSection(parser, name), name);
        }

        private static int FindRelocationSection(ELFParser parser, string sectionName)
        {
            // 同名但非 REL/RELA 的节不能遮挡后面的真正重定位节。
            return ELFSectionLocator.FindIndex(parser,
                index => ELFSymbolNameResolver.GetSectionName(parser, index) == sectionName && IsRelocationSection(parser.SectionHeaders[index]));
        }

        private static bool IsRelocationSection(ELFSectionHeader section)
        {
            return section.sh_type is ((uint)SectionType.SHT_RELA) or ((uint)SectionType.SHT_REL);
        }

        private static ELFRelocationSectionData? ReadSectionAtIndex(ELFParser parser, int index, string name)
        {
            return index >= 0 && parser.SectionHeaders[index].sh_entsize != 0 ? ReadSectionData(CreateContext(parser, index, name)) : null;
        }

        private static RelocationContext CreateContext(ELFParser parser, int index, string name)
        {
            return new RelocationContext(parser, parser.SectionHeaders[index], name, parser.GetSectionData(index),
                ReadRelocationSymbols(parser, (int)parser.SectionHeaders[index].sh_link),
                ResolveSymbolTableType(parser, (int)parser.SectionHeaders[index].sh_link));
        }

        private static ELFRelocationSectionData ReadSectionData(RelocationContext context)
        {
            return new ELFRelocationSectionData(context.Name, context.Parser.Is64Bit, context.Parser.Header.e_machine,
                context.SymbolType, context.Symbols, ReadEntries(context));
        }

        private static List<ELFRelocationEntryData> ReadEntries(RelocationContext context)
        {
            return Enumerable.Range(0, GetRelocationEntryCount(context))
                .Select(index => CreateEntry(context, ReadRelocationEntry(context, index))).ToList();
        }

        private static int GetRelocationEntryCount(RelocationContext context)
        {
            // 同时约束声明条数和实际字节数，防止截断、零步长或畸形节导致越界。
            return (int)Math.Min(context.Section.sh_size / context.Section.sh_entsize,
                (ulong)(context.Data.Length / GetRelocationEntrySize(context.Parser.Is64Bit, IsRela(context))));
        }

        private static bool IsRela(RelocationContext context) => context.Section.sh_type == (uint)SectionType.SHT_RELA;

        private static int GetRelocationEntrySize(bool is64Bit, bool isRela) => is64Bit ? (isRela ? 24 : 16) : (isRela ? 12 : 8);

        private static ELFRelocationEntryData CreateEntry(RelocationContext context, (ulong Offset, ulong Info, long Addend) entry)
        {
            return CreateEntry(context, entry, SplitRelocInfo(context.Parser, entry.Info));
        }

        private static ELFRelocationEntryData CreateEntry(RelocationContext context,
            (ulong Offset, ulong Info, long Addend) entry, (uint Symbol, uint Type) info)
        {
            return new ELFRelocationEntryData(entry.Offset, entry.Info, info.Symbol, info.Type, IsRela(context) ? entry.Addend : null);
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
