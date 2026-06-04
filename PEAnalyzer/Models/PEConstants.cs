namespace PersonalTools.PEAnalyzer.Models
{
    /// <summary>
    /// PE 文件格式相关的常量集合。
    /// 集中管理签名、可选头魔数、结构体大小与数据目录索引，避免在解析器中散落魔法数字。
    /// </summary>
    internal static class PEConstants
    {
        // 签名
        public const ushort DosSignature = 0x5A4D;   // "MZ"
        public const uint NtSignature = 0x00004550;  // "PE\0\0"

        // 可选头魔数（区分 32/64 位）
        public const ushort Pe32Magic = 0x10b;       // PE32   (32 位)
        public const ushort Pe32PlusMagic = 0x20b;   // PE32+  (64 位)

        // 可选头中数据目录之前的固定部分大小（字节）
        public const int Pe32OptionalHeaderBaseSize = 96;
        public const int Pe32PlusOptionalHeaderBaseSize = 112;

        // 数据目录
        public const int MaxDataDirectories = 16;
        public const int DataDirectoryEntrySize = 8;

        // 各结构体大小（字节）
        public const int SectionHeaderSize = 40;
        public const int ImportDescriptorSize = 20;
        public const int DelayLoadDescriptorSize = 32;
        public const int ExportDirectorySize = 40;
        public const int Cor20HeaderMinSize = 72;

        // 导入 thunk 的序号导入标志位
        public const uint OrdinalFlag32 = 0x80000000U;
        public const ulong OrdinalFlag64 = 0x8000000000000000UL;

        // 数据目录索引（IMAGE_DIRECTORY_ENTRY_*）
        public const int DirectoryExport = 0;
        public const int DirectoryImport = 1;
        public const int DirectoryResource = 2;
        public const int DirectorySecurity = 4;
        public const int DirectoryDelayImport = 13;
        public const int DirectoryClrHeader = 14;
    }
}
