using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// ABI浮点数模型枚举
    /// </summary>
    public enum ABIFPNumberModel : byte
    {
        Unused = 0,
        IEEE_754 = 1,
        IEEE_754_alt = 3
    }
}