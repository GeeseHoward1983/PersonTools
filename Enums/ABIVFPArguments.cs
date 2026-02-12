namespace PersonalTools.Enums
{
    /// <summary>
    /// ABI VFP参数传递方式枚举，用于Tag_ABI_VFP_args标签
    /// </summary>
    internal enum ABIVFPArguments : byte
    {
        /// <summary>
        /// 使用标准AAPCS参数传递
        /// </summary>
        BaseAAPCS = 0,

        /// <summary>
        /// 使用VFP寄存器传递参数
        /// </summary>
        VFPRegisters = 1,

        /// <summary>
        /// 使用合格的VFP寄存器传递参数
        /// </summary>
        ToolChainVFP = 2,

        /// <summary>
        /// 使用合格的通用寄存器传递参数
        /// </summary>
        ToolChainGeneric = 3
    }
}