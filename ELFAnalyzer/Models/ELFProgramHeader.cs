using System.Runtime.InteropServices;

namespace PersonalTools.ELFAnalyzer.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ELFProgramHeader
    {
        public uint p_type;     // Segment type
        public uint p_flags;    // Segment flags
        public ulong p_offset;  // Segment file offset
        public ulong p_vaddr;   // Segment virtual address
        public ulong p_paddr;   // Segment physical address
        public ulong p_filesz;  // Segment size in file
        public ulong p_memsz;   // Segment size in memory
        public ulong p_align;   // Segment alignment
    }
}