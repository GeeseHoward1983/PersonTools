using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// Base64EncoderDecoderControl.xaml 的交互逻辑
    /// </summary>
    public partial class Base64EncoderDecoderControl : UserControl
    {
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
                    MessageBox.Show("请输入要编码的文本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                byte[] bytes;

                if (Base64HexInputRadio.IsChecked == true)
                {
                    // Hex字符串模式
                    bytes = Utils.HexStringToByteArray(input);
                }
                else
                {
                    // 普通字符串模式
                    bytes = Encoding.UTF8.GetBytes(input);
                }

                string result = Convert.ToBase64String(bytes);
                Base64Result.Text = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Base64编码时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    MessageBox.Show("请输入要解码的文本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                byte[] bytes = Convert.FromBase64String(input);

                // 检查解码结果中是否包含不可见字符
                string decodedString = Encoding.UTF8.GetString(bytes);

                if (ContainsInvisibleCharacters(bytes))
                {
                    // 如果包含不可见字符，转换为Hex字符串显示
                    string hexString = Utils.ToHexString(bytes);
                    Base64Input.Text = hexString;
                }
                else
                {
                    // 否则显示普通字符串
                    Base64Input.Text = decodedString;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Base64解码时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string filePath = files[0]; // 只处理第一个文件
                    ProcessFileForBase64Encoding(filePath);
                }
            }
        }

        // 处理文件Base64编码
        private void ProcessFileForBase64Encoding(string filePath)
        {
            try
            {
                byte[] fileBytes;

                // 读取文件内容
                using (FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read))
                {
                    fileBytes = new byte[fileStream.Length];
                    fileStream.ReadExactly(fileBytes);
                }

                // 将文件内容显示在输入框中
                Base64Input.Text = Utils.ToHexString(fileBytes);
                // 将文件内容转换为Base64字符串并显示在结果框中
                Base64Result.Text = Convert.ToBase64String(fileBytes);

                Base64HexInputRadio.IsChecked = true;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理文件时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}