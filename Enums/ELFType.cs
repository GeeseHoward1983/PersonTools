namespace PersonalTools.Enums
{
    internal enum ELFType : ushort
    {
        ET_NONE = 0,      // No file type
        ET_REL = 1,       // Relocatable file
        ET_EXEC = 2,      // Executable file
        ET_DYN = 3,       // Shared object file
        ET_CORE = 4       // Core file
    }
}