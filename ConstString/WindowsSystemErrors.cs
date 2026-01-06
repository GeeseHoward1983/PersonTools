using PersonalTools;
using PersonalTools.Enums;

namespace PersonalTools.ConstString
{
    public static partial class WindowsSystemErrors
    {
        // Linux errno 错误码访问接口
        public static Dictionary<long, string> WindowsSystemErrorsMap
        {
            get
            {
                return GlobalState.CurrentLanguageType switch
                {
                    LanguageType.SimplifiedChinese => WindowsSystemErrorsMapSimplifiedChinese,
                    LanguageType.TraditionalChinese => WindowsSystemErrorsMapTraditionalChinese,
                    _ => WindowsSystemErrorsMapEnglish
                };
            }
        }
    }
}