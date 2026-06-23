namespace PersonalTools.ELFAnalyzer.Models
{
    internal sealed class ELFDynamicSectionInfo
    {
        public required string Tag { get; set; }
        public required string Type { get; set; }
        public required string Value { get; set; }
    }
}
