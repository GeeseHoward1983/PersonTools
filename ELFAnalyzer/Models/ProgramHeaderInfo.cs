namespace PersonalTools.ELFAnalyzer.Models
{
    public class ProgramHeaderInfo
    {
        public required string Type { get; set; }
        public required string Offset { get; set; }
        public required string VirtAddr { get; set; }
        public required string PhysAddr { get; set; }
        public required string FileSize { get; set; }
        public required string MemSize { get; set; }
        public required string Flags { get; set; }
        public required string Align { get; set; }
    }
}