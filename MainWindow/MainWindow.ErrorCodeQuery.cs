using System.Windows;
using System.Windows.Controls;

namespace MyTool
{
    public partial class MainWindow : Window
    {
        // Windows errno 查询按钮点击事件
        private void WindowsErrnoQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(WindowsErrnoInput.Text, ConstString.WindowsStandardErrnoMap, WindowsErrnoResult);
        }

        // Linux errno 查询按钮点击事件
        private void LinuxErrnoQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(LinuxErrnoInput.Text, ConstString.LinuxErrnoMap, LinuxErrnoResult);
        }

        // Mac errno 查询按钮点击事件
        private void MacErrnoQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(MacErrnoInput.Text, ConstString.MacErrnoMap, MacErrnoResult);
        }

        // HTTP 状态码查询按钮点击事件
        private void HttpStatusCodeQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(HttpStatusCodeInput.Text, ConstString.HttpStatusMap, HttpStatusCodeResult);
        }

        // SQL Server 错误码查询按钮点击事件
        private void SqlServerErrorQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(SqlServerErrorInput.Text, ConstString.SqlServerErrorsMap, SqlServerErrorResult);
        }

        // MySQL 错误码查询按钮点击事件
        private void MySqlErrorQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(MySqlErrorInput.Text, ConstString.MySqlErrorsMap, MySqlErrorResult);
        }

        // Oracle SQLCODE 查询按钮点击事件
        private void OracleSqlCodeQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(OracleSqlCodeInput.Text, ConstString.OracleSqlCodeMap, OracleSqlCodeResult);
        }

        // ODBC 错误码查询按钮点击事件
        private void OdbcErrorQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(OdbcErrorInput.Text, ConstString.OdbcErrorsMap, OdbcErrorResult);
        }

        // Windows 系统错误码查询按钮点击事件
        private void WindowsSystemErrorQuery_Click(object sender, RoutedEventArgs e)
        {
            QueryErrorCode(WindowsSystemErrorInput.Text, ConstString.WindowsSystemErrorsMap, WindowsSystemErrorResult);
        }

        // 通用错误码查询方法
        private static void QueryErrorCode(string input, Dictionary<long, string> errorCodeMap, TextBlock resultTextBlock)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                resultTextBlock.Text = "请输入错误码";
                return;
            }

            if (uint.TryParse(input.Trim(), out uint errorCode))
            {
                if (errorCodeMap.TryGetValue(errorCode, out string? errorMessage))
                {
                    resultTextBlock.Text = $"错误码: {errorCode}\n错误信息: {errorMessage}";
                }
                else
                {
                    resultTextBlock.Text = $"未找到错误码 {errorCode} 的相关信息";
                }
            }
            else
            {
                resultTextBlock.Text = "输入的不是有效的数字";
            }
        }

        private static void QueryErrorCode(string input, Dictionary<string, string> errorCodeMap, TextBlock resultTextBlock)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                resultTextBlock.Text = "请输入错误码";
                return;
            }

            if (errorCodeMap.TryGetValue(input, out string? errorMessage))
            {
                resultTextBlock.Text = $"错误码: {input}\n错误信息: {errorMessage}";
            }
            else
            {
                resultTextBlock.Text = $"未找到错误码 {input} 的相关信息";
            }
        }

    }
}