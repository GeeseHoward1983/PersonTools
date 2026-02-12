namespace PersonalTools.Enums
{
    internal enum SymbolVisibility : byte
    {
        STV_DEFAULT = 0,    // Default symbol visibility rules
        STV_INTERNAL = 1,   // Processor specific hidden class
        STV_HIDDEN = 2,     // Sym unavailable in other modules
        STV_PROTECTED = 3   // Not preemptible, not exported
    }
}