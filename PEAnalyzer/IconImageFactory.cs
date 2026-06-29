using PersonalTools.PEAnalyzer.Models;
using System.IO;
using System.Windows.Media.Imaging;

namespace PersonalTools.PEAnalyzer
{
    /// <summary>
    /// 从 IconInfo 构建可显示的 IconViewModel（含位图解码）；数据无效或解码失败返回 null。
    /// 把 BitmapImage/MemoryStream 等 WPF 解码细节移出 PEAnalyzerControl，降低控件复杂度与耦合。
    /// </summary>
    internal static class IconImageFactory
    {
        // 单个图标编码字节上限：与 DIB 解码路径(PEResourceParser.Icon.Data)的 10MB 一致，防超大嵌入图/解压炸弹
        private const int MaxIconBytes = 10 * 1024 * 1024;

        // 解码像素上限：恶意图标可声明小尺寸却内嵌巨幅 PNG/BMP，EndInit 会按真实尺寸解码到非托管内存致 OOM
        private const int MaxDecodePixels = 256;

        public static IconViewModel? TryCreate(IconInfo icon)
        {
            if (icon.Data == null || icon.Data.Length == 0 || icon.Width <= 0 || icon.Height <= 0)
            {
                return null;
            }

            // 字节上限：超大图标数据直接拒绝，避免读入/解码巨幅图像
            if (icon.Data.Length > MaxIconBytes)
            {
                PersonalTools.Utils.AppLogger.Log($"图标数据过大({icon.Data.Length} 字节)，已跳过解码");
                return null;
            }

            try
            {
                using MemoryStream stream = new(icon.Data);
                BitmapImage bitmap = new();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // 确保加载完成后可释放流
                // 解码像素上限：限制解码后位图宽度(高度按比例)，防止"声明小、实际巨幅"的图标解码爆内存
                bitmap.DecodePixelWidth = Math.Min(icon.Width, MaxDecodePixels);
                bitmap.EndInit();
                bitmap.Freeze();

                return new IconViewModel
                {
                    Width = icon.Width,
                    Height = icon.Height,
                    BitsPerPixel = icon.BitsPerPixel,
                    Size = icon.Size,
                    ImageSource = bitmap
                };
            }
            // 尽力解码单个图标，任何解码异常都降级为返回 null 不影响其余图标，符合本方法契约。
            // WPF BitmapImage.EndInit() 对损坏图标最常抛 NotSupportedException（.NET 9/10 还可能
            // NullReferenceException/FileFormatException），若不兜住会逃出 DisplayIcons 的 foreach，
            // 导致整个图标列表都不显示。故放宽为 catch (Exception)。
            #pragma warning disable CA1031 // 故意捕获所有异常：契约即“解码失败返回 null”，避免坏图标拖垮整列表
            catch (Exception ex)
            {
                PersonalTools.Utils.AppLogger.Log($"图标解码失败: {ex.Message}");
                return null;
            }
            #pragma warning restore CA1031
        }
    }
}
