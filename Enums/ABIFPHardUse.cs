using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// ABI硬浮点使用要求枚举，用于Tag_ABI_HardFP_use标签
    /// </summary>
    public enum ABIFPHardUse : byte
    {
        /// <summary>
        /// 不使用硬浮点
        /// </summary>
        Not_Allowed = 0,
        
        /// <summary>
        /// 仅SP（单精度）浮点运算
        /// </summary>
        SP_only = 1,
        
        /// <summary>
        /// SP和DP（双精度）浮点运算
        /// </summary>
        SP_and_DP = 2,
        
        /// <summary>
        /// 已弃用
        /// </summary>
        Deprecated = 3
    }
}