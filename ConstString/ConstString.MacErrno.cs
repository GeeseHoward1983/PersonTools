using System.Collections.Generic;

namespace MyTool
{
    public static partial class ConstString
    {
        // Mac errno 错误码
        public static readonly Dictionary<int, string> MacErrnoMap = new Dictionary<int, string>(LinuxErrnoMap);
    }
}