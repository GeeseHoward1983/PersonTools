using PersonalTools;
using PersonalTools.Enums;

namespace PersonalTools.ConstString
{
    internal static partial class LinuxErrno
    {
        // Linux errno 错误码访问接口
        internal static Dictionary<long, string> LinuxErrnoMap => GlobalState.CurrentLanguageType switch
        {
            LanguageType.SimplifiedChinese => LinuxErrnoMapSimplifiedChinese,
            LanguageType.TraditionalChinese => LinuxErrnoMapTraditionalChinese,
            _ => LinuxErrnoMapEnglish
        };
    }
}