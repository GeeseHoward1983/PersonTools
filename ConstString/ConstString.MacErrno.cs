namespace PersonalTools
{
    public static partial class ConstString
    {
        // Mac errno 错误码
        public static readonly Dictionary<long, string> MacErrnoMap = new(LinuxErrnoMap);
    }
}