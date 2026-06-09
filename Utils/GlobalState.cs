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
            string cultureName = System.Globalization.CultureInfo.CurrentCulture.Name;
            return cultureName switch
            {
                "zh-CN" => LanguageType.SimplifiedChinese,
                "zh-TW" => LanguageType.TraditionalChinese,
                "zh-HK" => LanguageType.TraditionalChinese,
                "zh-MO" => LanguageType.TraditionalChinese,
                _ => LanguageType.English,
            };
        }
    }
}