namespace PersonalTools.Enums
{
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
        SHT_HIUSER = 0xffffffff,          // End of application-specific
        SHT_ARM_EXIDX = 0x70000001,      // ARM exception index table section
        SHT_ARM_ATTRIBUTES = 0x70000003, // ARM attributes section
        SHT_ARM_PREEMPTMAP = 0x70000004, // ARM preemption map section
        SHT_ARM_DEBUGOVERLAY = 0x70000005,// ARM debug overlay section
        SHT_ARM_OVERLAYSECTION = 0x70000006, // ARM overlay section
        SHF_ARM_PURECODE = 0x20000000 // The contents of this section contains only program instructions and no program data

    }
}