namespace PersonalTools.ELFAnalyzer.Models
{
    internal sealed class ELFGotInfo
    {
        public required string Number { get; set; }
        public required string Offset { get; set; }
        public required string Value { get; set; }
        public required string Type { get; set; }
        public required string Symbol { get; set; }
        public required string SectionName { get; set; }
    }
}
