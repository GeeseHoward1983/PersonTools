namespace PersonalTools.PEAnalyzer.Models
{
    // 图标目录项
    internal struct ICONDIRENTRY
    {
        public byte Width { get; set; }
        public byte Height { get; set; }
        public byte ColorCount { get; set; }
        public byte Reserved { get; set; }
        public ushort Planes { get; set; }
        public ushort BitCount { get; set; }
        public uint BytesInRes { get; set; }
        public uint ImageOffset { get; set; }
    }
}