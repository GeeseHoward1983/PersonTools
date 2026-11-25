using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using System.Globalization;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class GotHelper
    {
        private const int MaxGotEntries = 2_000_000;
        private static readonly string[] RelocationSections = [".rela.plt", ".rel.plt", ".rela.dyn", ".rel.dyn"];
        private sealed record GotContext(ELFParser Parser, ELFSectionHeader Section, string Name,
            Dictionary<ulong, ELFRelocationInfo> Relocations);

        internal static List<ELFGotInfo> GetGotInfoList(ELFParser parser, string sectionName)
        {
            return parser.SectionHeaders == null ? []
                : ReadGotSection(parser, sectionName, BuildRelocationMap(parser));
        }

        private static Dictionary<ulong, ELFRelocationInfo> BuildRelocationMap(ELFParser parser)
        {
            // 顺序不变：重复 r_offset 的最后一个重定位覆盖前面的条目。
            return RelocationSections.SelectMany(name => RelocationHelper.GetRelocationInfoForSpecificSection(parser, name))
                .Aggregate(new Dictionary<ulong, ELFRelocationInfo>(), AddRelocation);
        }

        private static Dictionary<ulong, ELFRelocationInfo> AddRelocation(Dictionary<ulong, ELFRelocationInfo> map, ELFRelocationInfo relocation)
        {
            return TryGetRelocationOffset(relocation, out ulong offset) ? StoreRelocation(map, offset, relocation) : map;
        }

        private static Dictionary<ulong, ELFRelocationInfo> StoreRelocation(Dictionary<ulong, ELFRelocationInfo> map, ulong offset, ELFRelocationInfo relocation)
        {
            map[offset] = relocation;
            return map;
        }

        private static bool TryGetRelocationOffset(ELFRelocationInfo relocation, out ulong offset)
        {
            return ulong.TryParse(relocation.Offset.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out offset);
        }

        private static List<ELFGotInfo> ReadGotSection(ELFParser parser, string name, Dictionary<ulong, ELFRelocationInfo> relocations)
        {
            return ReadGotSectionAtIndex(parser, name, relocations,
                ELFDisplayFormatting.FindSectionIndex(parser, index => ELFSymbolNameResolver.GetSectionName(parser, index) == name));
        }

        private static List<ELFGotInfo> ReadGotSectionAtIndex(ELFParser parser, string name,
            Dictionary<ulong, ELFRelocationInfo> relocations, int index)
        {
            return index < 0 ? [] : ReadGotRows(new GotContext(parser, parser.SectionHeaders[index], name, relocations));
        }

        private static List<ELFGotInfo> ReadGotRows(GotContext context)
        {
            return Enumerable.Range(0, GetGotEntryCount(context)).Select(index => CreateGotInfo(context, index)).ToList();
        }

        private static int GetPointerSize(ELFParser parser)
        {
            // 槽宽恒为指针大小，不采信不可信的 sh_entsize。
            return parser.Is64Bit ? 8 : 4;
        }

        private static int GetGotEntryCount(GotContext context)
        {
            // 夹紧到声明条目数、文件实际可读数和硬上限，防止畸形节造成越界或 OOM。
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

        private readonly record struct GotSlot(ulong Address, long FileOffset);

        private static ELFGotInfo CreateGotInfo(GotContext context, int index)
        {
            return ResolveGotInfo(context, index, GetSlot(context, index));
        }

        private static ELFGotInfo ResolveGotInfo(GotContext context, int index, GotSlot slot)
        {
            return FormatGotInfo(context, index, slot, ResolveSlot(context.Relocations, slot.Address, context.Name, index));
        }

        private static ELFGotInfo FormatGotInfo(GotContext context, int index, GotSlot slot, (string Type, string Symbol) relocation)
        {
            return new ELFGotInfo
            {
                Number = $"{index}",
                Offset = ELFDisplayFormatting.Address(slot.Address, context.Parser.Is64Bit),
                Value = ReadSlotValue(context.Parser, slot.FileOffset, GetPointerSize(context.Parser), context.Parser.Header.IsLittleEndian()),
                Type = relocation.Type,
                Symbol = relocation.Symbol,
                SectionName = context.Name
            };
        }

        private static string ReadSlotValue(ELFParser parser, long fileOffset, int entrySize, bool littleEndian)
        {
            return IsReadableSlot(parser, fileOffset, entrySize)
                ? ReadSlotValueAtOffset(parser, (int)fileOffset, entrySize, littleEndian) : string.Empty;
        }

        private static bool IsReadableSlot(ELFParser parser, long fileOffset, int entrySize)
        {
            // 校验实际读取宽度，而不是不可信的 entrySize。
            return fileOffset >= 0 && fileOffset <= parser.FileData.Length - (entrySize >= 8 ? 8 : 4);
        }

        private static string ReadSlotValueAtOffset(ELFParser parser, int offset, int entrySize, bool littleEndian)
        {
            return entrySize >= 8 ? ELFDisplayFormatting.Address(ELFParserUtils.ReadUInt64(parser.FileData, offset, littleEndian), true)
                : ELFDisplayFormatting.Address(ELFParserUtils.ReadUInt32(parser.FileData, offset, littleEndian), false);
        }

        private static (string Type, string Symbol) ResolveSlot(Dictionary<ulong, ELFRelocationInfo> relocations,
            ulong address, string sectionName, int index)
        {
            return relocations.GetValueOrDefault(address) is ELFRelocationInfo relocation
                ? (relocation.Type, relocation.Symbol) : GetReservedSlot(sectionName, index);
        }

        private static (string Type, string Symbol) GetReservedSlot(string sectionName, int index)
        {
            // .got.plt 前三个槽保留给 .dynamic 和运行时链接器，除非已有重定位覆盖。
            return (sectionName == ".got.plt" && index < 3 ? "保留" : string.Empty, string.Empty);
        }
    }
}
