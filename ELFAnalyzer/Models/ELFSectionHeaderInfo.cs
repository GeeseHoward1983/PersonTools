namespace PersonalTools.ELFAnalyzer.Models
{
    public class ELFSectionHeaderInfo
    {
        public int Index { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Offset { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string EntSize { get; set; } = string.Empty;
        public string Flags { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
        public string Align { get; set; } = string.Empty;
    }
}