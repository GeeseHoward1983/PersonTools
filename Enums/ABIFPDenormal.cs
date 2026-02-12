using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// ABI浮点非规格化数处理枚举
    /// </summary>
    internal enum ABIFPDenormal : byte
    {
        Unused = 0,
        Needed = 1,
        Sign_only = 2
    }
}