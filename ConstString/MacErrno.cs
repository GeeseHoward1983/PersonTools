namespace PersonalTools.ConstString
{
    internal static partial class MacErrno
    {
        // Mac errno 错误码。
        // 注意：当前仅复用 Linux errno 数据；Darwin/BSD 在 ~35 以上的 errno 数值与 Linux 不同，
        // 这些条目的文案并不准确，需后续补入真实的 Darwin errno 表。改为属性以随语言切换重新取值。
        internal static Dictionary<long, string> MacErrnoMap => new(LinuxErrno.LinuxErrnoMap);
    }
}