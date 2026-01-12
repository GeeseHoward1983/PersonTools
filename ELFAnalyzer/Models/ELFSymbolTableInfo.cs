namespace PersonalTools.ELFAnalyzer.Models
{
    public class ELFSymbolTableInfo
    {
        public int Number { get; set; }
        public required string Value { get; set; }
        public required string Size { get; set; }
        public required string Type { get; set; }
        public required string Bind { get; set; }
        public required string Vis { get; set; }
        public required string Ndx { get; set; }
        public required string Name { get; set; }
    }
}