namespace PersonalTools.ELFAnalyzer.Models
{
    public class ELFSectionHeaderInfo
    {
        public int Index;
        public required string Name;
        public required string Type;
        public required string Address;
        public required string Offset;
        public required string Size;
        public required string EntSize;
        public required string Flags;
        public required string Link;
        public required string Info;
        public required string Align;
    }
}