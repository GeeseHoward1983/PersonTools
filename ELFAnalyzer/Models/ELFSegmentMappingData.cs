
namespace PersonalTools.ELFAnalyzer.Models
{
    internal sealed record ELFSegmentMappingData(int SegmentIndex, List<string> SectionNames);
    internal sealed record ELFSegmentMappingResult(List<ELFSegmentMappingData> Mappings, bool IsTruncated);
}
