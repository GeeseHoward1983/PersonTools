namespace PersonalTools.Enums
{
    internal enum SymbolType : byte
    {
        STT_NOTYPE = 0,         // Symbol type is unspecified
        STT_OBJECT = 1,         // Symbol is a data object
        STT_FUNC = 2,           // Symbol is a code object
        STT_SECTION = 3,        // Symbol associated with a section
        STT_FILE = 4,           // Symbol gives a file name
        STT_COMMON = 5,         // Symbol is a common data object
        STT_TLS = 6,            // Symbol is thread-local data object
        STT_GNU_IFUNC = 10,     // Symbol is GNU indirect function
        STT_LOOS = STT_GNU_IFUNC, // gABI: OS 保留段起始为 10（与 STT_GNU_IFUNC 同值别名；声明序在前者优先用于显示）
        STT_HIOS = 12,          // OS-specific symbol types
        STT_LOPROC = 13,        // Processor-specific symbol types
        STT_HIPROC = 15         // Processor-specific symbol types
    }
}