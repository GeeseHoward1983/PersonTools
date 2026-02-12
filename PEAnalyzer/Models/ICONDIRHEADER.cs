namespace PersonalTools.PEAnalyzer.Models
{
    // 图标目录头
    internal struct ICONDIRHEADER
    {
        public ushort Reserved { get; set; }
        public ushort Type { get; set; }
        public ushort Count { get; set; }
    }
}