using System.IO;

namespace PersonalTools.PEAnalyzer.Parsers
{
    /// <summary>
    /// 依赖 DLL 名称 → 完整路径解析：依次在文件所在目录、System32/SysWOW64、Windows 目录、PATH 中查找。
    /// 按目标 PE 位数优先匹配的系统目录（32位优先 SysWOW64，64位优先 System32）。找不到返回 null。
    /// </summary>
    internal static class DependencyResolver
    {
        /// <param name="targetIs64Bit">目标 PE 位数：true=64位优先 System32；false=32位优先 SysWOW64；null=未知，按 System32 优先。</param>
        public static string? Resolve(string dllName, string? baseDir, bool? targetIs64Bit = null)
        {
            if (string.IsNullOrWhiteSpace(dllName))
            {
                return null;
            }

            foreach (string? dir in EnumerateSearchDirs(baseDir, targetIs64Bit))
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

        private static IEnumerable<string?> EnumerateSearchDirs(string? baseDir, bool? targetIs64Bit)
        {
            yield return baseDir;                            // 与被分析文件同目录（应用目录，最高优先）

            string system32 = Environment.SystemDirectory;  // 本进程为64位 → 真实 System32（64位系统 DLL）
            string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string? sysWow64 = string.IsNullOrEmpty(windir) ? null : Path.Combine(windir, "SysWOW64"); // 32位系统 DLL

            // 按目标位数决定 System32 / SysWOW64 的优先顺序
            if (targetIs64Bit == false)
            {
                yield return sysWow64;   // 32位目标优先 SysWOW64
                yield return system32;
            }
            else
            {
                yield return system32;   // 64位/未知优先 System32
                yield return sysWow64;
            }

            if (!string.IsNullOrEmpty(windir))
            {
                yield return Path.Combine(windir, "System"); // 16位系统目录（历史遗留）
                yield return windir;                         // Windows 目录
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
