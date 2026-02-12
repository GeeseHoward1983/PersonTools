using System;
using System.Windows;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// UrlEncoderDecoderControl.xaml 的交互逻辑
    /// </summary>
    public partial class UrlEncoderDecoderControl : UserControl
    {
        public UrlEncoderDecoderControl()
        {
            InitializeComponent();
        }

        // URL编码
        private void UrlEncode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = UrlInput.Text;
                if (string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("请输入要编码的URL", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string encoded = Uri.EscapeDataString(input);
                UrlResult.Text = encoded;
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show($"URL编码时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
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
                    MessageBox.Show("输出框中没有可解码的内容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string decoded = Uri.UnescapeDataString(input);
                UrlInput.Text = decoded;
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show($"URL解码时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (UriFormatException ex)
            {
                MessageBox.Show($"URL解码时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
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