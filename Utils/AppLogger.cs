using System.Globalization;
using System.IO;

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

        // 单个日志文件大小上限：超过则截断重建，避免长期运行无限增长
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
                lock (Gate)
                {
                    if (File.Exists(path) && new FileInfo(path).Length > MaxLogBytes)
                    {
                        File.Delete(path);
                    }

                    string line = string.Create(CultureInfo.InvariantCulture, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
                    File.AppendAllText(path, line);
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
    }
}
