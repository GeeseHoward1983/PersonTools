using PersonalTools.ConstString;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// ErrorCodeQueryControl.xaml 的交互逻辑
    /// </summary>
    public partial class ErrorCodeQueryControl : UserControl
    {
        public ErrorCodeQueryControl()
        {
            InitializeComponent();
            
            // 初始化各个ErrorCodeQueryItem的错误码字典
            InitializeErrorCodeItems();
        }

        private void InitializeErrorCodeItems()
        {
            // Windows errno
            WindowsErrnoQueryItem.ErrorCodeMap = WindowsStandardErrno.WindowsStandardErrnoMap;
            
            // Linux errno
            LinuxErrnoQueryItem.ErrorCodeMap = LinuxErrno.LinuxErrnoMap;
            
            // Mac errno
            MacErrnoQueryItem.ErrorCodeMap = MacErrno.MacErrnoMap;
            
            // HTTP 状态码
            HttpStatusCodeQueryItem.ErrorCodeMap = HttpStatus.HttpStatusMap;
            
            // SQL Server 错误码
            SqlServerQueryItem.ErrorCodeMap = SqlServerErrors.SqlServerErrorsMap;
            
            // MySQL 错误码
            MySqlQueryItem.ErrorCodeMap = MySqlErrors.MySqlErrorsMap;
            
            // Oracle SQLCODE
            OracleQueryItem.ErrorCodeMap = OracleSqlCode.OracleSqlCodeMap;
            
            // ODBC 错误码
            OdbcQueryItem.ErrorCodeMapString = OdbcErrors.OdbcErrorsMap;
            
            // Windows 系统错误码
            WindowsSystemErrorQueryItem.ErrorCodeMap = WindowsSystemErrors.WindowsSystemErrorsMap;
        }
    }
}