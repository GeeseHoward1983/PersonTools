using System.Security.Cryptography;
using System.Text;

namespace PersonalTools
{
    public class SHA3(int variant) : IDisposable
    {
        // SHA3算法变体枚举
        public enum SHA3Variant
        {
            SHA3_256 = 256,
            SHA3_384 = 384,
            SHA3_512 = 512
        }

        private readonly int _outputLengthBits = variant;

        public byte[] ComputeHash(byte[] input)
        {
            return ComputeHash(input, _outputLengthBits);
        }

        public static string ComputeHash(string input, int outputLengthBits = 256)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] output = ComputeHash(inputBytes, outputLengthBits);
            return BitConverter.ToString(output).Replace("-", "").ToLower();
        }

        public static byte[] ComputeHash(byte[] input, int outputLengthBits = 256)
        {
            // 检查平台是否支持SHA3算法
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException("当前平台不支持SHA3算法");
            }

            // 根据输出长度选择相应的SHA3算法
            return outputLengthBits switch
            {
                256 => SHA3_256.HashData(input),
                384 => SHA3_384.HashData(input),
                512 => SHA3_512.HashData(input),
                //case 224:
                //    // .NET 8中没有内置的SHA3-224，需要使用SHA3-256并截断结果
                //    byte[] sha3_256_result = SHA3_256.HashData(input);
                //    byte[] result = new byte[28]; // 224位 = 28字节
                //    Array.Copy(sha3_256_result, result, 28);
                //    return result;
                _ => throw new ArgumentException($"不支持的输出长度: {outputLengthBits}"),
            };
        }

        // 检查平台是否支持SHA3算法
        public static bool IsSupported =>
            SHA3_256.IsSupported &&
            SHA3_384.IsSupported &&
            SHA3_512.IsSupported;

        public void Dispose()
        {
            // 释放资源（如果需要）
            GC.SuppressFinalize(this);
        }
    }
}