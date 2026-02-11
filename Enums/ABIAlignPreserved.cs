namespace PersonalTools.Enums
{
    /// <summary>
    /// ABI对齐保留要求枚举，用于Tag_ABI_align_preserved标签
    /// </summary>
    public enum ABIAlignPreserved : byte
    {
        /// <summary>
        /// 无特殊对齐保留要求
        /// </summary>
        None = 0,

        /// <summary>
        /// 8字节对齐保留，但叶函数SP（栈指针）除外
        /// 对应readelf输出的"8-byte, except leaf SP"
        /// </summary>
        Eight_byte_except_leaf_SP = 1,

        /// <summary>
        /// 4字节对齐保留
        /// </summary>
        Four_byte = 2,

        /// <summary>
        /// 未定义或未知的对齐保留要求
        /// </summary>
        Undefined = 3
    }
}