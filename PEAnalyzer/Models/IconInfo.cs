namespace PersonalTools.PEAnalyzer.Models
{
    // 图标信息
    internal sealed class IconInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int BitsPerPixel { get; set; }
        public int Size { get; set; }
        public byte[] Data { get; set; } = [];
    }
}