namespace PersonalTools.PEAnalyzer.Models
{
    /// <summary>
    /// CLR运行时头结构
    /// </summary>
    internal struct IMAGE_COR20_HEADER
    {
        public uint cb;                              // 结构大小
        public ushort MajorRuntimeVersion;           // 主版本号
        public ushort MinorRuntimeVersion;           // 次版本号
        public IMAGE_DATA_DIRECTORY MetaData;        // 元数据
        public uint Flags;                           // 标志位
        public uint EntryPointTokenOrRva;            // 入口点标记或RVA
        public IMAGE_DATA_DIRECTORY Resources;       // 资源
        public IMAGE_DATA_DIRECTORY StrongNameSignature; // 强名称签名
        public IMAGE_DATA_DIRECTORY CodeManagerTable;    // 代码管理器表
        public IMAGE_DATA_DIRECTORY VTableFixups;        // V表修复
        public IMAGE_DATA_DIRECTORY ExportAddressTableJumps; // 导出地址表跳转
        public IMAGE_DATA_DIRECTORY ManagedNativeHeader;     // 托管本地头
    }
}