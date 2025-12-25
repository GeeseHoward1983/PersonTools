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

                byte[] bytes = Encoding.UTF8.GetBytes(input);
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
                string result = Encoding.UTF8.GetString(bytes);
                Base64Input.Text = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Base64解码时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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