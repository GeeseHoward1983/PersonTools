using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// CPU非对齐访问枚举
    /// </summary>
    internal enum CPUUnalignedAccess : byte
    {
        None = 0,
        v6 = 1,
        v7 = 2
    }
}