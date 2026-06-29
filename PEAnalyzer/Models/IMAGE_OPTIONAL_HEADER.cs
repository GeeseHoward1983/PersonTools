namespace PersonalTools.PEAnalyzer.Models
{
    // 可选头聚合视图：含引用类型数组 DataDirectory 且为属性式结构，不可用 Marshal.PtrToStructure
    // 整体映射，由 PEHeaderParser 手工逐字段读取后填充（非内存布局体，故不标注 [StructLayout]）。
    internal struct IMAGE_OPTIONAL_HEADER
    {
        public ushort Magic { get; set; }
        public byte MajorLinkerVersion { get; set; }
        public byte MinorLinkerVersion { get; set; }
        public uint SizeOfCode { get; set; }
        public uint SizeOfInitializedData { get; set; }
        public uint SizeOfUninitializedData { get; set; }
        public uint AddressOfEntryPoint { get; set; }
        public uint BaseOfCode { get; set; }
        public uint BaseOfData { get; set; }
        public ulong ImageBase { get; set; }
        public uint SectionAlignment { get; set; }
        public uint FileAlignment { get; set; }
        public ushort MajorOperatingSystemVersion { get; set; }
        public ushort MinorOperatingSystemVersion { get; set; }
        public ushort MajorImageVersion { get; set; }
        public ushort MinorImageVersion { get; set; }
        public ushort MajorSubsystemVersion { get; set; }
        public ushort MinorSubsystemVersion { get; set; }
        public uint Win32VersionValue { get; set; }
        public uint SizeOfImage { get; set; }
        public uint SizeOfHeaders { get; set; }
        public uint CheckSum { get; set; }
        public ushort Subsystem { get; set; }
        public ushort DllCharacteristics { get; set; }
        public ulong SizeOfStackReserve { get; set; }
        public ulong SizeOfStackCommit { get; set; }
        public ulong SizeOfHeapReserve { get; set; }
        public ulong SizeOfHeapCommit { get; set; }
        public uint LoaderFlags { get; set; }
        public uint NumberOfRvaAndSizes { get; set; }
        public IMAGE_DATA_DIRECTORY[] DataDirectory { get; set; }
    }
}