namespace PersonalTools.Enums
{
    public enum ELFClass : byte
    {
        ELFCLASSNONE = 0,
        ELFCLASS32 = 1,  // 32-bit object file
        ELFCLASS64 = 2   // 64-bit object file
    }
}