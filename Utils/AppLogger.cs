using System.Globalization;
using System.IO;
using System.Threading;

namespace PersonalTools.Utils
{
    /// <summary>
    /// 轻量文件日志 sink。应用为 <c>OutputType=WinExe</c>（无控制台），<c>Console.WriteLine</c> 写出的
    /// 诊断信息实际丢失；本类把日志追加到 <c>%LocalAppData%/PersonalTools/logs/app.log</c>，
    /// 使解析/导出/兜底处理器记录的异常可被事后排查。线程安全，任何写日志失败都被吞掉，绝不影响主流程。
    /// </summary>
    internal static class AppLogger
    {
        private static readonly object Gate = new();
        private static readonly Lazy<string?> LogFilePath = new(ResolveLogFilePath);

        // 跨进程串行化日志写入：同一工具多开时，避免「判大小→滚动→追加」的 TOCTOU。
        // 构造失败（命名/权限）时为 null，退化为仅进程内 lock，绝不影响主流程。
        private static readonly Lazy<Mutex?> CrossProcessLock = new(CreateCrossProcessMutex);

        // 单个日志文件大小上限：超过则滚动保留一代(.1)，避免长期运行无限增长
        private const long MaxLogBytes = 5L * 1024 * 1024;

        public static void Log(string message)
        {
            string? path = LogFilePath.Value;
            if (path == null)
            {
                return;
            }

#pragma warning disable CA1031 // 日志 sink 自身绝不得抛出影响主流程
            try
            {
                // 清洗 CR/LF 及控制字符：message 常含解析恶意文件得到的异常文本，防止伪造/注入额外日志行
                string sanitized = SanitizeForLog(message);
                string line = string.Create(CultureInfo.InvariantCulture, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {sanitized}{Environment.NewLine}");

                lock (Gate)
                {
                    Mutex? mtx = CrossProcessLock.Value;
                    bool acquired = false;
                    try
                    {
                        if (mtx != null)
                        {
                            try
                            {
                                acquired = mtx.WaitOne(TimeSpan.FromSeconds(2));
                            }
                            catch (AbandonedMutexException)
                            {
                                acquired = true; // 持锁进程崩溃，本进程已获所有权，可继续
                            }
                        }

                        if (File.Exists(path) && new FileInfo(path).Length > MaxLogBytes)
                        {
                            string rolled = path + ".1";
                            File.Delete(rolled);     // 删除上一代(不存在则无操作)
                            File.Move(path, rolled); // 当前转为 .1，保留一代历史而非整删
                        }

                        File.AppendAllText(path, line);
                    }
                    finally
                    {
                        if (acquired && mtx != null)
                        {
                            mtx.ReleaseMutex();
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 写日志失败可忽略
            }
#pragma warning restore CA1031
        }

        private static string? ResolveLogFilePath()
        {
#pragma warning disable CA1031
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PersonalTools", "logs");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "app.log");
            }
            catch (Exception)
            {
                return null; // 无法建日志目录时静默禁用日志
            }
#pragma warning restore CA1031
        }

        private static Mutex? CreateCrossProcessMutex()
        {
#pragma warning disable CA1031 // 构造失败不得影响主流程，退化为仅进程内锁
            try
            {
                return new Mutex(false, @"Global\PersonalTools_AppLogger_app_log");
            }
            catch (Exception)
            {
                return null;
            }
#pragma warning restore CA1031
        }

        // 将 message 中的 CR/LF 及其它 C0 控制字符替换为空格，防止日志行注入
        private static string SanitizeForLog(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return string.Empty;
            }

            return string.Create(message.Length, message, static (span, src) =>
            {
                for (int i = 0; i < src.Length; i++)
                {
                    char c = src[i];
                    span[i] = c < 0x20 ? ' ' : c; // 含 \r \n \t 等控制符统一为空格
                }
            });
        }
    }
}
