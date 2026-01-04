using System;
using System.Runtime.InteropServices;

namespace PersonalTools.ELFAnalyzer.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ELFSymbol32
    {
        public uint st_name;    // Symbol name (index into string table)
        public uint st_value;   // Symbol value (address)
        public uint st_size;    // Symbol size
        public byte st_info;    // Type and binding information
        public byte st_other;   // Visibility
        public ushort st_shndx; // Section index
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ELFSymbol64
    {
        public uint st_name;    // Symbol name (index into string table)
        public byte st_info;    // Type and binding information
        public byte st_other;   // Visibility
        public ushort st_shndx; // Section index
        public ulong st_value;  // Symbol value (address)
        public ulong st_size;   // Symbol size
    }

    public enum SymbolType : byte
    {
        STT_NOTYPE = 0,         // Symbol type is unspecified
        STT_OBJECT = 1,         // Symbol is a data object
        STT_FUNC = 2,           // Symbol is a code object
        STT_SECTION = 3,        // Symbol associated with a section
        STT_FILE = 4,           // Symbol gives a file name
        STT_COMMON = 5,         // Symbol is a common data object
        STT_TLS = 6,            // Symbol is thread-local data object
        STT_GNU_IFUNC = 10,     // Symbol is GNU indirect function
        STT_LOOS = 11,          // OS-specific symbol types
        STT_HIOS = 12,          // OS-specific symbol types
        STT_LOPROC = 13,        // Processor-specific symbol types
        STT_HIPROC = 15         // Processor-specific symbol types
    }

    public enum SymbolBinding : byte
    {
        STB_LOCAL = 0,      // Local symbol
        STB_GLOBAL = 1,     // Global symbol
        STB_WEAK = 2,       // Weak symbol
        STB_LOOS = 10,      // OS-specific bindings
        STB_HIOS = 12,      // OS-specific bindings
        STB_LOPROC = 13,    // Processor-specific bindings
        STB_HIPROC = 15     // Processor-specific bindings
    }

    public enum SymbolVisibility : byte
    {
        STV_DEFAULT = 0,    // Default symbol visibility rules
        STV_INTERNAL = 1,   // Processor specific hidden class
        STV_HIDDEN = 2,     // Sym unavailable in other modules
        STV_PROTECTED = 3   // Not preemptible, not exported
    }
}