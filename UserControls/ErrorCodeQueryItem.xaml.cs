using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// ErrorCodeQueryItem.xaml 的交互逻辑
    /// </summary>
    public partial class ErrorCodeQueryItem : UserControl
    {
        // 依赖属性定义
        public static readonly DependencyProperty ErrorCodeMapLongProperty =
            DependencyProperty.Register("ErrorCodeMap", typeof(Dictionary<long, string>), typeof(ErrorCodeQueryItem));

        public static readonly DependencyProperty ErrorCodeMapStringProperty =
            DependencyProperty.Register("ErrorCodeMapString", typeof(Dictionary<string, string>), typeof(ErrorCodeQueryItem));

        public static readonly DependencyProperty QueryCommandProperty =
            DependencyProperty.Register("QueryCommand", typeof(ICommand), typeof(ErrorCodeQueryItem));

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

        public ICommand QueryCommand
        {
            get => (ICommand)GetValue(QueryCommandProperty);
            set => SetValue(QueryCommandProperty, value);
        }

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
            if (QueryCommand != null && QueryCommand.CanExecute(null))
            {
                QueryCommand.Execute(null);
                return;
            }

            string input = ErrorCodeInput.Text;

            if (string.IsNullOrWhiteSpace(input))
            {
                ResultTextBox.Text = "请输入错误码";
                return;
            }

            // 首先尝试使用数字类型的错误码字典
            if (ErrorCodeMap != null)
            {
                ResultTextBox.Text = long.TryParse(input.Trim(), out long errorCode)
                    ? ErrorCodeMap.TryGetValue(errorCode, out string? errorMessage)
                        ? $"错误码: {errorCode}\n错误信息: {errorMessage}"
                        : $"未找到错误码 {errorCode} 的相关信息"
                    : "输入的不是有效的数字";
            }
            else
            {
                ResultTextBox.Text = ErrorCodeMapString != null
                    ? ErrorCodeMapString.TryGetValue(input, out string? errorMessage)
                    ? $"错误码: {input}\n错误信息: {errorMessage}"
                    : $"未找到错误码 {input} 的相关信息"
                    : "错误码字典未设置";
            }
        }
    }
}