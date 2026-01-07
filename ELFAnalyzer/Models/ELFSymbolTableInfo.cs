namespace PersonalTools.ELFAnalyzer.Models
{
    public class ELFSymbolTableInfo
    {
        public int Number { get; set; }
        public string Value { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Bind { get; set; } = string.Empty;
        public string Vis { get; set; } = string.Empty;
        public string Ndx { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}