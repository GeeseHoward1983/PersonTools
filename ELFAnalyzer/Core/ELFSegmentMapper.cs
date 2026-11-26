using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFSegmentMapper
    {
        private const long MaxMappingChecks = 2_000_000;
        private sealed class MappingState(ELFParser parser)
        {
            internal ELFParser Parser { get; } = parser;
            internal long Budget { get; set; } = MaxMappingChecks;
        }

        internal static ELFSegmentMappingResult Map(ELFParser parser)
        {
            return CreateResult(new MappingState(parser));
        }

        private static ELFSegmentMappingResult CreateResult(MappingState state)
        {
            // 先枚举段映射再读取预算，保证最后一段和截断标记与原实现一致。
            List<ELFSegmentMappingData> mappings = ReadMappings(state).ToList();
            return new ELFSegmentMappingResult(mappings, state.Budget <= 0);
        }

        private static IEnumerable<ELFSegmentMappingData> ReadMappings(MappingState state)
        {
            return GetSegmentIndices(state).Select(index => ReadMapping(state, index));
        }

        private static IEnumerable<int> GetSegmentIndices(MappingState state)
        {
            return Enumerable.Range(0, state.Parser.ProgramHeaders?.Count ?? 0).TakeWhile(_ => state.Budget > 0);
        }

        private static ELFSegmentMappingData ReadMapping(MappingState state, int index)
        {
            return new ELFSegmentMappingData(index, GetSectionsInSegment(state, state.Parser.ProgramHeaders![index]).ToList());
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

    }
}
