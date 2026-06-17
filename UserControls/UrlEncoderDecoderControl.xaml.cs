using System;
using System.Windows;
using System.Windows.Controls;
using PersonalTools.Utils;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// UrlEncoderDecoderControl.xaml 的交互逻辑
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class UrlEncoderDecoderControl : UserControl
    {
        #pragma warning restore CA1515
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
                    MessageHelper.ShowInfo("请输入要编码的URL");
                    return;
                }

                string encoded = Uri.EscapeDataString(input);
                UrlResult.Text = encoded;
            }
            catch (ArgumentNullException ex)
            {
                MessageHelper.ShowError($"URL编码时发生错误: {ex.Message}");
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
                    MessageHelper.ShowInfo("输出框中没有可解码的内容");
                    return;
                }

                string decoded = Uri.UnescapeDataString(input);
                UrlInput.Text = decoded;
            }
            catch (Exception ex) when (ex is ArgumentNullException or UriFormatException)
            {
                MessageHelper.ShowError($"URL解码时发生错误: {ex.Message}");
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