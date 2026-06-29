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
            string input = UrlInput.Text;
            if (string.IsNullOrEmpty(input))
            {
                MessageHelper.ShowInfo("请输入要编码的URL");
                return;
            }

            // Uri.EscapeDataString 在 net10 仅会因入参为 null 抛 ArgumentNullException
            //（UriFormatException 的“长度超 32766”限制是 .NET Framework only，net10 已移除）。
            // input 取自 TextBox.Text，永不为 null 且上方已判空，原 catch 为不可达死捕获，故移除 try。
            string encoded = Uri.EscapeDataString(input);
            UrlResult.Text = encoded;
        }

        // URL解码
        private void UrlDecode_Click(object sender, RoutedEventArgs e)
        {
            string input = UrlResult.Text;
            if (string.IsNullOrEmpty(input))
            {
                MessageHelper.ShowInfo("输出框中没有可解码的内容");
                return;
            }

            // Uri.UnescapeDataString 在 net10 仅会因入参为 null 抛 ArgumentNullException，从不抛 UriFormatException
            //（非法转义序列会原样保留而非校验）。input 取自 TextBox.Text，永不为 null 且上方已判空，
            // 原 catch 的 ArgumentNullException/UriFormatException 均为不可达死捕获，故移除 try。
            string decoded = Uri.UnescapeDataString(input);
            UrlInput.Text = decoded;
        }

        // URL清空
        private void UrlClear_Click(object sender, RoutedEventArgs e)
        {
            UrlInput.Clear();
            UrlResult.Clear();
        }
    }
}