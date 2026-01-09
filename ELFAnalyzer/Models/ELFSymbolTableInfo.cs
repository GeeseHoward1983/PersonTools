namespace PersonalTools.ELFAnalyzer.Models
{
    public class ELFSymbolTableInfo
    {
        public int Number;
        public required string Value;
        public required string Size;
        public required string Type;
        public required string Bind;
        public required string Vis;
        public required string Ndx;
        public required string Name;
    }
}