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
        public IMAGEDATADIRECTORY MetaData;        // 元数据
        public uint Flags;                           // 标志位
        public uint EntryPointTokenOrRva;            // 入口点标记或RVA
        public IMAGEDATADIRECTORY Resources;       // 资源
        public IMAGEDATADIRECTORY StrongNameSignature; // 强名称签名
        public IMAGEDATADIRECTORY CodeManagerTable;    // 代码管理器表
        public IMAGEDATADIRECTORY VTableFixups;        // V表修复
        public IMAGEDATADIRECTORY ExportAddressTableJumps; // 导出地址表跳转
        public IMAGEDATADIRECTORY ManagedNativeHeader;     // 托管本地头
    }
}