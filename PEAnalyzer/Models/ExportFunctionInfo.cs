namespace PersonalTools.PEAnalyzer.Models
{
    // 导出函数信息
    internal sealed class ExportFunctionInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Ordinal { get; set; }
        public uint RVA { get; set; }

        // 添加序号显示属性，同时显示十进制和十六进制
        public string OrdinalDisplay => $"{Ordinal} (0x{Ordinal:X8})";
    }
}