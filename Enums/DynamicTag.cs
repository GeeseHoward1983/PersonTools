namespace PersonalTools.Enums
{
    public enum DynamicTag : long
    {
        DT_NULL = 0,                    // Marks end of dynamic section
        DT_NEEDED = 1,                  // Name of needed library
        DT_PLTRELSZ = 2,                // Size in bytes of PLT relocs
        DT_PLTGOT = 3,                  // Processor defined value
        DT_HASH = 4,                    // Address of symbol hash table
        DT_STRTAB = 5,                  // Address of string table
        DT_SYMTAB = 6,                  // Address of symbol table
        DT_RELA = 7,                    // Address of Rela relocs
        DT_RELASZ = 8,                  // Total size of Rela relocs
        DT_RELAENT = 9,                 // Size of one Rela reloc
        DT_STRSZ = 10,                  // Total size of string table
        DT_SYMENT = 11,                 // Size of one symbol table entry
        DT_INIT = 12,                   // Address of init function
        DT_FINI = 13,                   // Address of termination function
        DT_SONAME = 14,                 // Name of shared object
        DT_RPATH = 15,                  // Library search path (deprecated)
        DT_SYMBOLIC = 16,               // Start symbol search here
        DT_REL = 17,                    // Address of Rel relocs
        DT_RELSZ = 18,                  // Total size of Rel relocs
        DT_RELENT = 19,                 // Size of one Rel reloc
        DT_PLTREL = 20,                 // Type of reloc in PLT
        DT_DEBUG = 21,                  // For debugging
        DT_TEXTREL = 22,                // Reloc might modify .text
        DT_JMPREL = 23,                 // Address of PLT relocs
        DT_BIND_NOW = 24,               // Process relocations of object
        DT_INIT_ARRAY = 25,             // Array with addresses of init fct
        DT_FINI_ARRAY = 26,             // Array with addresses of fini fct
        DT_INIT_ARRAYSZ = 27,           // Size in bytes of DT_INIT_ARRAY
        DT_FINI_ARRAYSZ = 28,           // Size in bytes of DT_FINI_ARRAY
        DT_RUNPATH = 29,                // Library search path
        DT_FLAGS = 30,                  // Flags for the object being loaded
        DT_ENCODING = 32,               // Start of encoded range
        DT_PREINIT_ARRAY = 31,          // Array with addresses of preinit fct (was incorrectly set to 32, changed to 31 to avoid conflict)
        DT_PREINIT_ARRAYSZ = 33,        // Size in bytes of DT_PREINIT_ARRAY
        DT_NUM = 34,                    // Number used
        DT_LOOS = 0x6000000D,           // Start of OS-specific
        DT_HIOS = 0x6ffff000,           // End of OS-specific
        DT_LOPROC = 0x70000000,         // Start of processor-specific
        DT_HIPROC = 0x7fffffff,         // End of processor-specific

        // GNU versioning entries
        DT_VERSYM = 0x6ffffff0,         // Symbol versions
        DT_RELACOUNT = 0x6ffffff9,      // Count of RELATIVE relocations
        DT_RELCOUNT = 0x6ffffffa,       // Count of RELATIVE relocations
        DT_FLAGS_1 = 0x6ffffffb,        // Flags for the object being loaded
        DT_VERDEF = 0x6ffffffc,         // Version definition section
        DT_VERDEFNUM = 0x6ffffffd,      // Number of version definitions
        DT_VERNEED = 0x6ffffffe,        // Required version structure
        DT_VERNEEDNUM = 0x6fffffff,     // Number of required versions
        DT_VERSTR = 0x6ffffef0,         // Version string table

        // GNU-specific dynamic array tags
        DT_GNU_HASH = 0x6ffffef5,       // GNU hash table
        DT_GNU_CONFLICT = 0x6ffffef8,   // Start of conflict section
        DT_GNU_LIBLIST = 0x6ffffef9,    // Library list
        DT_CONFIG = 0x6ffffefa,         // Configuration information
        DT_DEPAUDIT = 0x6ffffefb,       // Dependency auditing
        DT_AUDIT = 0x6ffffefc,          // Object auditing
        DT_PLTPAD = 0x6ffffefd,         // PLT padding
        DT_MOVETAB = 0x6ffffefe,        // Move table
        DT_SYMINFO = 0x6ffffeff,        // Syminfo table

        // GNU-specific additional tags
        DT_GNU_PRELINKED = 0x6ffffdf5,  // Prelinking timestamp
        DT_GNU_CONFLICTSZ = 0x6ffffdf6, // Size of conflict section
        DT_GNU_LIBLISTSZ = 0x6ffffdf7,  // Size of library list
        DT_CHECKSUM = 0x6ffffdf8,       // Checksum for the object
        DT_PLTPADSZ = 0x6ffffdf9,       // Size of PLT padding
        DT_MOVEENT = 0x6ffffdfa,        // Size of DT_MOVETAB entries
        DT_MOVESZ = 0x6ffffdfb,         // Total size of the MOVETAB table
        DT_FEATURE_1 = 0x6ffffdfc,      // Feature selection (DTF_)
        DT_POSFLAG_1 = 0x6ffffdfd,      // Flags for DT_ entries, effecting
        DT_SYMINSZ = 0x6ffffdfe,        // Size of syminfo table (in bytes)
        DT_SYMINENT = 0x6ffffdff,       // Entry size of syminfo

        // Additional tags
        DT_SYMTAB_SHNDX = 0x6ffffff5,    // Address of SYMTAB_SHNDX section

        DT_MIPS_RLD_VERSION = 0x70000001,
        DT_MIPS_TIME_STAMP = 0x70000002,
        DT_MIPS_ICHECKSUM = 0x70000003,
        DT_MIPS_IVERSION = 0x70000004,
        DT_MIPS_FLAGS = 0x70000005,
        DT_MIPS_BASE_ADDRESS = 0x70000006,
        DT_MIPS_MSYM = 0x70000007,
        DT_MIPS_CONFLICT = 0x70000008,
        DT_MIPS_LIBLIST = 0x70000009,
        DT_MIPS_LOCAL_GOTNO = 0x7000000a,
        DT_MIPS_CONFLICTNO = 0x7000000b,
        DT_MIPS_LIBLISTNO = 0x70000010,
        DT_MIPS_SYMTABNO = 0x70000011,
        DT_MIPS_UNREFEXTNO = 0x70000012,
        DT_MIPS_GOTSYM = 0x70000013,
        DT_MIPS_HIPAGENO = 0x70000014,
        DT_MIPS_RLD_MAP = 0x70000016,
        DT_MIPS_DELTA_CLASS = 0x70000017,
        /* Number of entries in DT_MIPS_DELTA_CLASS.*/
        DT_MIPS_DELTA_CLASS_NO = 0x70000018,
        DT_MIPS_DELTA_INSTANCE = 0x70000019,
        /* Number of entries in DT_MIPS_DELTA_INSTANCE.*/
        DT_MIPS_DELTA_INSTANCE_NO = 0x7000001a,
        DT_MIPS_DELTA_RELOC = 0x7000001b,
        /* Number of entries in DT_MIPS_DELTA_RELOC.*/
        DT_MIPS_DELTA_RELOC_NO = 0x7000001c,
        DT_MIPS_DELTA_SYM = 0x7000001d,
        /* Number of entries in DT_MIPS_DELTA_SYM.*/
        DT_MIPS_DELTA_SYM_NO = 0x7000001e,
        DT_MIPS_DELTA_CLASSSYM = 0x70000020,
        /* Number of entries in DT_MIPS_DELTA_CLASSSYM.*/
        DT_MIPS_DELTA_CLASSSYM_NO = 0x70000021,
        DT_MIPS_CXX_FLAGS = 0x70000022,
        DT_MIPS_PIXIE_INIT = 0x70000023,
        DT_MIPS_SYMBOL_LIB = 0x70000024,
        DT_MIPS_LOCALPAGE_GOTIDX = 0x70000025,
        DT_MIPS_LOCAL_GOTIDX = 0x70000026,
        DT_MIPS_HIDDEN_GOTIDX = 0x70000027,
        DT_MIPS_PROTECTED_GOTIDX = 0x70000028,
        DT_MIPS_OPTIONS = 0x70000029,
        DT_MIPS_INTERFACE = 0x7000002a,
        DT_MIPS_DYNSTR_ALIGN = 0x7000002b,
        DT_MIPS_INTERFACE_SIZE = 0x7000002c,
        DT_MIPS_RLD_TEXT_RESOLVE_ADDR = 0x7000002d,
        DT_MIPS_PERF_SUFFIX = 0x7000002e,
        DT_MIPS_COMPACT_SIZE = 0x7000002f,
        DT_MIPS_GP_VALUE = 0x70000030,
        DT_MIPS_AUX_DYNAMIC = 0x70000031,
        DT_MIPS_PLTGOT = 0x70000032,
        DT_MIPS_RWPLT = 0x70000034,
        DT_MIPS_RLD_MAP_REL = 0x70000035,
        DT_MIPS_XHASH = 0x70000036
        /* Flags which may appear in a DT_MIPS_FLAGS entry.*/
    }
}