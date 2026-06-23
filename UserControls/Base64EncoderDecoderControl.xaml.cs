using System.IO;
using System.Windows;
using System.Windows.Controls;
using PersonalTools.Utils;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// Base64EncoderDecoderControl.xaml 的交互逻辑
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class Base64EncoderDecoderControl : UserControl
    {
        #pragma warning restore CA1515
        public Base64EncoderDecoderControl()
        {
            InitializeComponent();
        }
        private void Grid_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;
        }

        // Base64编码
        private void Base64Encode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = Base64Input.Text;
                if (string.IsNullOrEmpty(input))
                {
                    MessageHelper.ShowInfo("请输入要编码的文本");
                    return;
                }

                byte[] bytes = ConvertUtils.InputBytes(input, Base64HexInputRadio.IsChecked == true);

                string result = Convert.ToBase64String(bytes);
                Base64Result.Text = result;
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException or ObjectDisposedException)
            {
                MessageHelper.ShowError($"Base64编码时发生错误: {ex.Message}");
            }
        }

        // Base64解码
        private void Base64Decode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = Base64Result.Text;
                if (string.IsNullOrEmpty(input))
                {
                    MessageHelper.ShowInfo("请输入要解码的文本");
                    return;
                }

                byte[] bytes = Convert.FromBase64String(input);

                // 含不可见字符时转 Hex 显示避免乱码，否则按 UTF-8 文本显示
                Base64Input.Text = ConvertUtils.OutputString(bytes, ContainsInvisibleCharacters(bytes));
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException or ObjectDisposedException)
            {
                MessageHelper.ShowError($"Base64解码时发生错误: {ex.Message}");
            }
        }

        // 检查字节数组是否包含不可见字符
        private static bool ContainsInvisibleCharacters(byte[] bytes)
        {
            foreach (byte b in bytes)
            {

                // 检查是否为不可见字符（控制字符，除了常见的空格、制表符、换行符）
                if (b is < 32 and not 9 and not 10 and not 13) // 9=Tab, 10=Line Feed, 13=Carriage Return
                {
                    return true;
                }
            }
            return false;
        }

        // Base64清空
        private void Base64Clear_Click(object sender, RoutedEventArgs e)
        {
            Base64Input.Clear();
            Base64Result.Clear();
        }

        // 处理Base64标签页的文件拖放事件
        private void Base64Tab_Drop(object sender, DragEventArgs e)
        {
            string? filePath = FileDropHelper.GetFirstDroppedFile(e);
            if (filePath != null)
            {
                ProcessFileForBase64Encoding(filePath);
            }
        }

        // 处理文件Base64编码
        private void ProcessFileForBase64Encoding(string filePath)
        {
            try
            {
                byte[] fileBytes = FileDropHelper.ReadAllBytes(filePath);

                // 输入框显示 hex，结果框显示 Base64，并切到 Hex 模式
                Base64Input.Text = ConvertUtils.ToHexString(fileBytes);
                Base64Result.Text = Convert.ToBase64String(fileBytes);
                Base64HexInputRadio.IsChecked = true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageHelper.ShowError($"处理文件时发生错误: {ex.Message}");
            }
        }
    }
}