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
            ArgumentNullException.ThrowIfNull(hex);
            // 移除所有空白(空格/Tab/CR/LF 等)；仅剥离整串开头的一个 0x/0X 前缀——不全局替换，避免删除数据内部的 "0x" 子串或漏删大写 "0X"
            // 仅在确含空白时才重建字符串，避免对无空白的大字符串(文件 hex)无谓整串复制；用户常从多行/带制表符的 hex dump 粘贴密文
            if (ContainsWhitespace(hex))
            {
                hex = RemoveWhitespace(hex);
            }
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                hex = hex[2..];
            }

            // 空串（或仅 "0x"）不视为合法的 0 字节数据：否则解密/解码路径会把"未输入"当作空密文继续，掩盖真实问题
            if (hex.Length == 0)
            {
                throw new ArgumentException("Hex字符串不能为空");
            }

            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("Hex字符串长度必须是偶数");
            }

            // Convert.FromHexString 内部高效解析，零额外子串分配（替代逐对 Substring + Convert.ToByte 的分配热点）；
            // 含非十六进制字符时抛 FormatException（各调用方均已捕获并提示）
            return Convert.FromHexString(hex);
        }

        // 是否含任意空白字符（仅扫描，不分配）——用于决定是否需要重建字符串
        private static bool ContainsWhitespace(string s)
        {
            foreach (char c in s)
            {
                if (char.IsWhiteSpace(c))
                {
                    return true;
                }
            }
            return false;
        }

        // 去除所有空白字符
        private static string RemoveWhitespace(string s)
        {
            StringBuilder sb = new(s.Length);
            foreach (char c in s)
            {
                if (!char.IsWhiteSpace(c))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 在指定位宽内将整数按位反转（CRC 的 refin/refout 子步骤）：把 <paramref name="value"/> 的低
        /// <paramref name="width"/> 位按位镜像。<paramref name="width"/> 被钳制到 [0,32]。
        /// </summary>
        /// <param name="value">待反转的值</param>
        /// <param name="width">参与反转的位宽（0~32）</param>
        /// <returns>低 width 位按位反序后的结果</returns>
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