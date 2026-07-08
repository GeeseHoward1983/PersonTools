using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// ABI浮点数模型枚举，用于Tag_ABI_FP_number_model(23)标签
    /// </summary>
    internal enum ABIFPNumberModel : byte
    {
        /// <summary>未使用浮点数</summary>
        Unused = 0,

        /// <summary>仅IEEE 754格式的有限值（Finite）</summary>
        Finite = 1,

        /// <summary>RTABI模型：有限值 + 无穷 + 一个安静NaN</summary>
        RTABI = 2,

        /// <summary>完整IEEE 754模型</summary>
        IEEE_754 = 3
    }
}
