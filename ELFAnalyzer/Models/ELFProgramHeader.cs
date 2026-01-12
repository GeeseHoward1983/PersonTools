using System.Runtime.InteropServices;

namespace PersonalTools.ELFAnalyzer.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ELFProgramHeader
    {
        public uint p_type { get; set; }    // Segment type
        public uint p_flags { get; set; }    // Segment flags
        public ulong p_offset { get; set; }  // Segment file offset
        public ulong p_vaddr { get; set; }   // Segment virtual address
        public ulong p_paddr { get; set; }   // Segment physical address
        public ulong p_filesz { get; set; }  // Segment size in file
        public ulong p_memsz { get; set; }   // Segment size in memory
        public ulong p_align { get; set; }   // Segment alignment
    }
}