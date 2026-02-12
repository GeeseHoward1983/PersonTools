using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// ABI枚举大小枚举
    /// </summary>
    internal enum ABIEnumSize : byte
    {
        None = 0,
        Small = 1,            // small
        Int = 2,              // int
        Forced_to_int = 3     // forced to int
    }
}