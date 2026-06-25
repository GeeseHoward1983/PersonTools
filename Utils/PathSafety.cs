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

            // 含目录分隔符或绝对/UNC 路径 → 非裸名
            if (name.IndexOfAny(['/', '\\']) >= 0 || Path.IsPathRooted(name))
            {
                return false;
            }

            // "." / ".." 是当前/上级目录引用：不含分隔符、非 rooted、GetFileName 又原样返回，
            // 会漏过下面的等值校验。显式拒绝，兑现"含 .. 即拒绝"的防穿越契约。
            if (name == "." || name == "..")
            {
                return false;
            }

            // GetFileName 与原串不等说明含被规范化掉的路径成分；含非法路径字符也按非裸名处理
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            // Windows 保留设备名(CON/PRN/AUX/NUL/COM1-9/LPT1-9，含带扩展名形式如 "NUL.dll")参与文件打开时
            // 会被系统重定向到设备而非目录内文件；尾部点/空格会被 Windows 归一化，导致"校验名≠实际访问名"。一律拒绝。
            if (name[^1] == '.' || name[^1] == ' ' || IsReservedDeviceName(name))
            {
                return false;
            }

            return string.Equals(Path.GetFileName(name), name, StringComparison.Ordinal);
        }

        private static readonly string[] ReservedDeviceNames =
        [
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
        ];

        // 设备名在任意扩展名下均生效（"NUL.dll" 的设备名仍是 NUL），故取首个点之前的基名做比对
        private static bool IsReservedDeviceName(string name)
        {
            int dot = name.IndexOf('.', StringComparison.Ordinal);
            string baseName = dot >= 0 ? name[..dot] : name;
            foreach (string reserved in ReservedDeviceNames)
            {
                if (string.Equals(baseName, reserved, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
