namespace PersonalTools.Enums
{
    [Flags]
    internal enum SectionAttributes : ulong
    {
        SHF_WRITE = 0x1,          // Writable data during execution
        SHF_ALLOC = 0x2,          // Occupies memory during execution
        SHF_EXECINSTR = 0x4,      // Executable machine instructions
        SHF_MERGE = 0x10,         // Might be merged
        SHF_STRINGS = 0x20,       // Contains null-terminated strings
        SHF_INFO_LINK = 0x40,     // This section header's sh_info field holds a section header table index
        SHF_LINK_ORDER = 0x80,    // Special ordering requirement during linking（ELF 标准为独立位 0x80，不同于 SHF_INFO_LINK=0x40）
        SHF_OS_NONCONFORMING = 0x100, // OS-specific processing required
        SHF_GROUP = 0x200,        // Section is a member of a group
        SHF_TLS = 0x400,          // Section holds thread-local data
        SHF_COMPRESSED = 0x800,   // Section with compressed data
        SHF_MASKOS = 0x0ff00000,  // OS-specific flags
        SHF_ARM_PURECODE = 0x20000000, // ARM：节仅含纯指令、无数据（处理器特定 section flag，自 SectionType 迁入归位）
        SHF_MASKPROC = 0xf0000000, // Processor-specific flags
    }
}