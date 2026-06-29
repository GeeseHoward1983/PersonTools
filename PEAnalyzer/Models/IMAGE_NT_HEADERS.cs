namespace PersonalTools.PEAnalyzer.Models
{
    // NT 头聚合视图：由 PEHeaderParser 手工逐字段读取后填充(非整体 Marshal 内存映射)，故不标注 [StructLayout]。
    // 注：可选头不在本结构内——它由 PEParser 单独解析并存于 PEInfo.OptionalHeader(含引用类型 DataDirectory，
    // 不可 blittable 映射)。此处仅保留实际使用的 Signature 与 FileHeader(原 OptionalHeader 字段从未赋值，是死成员，已移除)。
    internal struct IMAGE_NT_HEADERS
    {
        public uint Signature;
        public IMAGE_FILE_HEADER FileHeader;
    }
}