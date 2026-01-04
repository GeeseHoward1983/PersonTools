using System;
using System.Runtime.InteropServices;

namespace PersonalTools.ELFAnalyzer.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ELFSectionHeader32
    {
        public uint sh_name;      // Section name (index into string table)
        public uint sh_type;      // Section type
        public uint sh_flags;     // Section flags
        public uint sh_addr;      // Section virtual address at execution
        public uint sh_offset;    // Section file offset
        public uint sh_size;      // Section size in bytes
        public uint sh_link;      // Link to other section
        public uint sh_info;      // Additional section information
        public uint sh_addralign; // Section alignment
        public uint sh_entsize;   // Entry size if section holds table
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ELFSectionHeader64
    {
        public uint sh_name;      // Section name (index into string table)
        public uint sh_type;      // Section type
        public ulong sh_flags;    // Section flags
        public ulong sh_addr;     // Section virtual address at execution
        public ulong sh_offset;   // Section file offset
        public ulong sh_size;     // Section size in bytes
        public uint sh_link;      // Link to other section
        public uint sh_info;      // Additional section information
        public ulong sh_addralign; // Section alignment
        public ulong sh_entsize;  // Entry size if section holds table
    }

    public enum SectionType : uint
    {
        SHT_NULL = 0,             // Section header table entry unused
        SHT_PROGBITS = 1,         // Program data
        SHT_SYMTAB = 2,           // Symbol table
        SHT_STRTAB = 3,           // String table
        SHT_RELA = 4,             // Relocation entries with addends
        SHT_HASH = 5,             // Symbol hash table
        SHT_DYNAMIC = 6,          // Dynamic linking information
        SHT_NOTE = 7,             // Notes
        SHT_NOBITS = 8,           // Program space with no data (bss)
        SHT_REL = 9,              // Relocation entries, no addends
        SHT_SHLIB = 10,           // Reserved
        SHT_DYNSYM = 11,          // Dynamic linker symbol table
        SHT_INIT_ARRAY = 14,      // Array of constructors
        SHT_FINI_ARRAY = 15,      // Array of destructors
        SHT_PREINIT_ARRAY = 16,   // Array of pre-constructors
        SHT_GROUP = 17,           // Section group
        SHT_SYMTAB_SHNDX = 18,    // Extended section indices
        SHT_LOOS = 0x60000000,    // Start OS-specific
        SHT_GNU_ATTRIBUTES = 0x6ffffff5, // Object attributes
        SHT_GNU_HASH = 0x6ffffff6,       // GNU-style hash table
        SHT_GNU_LIBLIST = 0x6ffffff7,    // Prelink library list
        SHT_CHECKSUM = 0x6ffffff8,       // Checksum for DSO content
        SHT_LOSUNW = 0x6ffffffa,         // Sun-specific low bound - correct ELF standard value
        SHT_SUNW_move = SHT_LOSUNW,      // Same value as SHT_LOSUNW - correct ELF standard value
        SHT_SUNW_COMDAT = 0x6ffffffb,
        SHT_SUNW_syminfo = 0x6ffffffc,
        SHT_GNU_verdef = 0x6ffffffd,     // Version definition section
        SHT_GNU_verneed = 0x6ffffffe,    // Version needs section
        SHT_GNU_versym = 0x6fffffff,     // Version symbol table - correct ELF standard value
        SHT_HISUNW = SHT_GNU_versym,         // Same value as SHT_GNU_versym - correct ELF standard value
        SHT_HIOS = SHT_GNU_versym,           // Same value as SHT_GNU_versym - correct ELF standard value
        SHT_LOPROC = 0x70000000,         // Start of processor-specific
        SHT_HIPROC = 0x7fffffff,         // End of processor-specific
        SHT_LOUSER = 0x80000000,         // Start of application-specific
        SHT_HIUSER = 0xffffffff          // End of application-specific
    }

    [Flags]
    public enum SectionFlags : ulong
    {
        SHF_WRITE = 0x1,          // Writable data during execution
        SHF_ALLOC = 0x2,          // Occupies memory during execution
        SHF_EXECINSTR = 0x4,      // Executable machine instructions
        SHF_MERGE = 0x10,         // Might be merged
        SHF_STRINGS = 0x20,       // Contains null-terminated strings
        SHF_INFO_LINK = 0x40,     // This section header's sh_info field holds a section header table index
        SHF_LINK_ORDER = SHF_INFO_LINK,    // Special ordering requirement during linking - same value as SHF_INFO_LINK per ELF standard
        SHF_OS_NONCONFORMING = 0x100, // OS-specific processing required
        SHF_GROUP = 0x200,        // Section is a member of a group
        SHF_TLS = 0x400,          // Section holds thread-local data
        SHF_COMPRESSED = 0x800,   // Section with compressed data
        SHF_MASKOS = 0x0ff00000,  // OS-specific flags
        SHF_MASKPROC = 0xf0000000, // Processor-specific flags
    }
}