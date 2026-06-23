using System.IO;

namespace PersonalTools.Utils
{
    /// <summary>
    /// 路径安全判定的集中工具。被分析 PE/ELF 的导入表等不可信来源中的"模块名"必须为纯文件名，
    /// 含目录分隔符 / .. / 绝对路径 / UNC 时若参与 Path.Combine 会跳出预期搜索目录，
    /// 造成任意路径探测，UNC 还会触发出站 SMB 泄露凭据。此处单点封装，供各解析器统一调用。
    /// </summary>
    internal static class PathSafety
    {
        /// <summary>
        /// 判断 <paramref name="name"/> 是否为"裸文件名"（不含任何目录成分、非绝对路径/UNC、不含非法路径字符）。
        /// </summary>
        public static bool IsBareFileName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            // 含目录分隔符或 .. 或绝对/UNC 路径 → 非裸名
            if (name.IndexOfAny(['/', '\\']) >= 0 || Path.IsPathRooted(name))
            {
                return false;
            }

            // GetFileName 与原串不等说明含被规范化掉的路径成分；含非法路径字符也按非裸名处理
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            return string.Equals(Path.GetFileName(name), name, StringComparison.Ordinal);
        }
    }
}
