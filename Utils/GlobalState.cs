using PersonalTools.Enums;

namespace PersonalTools.Utils
{
    /// <summary>
    /// 全局状态管理类
    /// 存储应用程序级别的全局状态信息
    /// </summary>
    internal static class GlobalState
    {
        /// <summary>
        /// 当前语言类型（程序启动时求值一次并缓存）
        /// </summary>
        public static LanguageType CurrentLanguageType { get; } = GetCurrentLanguageType();
        /// <summary>
        /// 获取当前系统的语言类型
        /// </summary>
        /// <returns>语言类型枚举</returns>
        private static LanguageType GetCurrentLanguageType()
        {
            System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CurrentCulture;
            if (!string.Equals(culture.TwoLetterISOLanguageName, "zh", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageType.English;
            }

            // 中文按脚本/区域细分简繁：繁体 = zh-Hant*（含中性）、台/港/澳区域及旧式 zh-CHT；
            // 其余中文（zh-CN、zh-SG、zh-Hans*、中性 zh 等）归简体，避免 zh-SG/zh-Hans 被漏判为英文
            string name = culture.Name;
            bool isTraditional = name.StartsWith("zh-Hant", StringComparison.OrdinalIgnoreCase)
                || name is "zh-TW" or "zh-HK" or "zh-MO" or "zh-CHT";
            return isTraditional ? LanguageType.TraditionalChinese : LanguageType.SimplifiedChinese;
        }
    }
}