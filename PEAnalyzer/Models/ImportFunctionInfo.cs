namespace PersonalTools.PEAnalyzer.Models
{
    // 导入函数信息
    internal sealed class ImportFunctionInfo
    {
        public string DllName { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public int Ordinal { get; set; }
        public bool IsOrdinalImport { get; set; }
        public bool IsDelayLoaded { get; set; }   // 添加延迟加载标记

        // 添加序号显示属性，同时显示十进制和十六进制
        public string OrdinalDisplay => $"{Ordinal} (0x{Ordinal:X8})";
    }
}