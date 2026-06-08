using System.IO;

namespace PersonalTools.PEAnalyzer.Parsers
{
    /// <summary>
    /// 依赖 DLL 名称 → 完整路径解析：依次在文件所在目录、System32、SysWOW64、Windows 目录中查找。
    /// 找不到返回 null（如私有依赖不在搜索路径中）。
    /// </summary>
    internal static class DependencyResolver
    {
        public static string? Resolve(string dllName, string? baseDir)
        {
            if (string.IsNullOrWhiteSpace(dllName))
            {
                return null;
            }

            foreach (string? dir in EnumerateSearchDirs(baseDir))
            {
                if (string.IsNullOrEmpty(dir))
                {
                    continue;
                }

                try
                {
                    string candidate = Path.Combine(dir, dllName);
                    if (File.Exists(candidate))
                    {
                        return Path.GetFullPath(candidate);
                    }
                }
                catch (ArgumentException)
                {
                    // dllName 含非法路径字符，跳过该候选
                }
            }

            return null;
        }

        private static IEnumerable<string?> EnumerateSearchDirs(string? baseDir)
        {
            yield return baseDir;                       // 与被分析文件同目录（应用目录，最高优先）
            yield return Environment.SystemDirectory;   // System32（64位进程的系统 DLL）

            string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (!string.IsNullOrEmpty(windir))
            {
                yield return Path.Combine(windir, "SysWOW64"); // 32位系统 DLL（WOW64 重定向）
                yield return Path.Combine(windir, "System");   // 16位系统目录（历史遗留）
                yield return windir;                           // Windows 目录
            }

            // PATH 环境变量中的各目录（与 Windows 默认 DLL 搜索顺序一致，置于最后）
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathEnv))
            {
                foreach (string dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    yield return dir;
                }
            }
        }
    }
}
