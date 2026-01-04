using PersonalTools.Enums;

namespace PersonalTools
{
    /// <summary>
    /// 全局状态管理类
    /// 存储应用程序级别的全局状态信息
    /// </summary>
    public static class GlobalState
    {
        /// <summary>
        /// 当前语言类型
        /// 在程序启动时初始化
        /// </summary>
        public static LanguageType CurrentLanguageType { get; set; } = GetCurrentLanguageType();

        /// <summary>
        /// 获取当前系统的语言类型
        /// </summary>
        /// <returns>语言类型枚举</returns>
        private static LanguageType GetCurrentLanguageType()
        {
            var cultureName = System.Globalization.CultureInfo.CurrentCulture.Name;
            if (cultureName.Equals("zh-CN", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageType.SimplifiedChinese;
            }
            else if (cultureName.Equals("zh-TW", StringComparison.OrdinalIgnoreCase) ||
                     cultureName.Equals("zh-HK", StringComparison.OrdinalIgnoreCase) ||
                     cultureName.Equals("zh-MO", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageType.TraditionalChinese;
            }
            else
            {
                return LanguageType.English;
            }
        }
    }
}