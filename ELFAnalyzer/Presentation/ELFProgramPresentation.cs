using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using System.Globalization;

namespace PersonalTools.ELFAnalyzer.Presentation
{
    internal static class ELFProgramPresentation
    {
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
                Offset = ELFValueFormatting.Address(header.p_offset, is64Bit),
                VirtAddr = ELFValueFormatting.Address(header.p_vaddr, is64Bit),
                PhysAddr = ELFValueFormatting.Address(header.p_paddr, is64Bit),
                FileSize = $"{header.p_filesz}",
                MemSize = $"{header.p_memsz}",
                Flags = ELFProgramHeaderInfo.GetProgramHeaderFlags(header.p_flags) ?? "",
                Align = $"0x{header.p_align:x}"
            };
        }

        internal static string GetSectionToSegmentMappingInfo(ELFParser parser)
        {
            return FormatMappings(ELFSegmentMapper.Map(parser));
        }

        private static string FormatMappings(ELFSegmentMappingResult result)
        {
            return string.Join(Environment.NewLine, MappingHeaders.Concat(result.Mappings.Select(FormatMapping))
                .Concat(GetMappingNotice(result)).Append(string.Empty));
        }

        private static string FormatMapping(ELFSegmentMappingData mapping)
        {
            return string.Format(CultureInfo.InvariantCulture, "   {0:D2}     {1}", mapping.SegmentIndex,
                string.Join(" ", mapping.SectionNames));
        }

        private static IEnumerable<string> GetMappingNotice(ELFSegmentMappingResult result)
        {
            if (result.IsTruncated) yield return "  ...(映射计算量超出上限，其余段已省略)";
        }
    }
}
