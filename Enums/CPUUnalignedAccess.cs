using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// CPU非对齐访问枚举
    /// </summary>
    internal enum CPUUnalignedAccess : byte
    {
        None = 0,
        v6 = 1
        // ARM ABI Tag_CPU_unaligned_access 仅定义 0(None)/1(v6)，无 v7
    }
}