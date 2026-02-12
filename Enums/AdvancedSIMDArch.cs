using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// 高级SIMD架构枚举
    /// </summary>
    internal enum AdvancedSIMDArch : byte
    {
        No = 0,
        NEONv1 = 1,
        NEONv1_FP16 = 2,
        NEONv1_FP16_HP = 3,
        NEONv2 = 4
    }
}