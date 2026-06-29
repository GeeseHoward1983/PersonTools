using System.IO;
using System.Windows;

namespace PersonalTools.Utils
{
    /// <summary>
    /// 文件拖放共享辅助：取首个拖入文件 + 读全部字节，消除各控件里重复的拖放/读取样板。
    /// </summary>
    internal static class FileDropHelper
    {
        /// <summary>从拖放事件取第一个文件路径；无文件返回 null。</summary>
        public static string? GetFirstDroppedFile(DragEventArgs e)
        {
            return e.Data.GetDataPresent(DataFormats.FileDrop) switch
            {
                true when e.Data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } files => files[0],
                _ => null,
            };
        }

        // hex/Base64 展示到 TextBox 的实用大小上限：超过则提示用户取消，避免数百 MB 文件转成巨串后卡死 UI 渲染。
        // 远低于 ReadAllBytes 的 2GB 硬上限，目的是可用性而非内存安全。
        public const long HexDisplayWarnBytes = 50L * 1024 * 1024;

        /// <summary>拖放文件用于 hex/Base64 展示前的大小预检：≤上限返回 true；取大小失败时返回 true(不阻断，交后续读盘异常处理)。</summary>
        public static bool IsWithinHexDisplayLimit(string filePath)
        {
            try
            {
                return new FileInfo(filePath).Length <= HexDisplayWarnBytes;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return true;
            }
        }

        /// <summary>读取整个文件的字节。</summary>
        public static byte[] ReadAllBytes(string filePath)
        {
            using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
            // 单个 byte[] 上限为 int.MaxValue：超 2GB 文件直接以可控的 IOException 报错，避免 new byte[long] 抛 OverflowException
            if (fileStream.Length > int.MaxValue)
            {
                throw new IOException($"文件过大（{fileStream.Length} 字节），无法一次性加载到内存");
            }

            byte[] bytes = new byte[fileStream.Length];
            fileStream.ReadExactly(bytes);
            return bytes;
        }
    }
}
