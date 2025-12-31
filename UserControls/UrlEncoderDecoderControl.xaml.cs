using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace MyTool.UserControls
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