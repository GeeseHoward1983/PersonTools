namespace PersonalTools.ConstString
{
    public static partial class MacErrno
    {
        // Mac errno 错误码
        public static readonly Dictionary<long, string> MacErrnoMap = new(LinuxErrno.LinuxErrnoMap);
    }
}