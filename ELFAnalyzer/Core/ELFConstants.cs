namespace PersonalTools.ELFAnalyzer.Core
{
    /// <summary>
    /// ELF 规范中的保留节索引(SHN_*)与符号信息位掩码常量，替代散落各 helper 的裸魔法数。
    /// 参见 ELF spec / readelf。
    /// </summary>
    internal static class ELFConstants
    {
        // 保留节索引（st_shndx 的特殊取值）
        public const ushort SHN_UNDEF = 0x0000;     // 未定义/无关联节
        public const ushort SHN_LORESERVE = 0xFF00; // 保留索引区下界
        public const ushort SHN_ABS = 0xFFF1;       // 绝对值，不受重定位影响
        public const ushort SHN_COMMON = 0xFFF2;    // 公共(未分配)符号

        // st_info 低 4 位为符号类型（ELF32_ST_TYPE / ELF64_ST_TYPE）
        public const byte ST_TYPE_MASK = 0x0F;

        // 各结构体在 64/32 位下的固定项大小（字节），用于按规范固定宽度读取，不依赖不可信的 sh_entsize
        public const int ProgramHeaderSize64 = 56;
        public const int ProgramHeaderSize32 = 32;
        public const int SectionHeaderSize64 = 64;
        public const int SectionHeaderSize32 = 40;
        public const int SymbolEntrySize64 = 24;  // Elf64_Sym
        public const int SymbolEntrySize32 = 16;  // Elf32_Sym
        public const int DynamicEntrySize64 = 16; // Elf64_Dyn
        public const int DynamicEntrySize32 = 8;  // Elf32_Dyn

        // 不可信计数的预分配上限：容量按需增长，不按伪造的超大计数一次性预分配(防 OOM)
        public const int MaxPreallocCount = 4096;
    }
}
