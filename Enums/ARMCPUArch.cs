using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// ARM CPU架构枚举
    /// </summary>
    internal enum ARMCPUArch : byte
    {
        Pre_v4 = 0,
        v4 = 1,
        v4T = 2,
        v5T = 3,
        v5TE = 4,
        v5TEJ = 5,
        v6 = 6,
        v6KZ = 7,
        v6T2 = 8,
        v6K = 9,
        v7 = 10,
        v6_M = 11,        // v6-M
        v6S_M = 12,       // v6S-M
        v7E_M = 13,       // v7E-M
        v8_A = 14,        // v8-A
        v8_R = 15,        // v8-R
        v8_M_Baseline = 16, // v8-M.Baseline
        v8_M_Mainline = 17, // v8-M.Mainline
        v8_1_A = 18,        // v8.1-A
        v8_2_A = 19,        // v8.2-A
        v8_3_A = 20,        // v8.3-A
        v8_1_M_Mainline = 21, // v8.1-M.Mainline
        v9 = 22             // v9（与 s_aeabiCpuArch/binutils arm_attr_tag_CPU_arch 对齐）
    }
}