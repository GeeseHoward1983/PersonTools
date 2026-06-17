using System.Windows;
using PersonalTools.Utils;

namespace PersonalTools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class App : Application
    {
        #pragma warning restore CA1515
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 全局未处理异常兜底：避免解析/控件抛出的异常导致应用静默崩溃
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += App_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageHelper.ShowError($"发生未处理的错误：{e.Exception.Message}");
            e.Handled = true; // 标记为已处理，避免应用崩溃
        }

        private static void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageHelper.ShowError($"发生严重错误：{ex.Message}");
            }
        }
    }

}