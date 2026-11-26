using PersonalTools.ELFAnalyzer.Models;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFGotReader
    {
        private const int MaxGotEntries = 2_000_000;
        private static readonly string[] RelocationSections = [".rela.plt", ".rel.plt", ".rela.dyn", ".rel.dyn"];
        private sealed record GotContext(ELFParser Parser, ELFSectionHeader Section, string Name,
            Dictionary<ulong, ELFRelocationReference> Relocations);
        private readonly record struct GotSlot(ulong Address, long FileOffset);

        internal static List<ELFGotEntryData> ReadSection(ELFParser parser, string name)
        {
            return parser.SectionHeaders == null ? [] : ReadSection(parser, name, BuildRelocationMap(parser));
        }

        private static Dictionary<ulong, ELFRelocationReference> BuildRelocationMap(ELFParser parser)
        {
            // 直接用数值 r_offset 建索引，不再把十六进制显示字符串反解析回地址。
            return RelocationSections.SelectMany(name => ReadRelocations(parser, name))
                .Aggregate(new Dictionary<ulong, ELFRelocationReference>(), StoreRelocation);
        }

        private static Dictionary<ulong, ELFRelocationReference> StoreRelocation(Dictionary<ulong, ELFRelocationReference> map,
            ELFRelocationReference relocation)
        {
            // 与原顺序一致：重复地址的最后一个条目覆盖前面的条目。
            map[relocation.Entry.Offset] = relocation;
            return map;
        }

        private static IEnumerable<ELFRelocationReference> ReadRelocations(ELFParser parser, string name)
        {
            return GetReferences(ELFRelocationReader.ReadSection(parser, name));
        }

        private static IEnumerable<ELFRelocationReference> GetReferences(ELFRelocationSectionData? section)
        {
            return section?.Entries.Select(entry => new ELFRelocationReference(entry, section)) ?? [];
        }

        private static List<ELFGotEntryData> ReadSection(ELFParser parser, string name, Dictionary<ulong, ELFRelocationReference> relocations)
        {
            return ReadSectionAtIndex(parser, name, relocations, ELFSectionLocator.FindByName(parser, name));
        }

        private static List<ELFGotEntryData> ReadSectionAtIndex(ELFParser parser, string name,
            Dictionary<ulong, ELFRelocationReference> relocations, int index)
        {
            return index < 0 ? [] : ReadEntries(new GotContext(parser, parser.SectionHeaders[index], name, relocations));
        }

        private static List<ELFGotEntryData> ReadEntries(GotContext context)
        {
            return Enumerable.Range(0, GetEntryCount(context)).Select(index => ReadEntry(context, index)).ToList();
        }

        private static int GetPointerSize(ELFParser parser) => parser.Is64Bit ? 8 : 4;

        private static int GetEntryCount(GotContext context)
        {
            // 槽宽按指针大小，声明数量、实际可读数量及硬上限共同约束；忽略 sh_entsize。
            return (int)Math.Min(Math.Min(context.Section.sh_size / (ulong)GetPointerSize(context.Parser),
                GetReadableBytes(context) / (ulong)GetPointerSize(context.Parser)), MaxGotEntries);
        }

        private static ulong GetReadableBytes(GotContext context)
        {
            return context.Section.sh_offset >= (ulong)context.Parser.FileData.Length
                ? 0 : (ulong)context.Parser.FileData.Length - context.Section.sh_offset;
        }

        private static GotSlot GetSlot(GotContext context, int index)
        {
            return new GotSlot(context.Section.sh_addr + (ulong)((long)index * GetPointerSize(context.Parser)),
                (long)context.Section.sh_offset + (long)index * GetPointerSize(context.Parser));
        }

        private static ELFGotEntryData ReadEntry(GotContext context, int index)
        {
            return ReadEntry(context, index, GetSlot(context, index));
        }

        private static ELFGotEntryData ReadEntry(GotContext context, int index, GotSlot slot)
        {
            return CreateEntry(context, index, slot, FindRelocation(context.Relocations, slot.Address));
        }

        private static ELFRelocationReference? FindRelocation(Dictionary<ulong, ELFRelocationReference> relocations, ulong address)
        {
            return relocations.TryGetValue(address, out ELFRelocationReference entry) ? entry : null;
        }

        private static ELFGotEntryData CreateEntry(GotContext context, int index, GotSlot slot, ELFRelocationReference? relocation)
        {
            return new ELFGotEntryData(index, slot.Address, ReadValue(context.Parser, slot.FileOffset),
                relocation == null && context.Name == ".got.plt" && index < 3, relocation);
        }

        private static ulong? ReadValue(ELFParser parser, long fileOffset)
        {
            return fileOffset >= 0 && fileOffset <= parser.FileData.Length - GetPointerSize(parser)
                ? ReadValueAtOffset(parser, (int)fileOffset) : null;
        }

        private static ulong ReadValueAtOffset(ELFParser parser, int offset)
        {
            return parser.Is64Bit ? ELFParserUtils.ReadUInt64(parser.FileData, offset, parser.Header.IsLittleEndian())
                : ELFParserUtils.ReadUInt32(parser.FileData, offset, parser.Header.IsLittleEndian());
        }
    }
}
