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

    public enum ProgramHeaderType : uint
    {
        PT_NULL = 0,           // Unused program header table entry
        PT_LOAD = 1,           // Loadable program segment
        PT_DYNAMIC = 2,        // Dynamic linking information
        PT_INTERP = 3,         // Program interpreter
        PT_NOTE = 4,           // Auxiliary information
        PT_SHLIB = 5,          // Reserved, unspecified semantics
        PT_PHDR = 6,           // Entry for header table itself
        PT_TLS = 7,            // Thread-local storage segment
        PT_LOOS = 0x60000000,  // OS-specific
        PT_HIOS = 0x6FFFFFFF,  // OS-specific
        PT_LOPROC = 0x70000000,// Processor-specific
        PT_EXIDX = 0x70000001,
        PT_EXTAB = 0x70000002,
        PT_HIPROC = 0x7FFFFFFF,// Processor-specific
        PT_GNU_EH_FRAME = 0x6474E550, // GCC .eh_frame_hdr segment
        PT_GNU_STACK = 0x6474E551,    // Indicates stack executability
        PT_GNU_RELRO = 0x6474E552     // Read-only after relocation
    }

    [Flags]
    public enum ProgramHeaderFlags : uint
    {
        PF_X = 0x1,            // Execute
        PF_W = 0x2,            // Write
        PF_R = 0x4,            // Read
        PF_MASKOS = 0x00FF0000,// Unspecified
        PF_MASKPROC = 0xFF000000 // Unspecified
    }
}