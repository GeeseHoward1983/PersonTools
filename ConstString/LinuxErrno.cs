using PersonalTools;
using PersonalTools.Enums;

namespace PersonalTools.ConstString
{
    public static partial class LinuxErrno
    {
        // Linux errno 错误码访问接口
        public static Dictionary<long, string> LinuxErrnoMap
        {
            get
            {
                return GlobalState.CurrentLanguageType switch
                {
                    LanguageType.SimplifiedChinese => LinuxErrnoMapSimplifiedChinese,
                    LanguageType.TraditionalChinese => LinuxErrnoMapTraditionalChinese,
                    _ => LinuxErrnoMapEnglish
                };
            }
        }
    }
}