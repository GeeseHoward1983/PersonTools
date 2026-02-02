using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// ABI优化目标枚举
    /// </summary>
    public enum ABIOptimizationGoals : byte
    {
        None = 0,
        Aggressive_Debug = 1,
        Balanced = 2,
        Aggressive_Speed = 3,
        Aggressive_Speed_Alternative = 6  // 根据汇编代码.eabi_attribute 30, 6，值6也表示Aggressive Speed
    }
}