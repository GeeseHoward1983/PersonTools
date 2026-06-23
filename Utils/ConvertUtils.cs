using System.Text;

namespace PersonalTools.Utils
{
    internal static class ConvertUtils
    {
        /// <summary>
        /// 将Hex字符串转换为字节数组
        /// </summary>
        /// <param name="hex">十六进制字符串</param>
        /// <returns>字节数组</returns>
        public static byte[] HexStringToByteArray(string hex)
        {
            ArgumentNullException.ThrowIfNull(hex, nameof(hex));
            // 移除空格分隔；仅剥离整串开头的一个 0x/0X 前缀——不全局替换，避免删除数据内部的 "0x" 子串或漏删大写 "0X"
            hex = hex.Replace(" ", "", StringComparison.Ordinal);
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hex = hex[2..];
            }

            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("Hex字符串长度必须是偶数");
            }

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                // 含非十六进制字符时 Convert.ToByte 抛 FormatException（各调用方均已捕获并提示）
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static uint ReverseBits(uint value, int width)
        {
            // 限制在 [0,32]：width>32 会移位溢出丢高位、width<0 直接返回 0，均非"反转 width 位"的预期结果
            width = Math.Clamp(width, 0, 32);
            uint result = 0;
            for (int i = 0; i < width; i++)
            {
                result <<= 1;
                result |= value & 1;
                value >>= 1;
            }
            return result;
        }

        public static string ToHexString(byte[] bytes, int startIndex, int length)
        {
            return Convert.ToHexString(bytes, startIndex, length);
        }

        public static string ToHexString(byte[] bytes)
        {
            return Convert.ToHexString(bytes);
        }

        public static string EnumerableToString(string? separator, IEnumerable<string> values)
        {
            // string.Join 对空序列本就返回 string.Empty，无需额外 Any() 判空（避免二次枚举惰性序列）
            return string.Join(separator, values);
        }

        /// <summary>按输入模式取字节：isHex=true 按十六进制解析，否则按 UTF-8。</summary>
        public static byte[] InputBytes(string text, bool isHex)
        {
            return isHex ? HexStringToByteArray(text) : Encoding.UTF8.GetBytes(text);
        }

        /// <summary>按输出模式取字符串：isHex=true 转十六进制，否则按 UTF-8 解码。</summary>
        public static string OutputString(byte[] bytes, bool isHex)
        {
            return isHex ? ToHexString(bytes) : Encoding.UTF8.GetString(bytes);
        }
    }
}