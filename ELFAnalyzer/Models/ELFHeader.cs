using System.Runtime.InteropServices;

namespace PersonalTools.ELFAnalyzer.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct ELFHeader
    {
        public byte EI_MAG0;      // 0x7F
        public byte EI_MAG1;      // 'E'
        public byte EI_MAG2;      // 'L'
        public byte EI_MAG3;      // 'F'
        public byte EI_CLASS;     // Architecture (32/64-bit)
        public byte EI_DATA;      // Byte order
        public byte EI_VERSION;   // ELF version
        public byte EI_OSABI;     // OS-specific ELF extensions
        public byte EI_ABIVERSION; // ABI version
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] EI_PAD;     // Padding

        public ushort e_type;      // Object file type
        public ushort e_machine;   // Architecture
        public uint e_version;     // Object file version
        public ulong e_entry;      // Entry point virtual address
        public ulong e_phoff;      // Program header table file offset
        public ulong e_shoff;      // Section header table file offset
        public uint e_flags;       // Processor-specific flags
        public ushort e_ehsize;    // ELF header size
        public ushort e_phentsize; // Size of program header entry
        public ushort e_phnum;     // Number of program header entries
        public ushort e_shentsize; // Size of section header entry
        public ushort e_shnum;     // Number of section header entries
        public ushort e_shstrndx;  // Section name string table index
    }
}