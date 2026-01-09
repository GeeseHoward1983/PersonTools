namespace PersonalTools.ELFAnalyzer.Models
{
    public class ELFRelocationInfo
    {
        public required string Offset;
        public required string Info;
        public required string Type;
        public required string SymbolValue;
        public required string Symbol;
        public required string Addend;
        public required string SectionName;
    }
}