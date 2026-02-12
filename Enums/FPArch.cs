using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// 浮点架构枚举
    /// </summary>
    internal enum FPArch : byte
    {
        No = 0,
        VFPv1 = 1,
        VFPv2 = 2,
        VFPv3 = 3,
        VFPv3_D16 = 4,
        VFPv4 = 5,
        VFPv4_D16 = 6,
        FP_ARM_v8 = 7,
        FPv5_FP_D16_for_ARMv8 = 8
    }
}