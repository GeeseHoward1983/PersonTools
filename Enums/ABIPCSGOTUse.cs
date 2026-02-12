using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// ABI PCS GOT使用情况枚举
    /// </summary>
    internal enum ABIPCSGOTUse : byte
    {
        None = 0,
        GOT_32 = 1,
        GOT_indirect = 2
    }
}