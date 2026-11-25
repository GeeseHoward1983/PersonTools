using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.Globalization;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class ProgramHeaderHelper
    {
        // 全局段×节检查上限，避免畸形 ELF 的 O(n²) 工作量和无界输出。
        private const long MaxMappingChecks = 2_000_000;
        private static readonly string[] MappingHeaders = [" Section to Segment mapping:", "  段节..."];

        internal static List<ProgramHeaderInfo> GetProgramHeaderInfoList(ELFParser parser)
        {
            return parser.ProgramHeaders?.Select(header => CreateProgramInfo(header, parser.Is64Bit)).ToList() ?? [];
        }

        private static ProgramHeaderInfo CreateProgramInfo(ELFProgramHeader header, bool is64Bit)
        {
            return new ProgramHeaderInfo
            {
                Type = ELFProgramHeaderInfo.GetProgramHeaderType(header.p_type) ?? "UNKNOWN",
                Offset = ELFDisplayFormatting.Address(header.p_offset, is64Bit),
                VirtAddr = ELFDisplayFormatting.Address(header.p_vaddr, is64Bit),
                PhysAddr = ELFDisplayFormatting.Address(header.p_paddr, is64Bit),
                FileSize = $"{header.p_filesz}",
                MemSize = $"{header.p_memsz}",
                Flags = ELFProgramHeaderInfo.GetProgramHeaderFlags(header.p_flags) ?? "",
                Align = $"0x{header.p_align:x}"
            };
        }

        internal static string GetSectionToSegmentMappingInfo(ELFParser parser)
        {
            return string.Join(Environment.NewLine, GetMappingLines(new MappingState(parser)).Append(string.Empty));
        }

        private sealed class MappingState(ELFParser parser)
        {
            internal ELFParser Parser { get; } = parser;
            internal long Budget { get; set; } = MaxMappingChecks;
        }

        private static IEnumerable<string> GetMappingLines(MappingState state)
        {
            return MappingHeaders
                .Concat(GetSegmentLines(state)).Concat(GetMappingNotice(state));
        }

        private static IEnumerable<string> GetSegmentLines(MappingState state)
        {
            // 必须在计算下一段之前检查预算，保留预算耗尽时已经产生的最后一行。
            return GetSegmentIndices(state).Select(index => FormatSegmentLine(state, index));
        }

        private static IEnumerable<int> GetSegmentIndices(MappingState state)
        {
            return Enumerable.Range(0, state.Parser.ProgramHeaders?.Count ?? 0).TakeWhile(_ => state.Budget > 0);
        }

        private static IEnumerable<string> GetMappingNotice(MappingState state)
        {
            // 延迟到段行枚举完毕后检查，不能在创建序列时提前读取 Budget。
            if (state.Budget <= 0)
            {
                yield return "  ...(映射计算量超出上限，其余段已省略)";
            }
        }

        private static string FormatSegmentLine(MappingState state, int index)
        {
            return string.Format(CultureInfo.InvariantCulture, "   {0:D2}     {1}", index,
                string.Join(" ", GetSectionsInSegment(state, state.Parser.ProgramHeaders![index])));
        }

        private readonly record struct NamedSection(ELFSectionHeader Header, string Name);

        private static IEnumerable<string> GetSectionsInSegment(MappingState state, ELFProgramHeader segment)
        {
            return GetNamedAllocatedSections(state)
                .Where(item => IsNamedOverlappingSection(item.Header, item.Name, segment)).Select(GetSectionName);
        }

        private static string GetSectionName(NamedSection section)
        {
            return section.Name;
        }

        private static IEnumerable<NamedSection> GetNamedAllocatedSections(MappingState state)
        {
            return GetCheckedSections(state).Where(IsAllocatedSection)
                .Select(item => new NamedSection(item.Header, ELFSymbolNameResolver.GetSectionName(state.Parser, item.Index)));
        }

        private static bool IsAllocatedSection((ELFSectionHeader Header, int Index) item)
        {
            return IsAllocatedNonemptySection(item.Header);
        }

        private static IEnumerable<(ELFSectionHeader Header, int Index)> GetCheckedSections(MappingState state)
        {
            // 预算对每个节计数，包括空节和非分配节；首次不足时预算减为 -1。
            return GetIndexedSections(state.Parser).TakeWhile(_ => --state.Budget >= 0);
        }

        private static IEnumerable<(ELFSectionHeader Header, int Index)> GetIndexedSections(ELFParser parser)
        {
            return (parser.SectionHeaders ?? []).Select((section, index) => (section, index));
        }

        private static bool IsAllocatedNonemptySection(ELFSectionHeader section)
        {
            // 非分配节（.debug*、.symtab 等）不能因地址为 0 被误映射到首个 LOAD 段。
            return section.sh_size != 0 && (section.sh_flags & (ulong)SectionAttributes.SHF_ALLOC) != 0;
        }

        private static bool IsNamedOverlappingSection(ELFSectionHeader section, string name, ELFProgramHeader segment)
        {
            return !string.IsNullOrEmpty(name)
                && SectionOverlapsSegment(section, segment.p_vaddr, SaturatingEnd(segment.p_vaddr, segment.p_memsz));
        }

        private static ulong SaturatingEnd(ulong start, ulong size)
        {
            return size > ulong.MaxValue - start ? ulong.MaxValue : start + size;
        }

        private static bool SectionOverlapsSegment(ELFSectionHeader section, ulong start, ulong end)
        {
            return RangesOverlap(section.sh_addr, SaturatingEnd(section.sh_addr, section.sh_size), start, end);
        }

        private static bool RangesOverlap(ulong sectionStart, ulong sectionEnd, ulong segmentStart, ulong segmentEnd)
        {
            return (sectionStart >= segmentStart && sectionStart < segmentEnd)
                || (sectionEnd > segmentStart && sectionEnd <= segmentEnd)
                || (sectionStart <= segmentStart && sectionEnd >= segmentEnd);
        }

        internal static string GetInterpreterInfo(ELFParser parser)
        {
            return ReadInterpreter(parser, parser.ProgramHeaders?.FirstOrDefault(header => header.p_type == (uint)ProgramHeaderType.PT_INTERP) ?? default);
        }

        private static string ReadInterpreter(ELFParser parser, ELFProgramHeader header)
        {
            return header.p_type != 0 && header.p_offset < (ulong)parser.FileData.Length
                ? ELFParserUtils.ExtractStringFromBytes(parser.FileData, (int)header.p_offset, GetInterpreterLength(parser, header))
                : string.Empty;
        }

        private static int GetInterpreterLength(ELFParser parser, ELFProgramHeader header)
        {
            // 先夹紧 ulong 范围再转换 int，长度不得超过 PT_INTERP 的声明范围。
            return (int)Math.Min(header.p_filesz, (ulong)parser.FileData.Length - header.p_offset);
        }
    }
}
