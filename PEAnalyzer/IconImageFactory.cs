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
        public static IconViewModel? TryCreate(IconInfo icon)
        {
            if (icon.Data == null || icon.Data.Length == 0 || icon.Width <= 0 || icon.Height <= 0)
            {
                return null;
            }

            try
            {
                using MemoryStream stream = new(icon.Data);
                BitmapImage bitmap = new();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // 确保加载完成后可释放流
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
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                PersonalTools.Utils.AppLogger.Log($"图标解码失败: {ex.Message}");
                return null;
            }
        }
    }
}
