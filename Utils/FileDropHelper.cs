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

        /// <summary>读取整个文件的字节。</summary>
        public static byte[] ReadAllBytes(string filePath)
        {
            using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read);
            byte[] bytes = new byte[fileStream.Length];
            fileStream.ReadExactly(bytes);
            return bytes;
        }
    }
}
