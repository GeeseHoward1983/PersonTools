using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// ABI对齐需求枚举
    /// </summary>
    internal enum ABIAlignNeeded : byte
    {
        None = 0,
        Eight_byte = 1,   // 8-byte
        Four_byte = 2,    // 4-byte
        Undefined = 3
    }
}