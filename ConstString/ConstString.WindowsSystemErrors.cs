using MyTool.Enums;

namespace MyTool
{
    public static partial class ConstString
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