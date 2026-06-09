using PersonalTools.Enums;
using PersonalTools.Utils;

namespace PersonalTools.ConstString
{
    internal static partial class MacErrno
    {
        // Mac (Darwin/XNU) errno 错误码访问接口
        internal static Dictionary<long, string> MacErrnoMap => GlobalState.CurrentLanguageType switch
        {
            LanguageType.SimplifiedChinese => MacErrnoMapSimplifiedChinese,
            LanguageType.TraditionalChinese => MacErrnoMapTraditionalChinese,
            _ => MacErrnoMapEnglish
        };
    }
}
