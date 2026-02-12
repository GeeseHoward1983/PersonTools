namespace PersonalTools.Enums
{
    internal enum SymbolBinding : byte
    {
        STB_LOCAL = 0,      // Local symbol
        STB_GLOBAL = 1,     // Global symbol
        STB_WEAK = 2,       // Weak symbol
        STB_LOOS = 10,      // OS-specific bindings
        STB_HIOS = 12,      // OS-specific bindings
        STB_LOPROC = 13,    // Processor-specific bindings
        STB_HIPROC = 15     // Processor-specific bindings
    }
}