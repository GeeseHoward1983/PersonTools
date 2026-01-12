namespace PersonalTools.ELFAnalyzer.Models
{
    public class ELFSectionHeaderInfo
    {
        public int Index { get; set; }
        public required string Name { get; set; }
        public required string Type { get; set; }
        public required string Address { get; set; }
        public required string Offset { get; set; }
        public required string Size { get; set; }
        public required string EntSize { get; set; }
        public required string Flags { get; set; }
        public required string Link { get; set; }
        public required string Info { get; set; }
        public required string Align { get; set; }
    }
}