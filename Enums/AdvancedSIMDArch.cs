using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// 高级SIMD架构枚举，用于Tag_Advanced_SIMD_arch(12)标签
    /// </summary>
    internal enum AdvancedSIMDArch : byte
    {
        /// <summary>不允许使用Advanced SIMD</summary>
        No = 0,

        /// <summary>Advanced SIMDv1 (NEONv1)</summary>
        NEONv1 = 1,

        /// <summary>Advanced SIMDv2 (NEONv1 含 Fused-MAC)，对应readelf "NEONv1 with Fused-MAC"</summary>
        NEONv1_FusedMAC = 2,

        /// <summary>NEON for ARMv8</summary>
        NEON_ARMv8 = 3,

        /// <summary>NEON for ARMv8.1</summary>
        NEON_ARMv8_1 = 4
    }
}
