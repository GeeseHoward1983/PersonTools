using System.Windows.Media.Imaging;

namespace PersonalTools.PEAnalyzer.Models
{
    // 图标视图模型，用于在DataGrid中显示图标
    internal sealed class IconViewModel
    {
        public BitmapSource? ImageSource { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int BitsPerPixel { get; set; }
        public int Size { get; set; }
    }
}