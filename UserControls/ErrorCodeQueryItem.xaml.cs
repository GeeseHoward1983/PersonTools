using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// ErrorCodeQueryItem.xaml 的交互逻辑
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class ErrorCodeQueryItem : UserControl
    {
        #pragma warning restore CA1515
        // 依赖属性定义
        public static readonly DependencyProperty ErrorCodeMapLongProperty =
            DependencyProperty.Register("ErrorCodeMap", typeof(Dictionary<long, string>), typeof(ErrorCodeQueryItem));

        public static readonly DependencyProperty ErrorCodeMapStringProperty =
            DependencyProperty.Register("ErrorCodeMapString", typeof(Dictionary<string, string>), typeof(ErrorCodeQueryItem));

        #pragma warning disable CA2227 // 符合WPF依赖属性要求，需要公共setter
        public Dictionary<long, string> ErrorCodeMap
        {
            get => (Dictionary<long, string>)GetValue(ErrorCodeMapLongProperty);
            set => SetValue(ErrorCodeMapLongProperty, value);
        }

        public Dictionary<string, string> ErrorCodeMapString
        {
            get => (Dictionary<string, string>)GetValue(ErrorCodeMapStringProperty);
            set => SetValue(ErrorCodeMapStringProperty, value);
        }
        #pragma warning restore CA2227

        public ErrorCodeQueryItem()
        {
            InitializeComponent();
        }

        // 查询按钮点击事件
        private void QueryButton_Click(object sender, RoutedEventArgs e)
        {
            PerformQuery();
        }

        // 输入框回车键事件
        private void ErrorCodeInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformQuery();
            }
        }

        // 执行查询
        private void PerformQuery()
        {
            string input = ErrorCodeInput.Text.Trim();

            // 首先尝试使用数字类型的错误码字典
            ResultTextBox.Text = string.IsNullOrWhiteSpace(input) switch
            {
                true => "请输入错误码",
                _ => ErrorCodeMap switch
                {
                    null => ErrorCodeMapString switch
                    {
                        null => "错误码字典未设置",
                        _ => ErrorCodeMapString.TryGetValue(input, out string? errorMessage) switch
                        {
                            true => $"错误码: {input}\n错误信息: {errorMessage}",
                            false => $"未找到错误码 {input} 的相关信息"
                        }
                    },
                    _ => TryParseErrorCode(input, out long errorCode) switch
                    {
                        true => ErrorCodeMap.TryGetValue(errorCode, out string? errorMessage) switch
                        {
                            true => $"错误码: {errorCode}\n��误信息: {errorMessage}",
                            false => $"未找到错误码 {errorCode} 的相关信息"
                        },
                        false => "输入的不是有效的数字"
                    }
                }
            };
        }

        // 同时支持十进制与十六进制(0x/0X 前缀)错误码输入：十六进制按无符号解析为正 long，
        // 与字典中以正值存储的 Win32/系统错误码匹配（不对 HRESULT/NTSTATUS 做有符号 int32 扩展）。
        private static bool TryParseErrorCode(string input, out long value)
        {
            return input.StartsWith("0x", StringComparison.OrdinalIgnoreCase) switch
            {
                true => long.TryParse(input.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value),
                _ => long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)
            };
        }
    }
}