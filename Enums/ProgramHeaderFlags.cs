namespace PersonalTools.Enums
{
    [Flags]
    internal enum ProgramHeaderFlags : uint
    {
        PF_X = 0x1,            // Execute
        PF_W = 0x2,            // Write
        PF_R = 0x4,            // Read
        PF_MASKOS = 0x0FF00000,  // gABI: OS-specific 段掩码
        PF_MASKPROC = 0xF0000000 // gABI: Processor-specific 段掩码
    }
}