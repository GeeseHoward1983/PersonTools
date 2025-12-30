using System;
using System.Runtime.InteropServices;

namespace MyTool.ELFAnalyzer.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ELFDynamic32
    {
        public int d_tag;      // Dynamic entry type
        public uint d_val;     // Integer value
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ELFDynamic64
    {
        public long d_tag;     // Dynamic entry type
        public ulong d_val;    // Integer value
    }

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
        DT_HIPROC = 0x7fffffff          // End of processor-specific
    }

    [Flags]
    public enum DynamicFlags : uint
    {
        DF_ORIGIN = 0x00000001,         // Object may use DF_ORIGIN
        DF_SYMBOLIC = 0x00000002,       // Symbol resolutions starts here
        DF_TEXTREL = 0x00000004,        // Object contains text relocations
        DF_BIND_NOW = 0x00000008,       // Don't lazy bind
        DF_STATIC_TLS = 0x00000010      // Static thread local storage
    }
}