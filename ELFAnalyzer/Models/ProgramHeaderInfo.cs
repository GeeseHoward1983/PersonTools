namespace PersonalTools.ELFAnalyzer.Models
{
    public class ProgramHeaderInfo
    {
        public required string Type;
        public required string Offset;
        public required string VirtAddr;
        public required string PhysAddr;
        public required string FileSize;
        public required string MemSize;
        public required string Flags;
        public required string Align;
    }
}