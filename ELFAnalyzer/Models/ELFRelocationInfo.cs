namespace PersonalTools.ELFAnalyzer.Models
{
    public class ELFRelocationInfo
    {
        public string Offset { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string SymbolValue { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Addend { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
    }
}