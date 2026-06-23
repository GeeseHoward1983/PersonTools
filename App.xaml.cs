using System.IO;
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

        protected override void OnExit(ExitEventArgs e)
        {
            // 统一清理 Markdown 预览临时目录：预览文件不再随每次切 Tab 删除(避免切回空白)，改在退出时整目录清理
            CleanupPreviewTempDir();
            base.OnExit(e);
        }

        private static void CleanupPreviewTempDir()
        {
#pragma warning disable CA1031 // 退出清理失败无关紧要，吞掉任何异常
            try
            {
                string previewDir = Path.Combine(Path.GetTempPath(), "PersonalTools");
                if (Directory.Exists(previewDir))
                {
                    Directory.Delete(previewDir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                PersonalTools.Utils.AppLogger.Log($"清理预览临时目录失败: {ex.Message}");
            }
#pragma warning restore CA1031
        }

        private static void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // 先标记已处理避免再入崩溃，再弹窗；弹窗本身异常时降级为日志，确保兜底处理器自身不再抛出
            e.Handled = true;
#pragma warning disable CA1031 // 兜底处理器自身不得再抛：弹窗失败时吞掉任何异常，降级为日志
            try
            {
                // 仅向用户展示通用提示，完整异常(含路径/内部类型等敏感细节)写入日志而非弹窗
                PersonalTools.Utils.AppLogger.Log($"[DispatcherUnhandledException] {e.Exception}");
                MessageHelper.ShowError("发生未处理的错误，操作未能完成。");
            }
            catch (Exception ex)
            {
                PersonalTools.Utils.AppLogger.Log($"错误提示弹窗失败: {ex.Message}");
            }
#pragma warning restore CA1031
        }

        private static void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                // 此回调常在进程即将终止阶段触发：用 BeginInvoke 异步封送时消息泵往往来不及处理，
                // 用户看不到提示等于静默崩溃。MessageBox.Show 是阻塞式 Win32 模态、可跨线程直接调用，
                // 故同步弹窗确保提示一定可见；完整异常写日志，仅给用户通用提示。
                PersonalTools.Utils.AppLogger.Log($"[UnhandledException] {ex}");
#pragma warning disable CA1031 // 兜底处理器自身不得再抛：弹窗失败时吞掉任何异常，降级为日志
                try
                {
                    MessageHelper.ShowError("发生严重错误。");
                }
                catch (Exception inner)
                {
                    PersonalTools.Utils.AppLogger.Log($"错误提示弹窗失败: {inner.Message}");
                }
#pragma warning restore CA1031
            }
        }
    }

}