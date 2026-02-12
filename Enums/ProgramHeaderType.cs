namespace PersonalTools.Enums
{
    internal enum ProgramHeaderType : uint
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
}