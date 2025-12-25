using System;

namespace MyTool
{
    public static class Utils
    {
        /// <summary>
        /// 将Hex字符串转换为字节数组
        /// </summary>
        /// <param name="hex">十六进制字符串</param>
        /// <returns>字节数组</returns>
        public static byte[] HexStringToByteArray(string hex)
        {
            // 移除可能的空格和0x前缀
            hex = hex.Replace(" ", "").Replace("0x", "");

            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("Hex字符串长度必须是偶数");
            }

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
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
            uint result = 0;
            for (int i = 0; i < width; i++)
            {
                result <<= 1;
                result |= value & 1;
                value >>= 1;
            }
            return result;
        }
    }
}