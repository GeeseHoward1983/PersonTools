namespace PersonalTools.Enums
{
    /// <summary>
    /// ABI硬浮点使用要求枚举，用于Tag_ABI_HardFP_use(27)标签
    /// </summary>
    internal enum ABIFPHardUse : byte
    {
        /// <summary>
        /// 按Tag_FP_arch的隐含约定使用（As Tag_FP_arch）
        /// </summary>
        As_Tag_FP_arch = 0,

        /// <summary>
        /// 仅SP（单精度）浮点运算
        /// </summary>
        SP_only = 1,

        /// <summary>
        /// 保留（旧版"SP和DP"含义已由ABI标记为Reserved）
        /// </summary>
        Reserved = 2,

        /// <summary>
        /// 已弃用
        /// </summary>
        Deprecated = 3
    }
}
