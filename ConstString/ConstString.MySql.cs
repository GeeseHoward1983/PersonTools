using MyTool.Enums;

namespace MyTool
{
    public static partial class ConstString
    {
        // MySQL 错误码访问接口
        public static Dictionary<long, string> MySqlErrorsMap
        {
            get
            {
                return GlobalState.CurrentLanguageType switch
                {
                    LanguageType.SimplifiedChinese => MySqlErrorsMapSimplifiedChinese,
                    LanguageType.TraditionalChinese => MySqlErrorsMapTraditionalChinese,
                    _ => MySqlErrorsMapEnglish
                };
            }
        }
    }
}