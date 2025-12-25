using System.Net;
using System.Text;
using System.Windows;

namespace MyTool
{
    public partial class MainWindow : Window
    {
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
                    string hexString = BitConverter.ToString(bytes).Replace("-", "");
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
        private bool ContainsInvisibleCharacters(byte[] bytes)
        {
            foreach (byte b in bytes)
            {
                // 检查是否为不可见字符（控制字符，除了常见的空格、制表符、换行符）
                if (b < 32 && b != 9 && b != 10 && b != 13) // 9=Tab, 10=Line Feed, 13=Carriage Return
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


        // URL编码
        private void UrlEncode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = UrlInput.Text;
                if (string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("请输入要编码的文本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string result = WebUtility.UrlEncode(input);
                UrlResult.Text = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"URL编码时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // URL解码
        private void UrlDecode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = UrlResult.Text;
                if (string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("请输入要解码的文本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string result = WebUtility.UrlDecode(input);
                UrlInput.Text = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"URL解码时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // URL清空
        private void UrlClear_Click(object sender, RoutedEventArgs e)
        {
            UrlInput.Clear();
            UrlResult.Clear();
        }

    }
}