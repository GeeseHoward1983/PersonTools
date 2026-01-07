namespace PersonalTools.Enums
{
    [Flags]
    public enum ProgramHeaderFlags : uint
    {
        PF_X = 0x1,            // Execute
        PF_W = 0x2,            // Write
        PF_R = 0x4,            // Read
        PF_MASKOS = 0x00FF0000,// Unspecified
        PF_MASKPROC = 0xFF000000 // Unspecified
    }
}