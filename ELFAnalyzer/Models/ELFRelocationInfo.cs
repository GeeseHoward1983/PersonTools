namespace PersonalTools.ELFAnalyzer.Models
{
    public class ELFRelocationInfo
    {
        public required string Offset { get; set; }
        public required string Info { get; set; }
        public required string Type { get; set; }
        public required string SymbolValue { get; set; }
        public required string Symbol { get; set; }
        public required string Addend { get; set; }
        public required string SectionName { get; set; }
    }
}