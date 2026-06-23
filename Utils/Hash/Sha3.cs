using System.Security.Cryptography;
using System.Text;

namespace PersonalTools.Utils.Hash
{
    /// <summary>
    /// SHA3 包装：委托 System.Security.Cryptography 的 SHA3_256/384/512 静态实现。
    /// 无实例状态、无需释放资源，故为纯静态工具类。
    /// </summary>
    internal static class Sha3
    {
        public static string ComputeHash(string input, int outputLengthBits = 256)
        {
            return ConvertUtils.ToHexString(ComputeHash(Encoding.UTF8.GetBytes(input), outputLengthBits));
        }

        public static byte[] ComputeHash(byte[] input, int outputLengthBits = 256)
        {
            return IsSupported switch
            {
                true => outputLengthBits switch
                {
                    256 => SHA3_256.HashData(input),
                    384 => SHA3_384.HashData(input),
                    512 => SHA3_512.HashData(input),
                    _ => throw new ArgumentException($"不支持的输出长度: {outputLengthBits}"),
                },
                false => throw new PlatformNotSupportedException("当前平台不支持SHA3算法"),
            };
        }

        // 检查平台是否支持SHA3算法
        public static bool IsSupported =>
            SHA3_256.IsSupported &&
            SHA3_384.IsSupported &&
            SHA3_512.IsSupported;
    }
}