namespace PersonalTools.ELFAnalyzer.Models
{
    // 不用于 Marshal（字段由解析器逐字段填充），故不加 [StructLayout(Pack=1)]（死属性且误导）。
    internal struct ELFDynamic
    {
        public long d_tag;     // Dynamic entry type
        public ulong d_val;    // Integer value
    }
}