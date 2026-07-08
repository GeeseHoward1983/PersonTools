using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// ABI优化目标枚举，用于Tag_ABI_optimization_goals(30)标签
    /// </summary>
    internal enum ABIOptimizationGoals : byte
    {
        /// <summary>无特定优化目标</summary>
        None = 0,

        /// <summary>优先速度（兼顾大小与调试性）</summary>
        Prefer_Speed = 1,

        /// <summary>激进速度优化</summary>
        Aggressive_Speed = 2,

        /// <summary>优先大小（兼顾速度与调试性）</summary>
        Prefer_Size = 3,

        /// <summary>激进大小优化</summary>
        Aggressive_Size = 4,

        /// <summary>优先调试体验（兼顾速度与大小）</summary>
        Prefer_Debug = 5,

        /// <summary>激进调试优化</summary>
        Aggressive_Debug = 6
    }
}
