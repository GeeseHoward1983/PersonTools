using PersonalTools.Enums;

namespace PersonalTools.ConstString
{
    public static partial class MySqlErrors
    {
        // MySQL 错误码访问接口
        public static Dictionary<long, string> MySqlErrorsMap => GlobalState.CurrentLanguageType switch
        {
            LanguageType.SimplifiedChinese => MySqlErrorsMapSimplifiedChinese,
            LanguageType.TraditionalChinese => MySqlErrorsMapTraditionalChinese,
            _ => MySqlErrorsMapEnglish
        };
    }
}