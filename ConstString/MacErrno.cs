namespace PersonalTools.ConstString
{
    internal static partial class MacErrno
    {
        // Mac errno 错误码
        internal static readonly Dictionary<long, string> MacErrnoMap = new(LinuxErrno.LinuxErrnoMap);
    }
}