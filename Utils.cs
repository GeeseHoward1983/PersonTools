using System.IO;
using System.Windows;

namespace PersonalTools
{
    internal static class Utils
    {
        /// <summary>
        /// 将Hex字符串转换为字节数组
        /// </summary>
        /// <param name="hex">十六进制字符串</param>
        /// <returns>字节数组</returns>
        public static byte[] HexStringToByteArray(string hex)
        {
            ArgumentNullException.ThrowIfNull(hex, nameof(hex));
            // 移除可能的空格和0x前缀
            hex = hex.Replace(" ", "", StringComparison.CurrentCulture).Replace("0x", "", StringComparison.CurrentCulture);

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
            return values.Any() ? string.Join(separator, values) : string.Empty;
        }

        public static string FormatAddress(ulong address)
        {
            return $"0x{address:X}";
        }

        public static string ExtractStringFromBytes(byte[] data, int startOffset)
        {
            if (data == null || startOffset < 0 || startOffset >= data.Length)
            {
                return string.Empty;
            }
            int endOffset = startOffset;
            while (endOffset < data.Length && data[endOffset] != 0)
            {
                endOffset++;
            }

            return endOffset > startOffset ? System.Text.Encoding.UTF8.GetString(data, startOffset, endOffset - startOffset) : string.Empty;
        }

        public static string ExtractStringFromBytes(byte[] data, int startOffset, int maxLength = -1)
        {
            if (data == null || startOffset < 0 || startOffset >= data.Length)
            {
                return string.Empty;
            }
            while (maxLength > 0 && data[startOffset + maxLength - 1] == 0)
            {
                maxLength--;
            }

            return maxLength > 0 ? System.Text.Encoding.UTF8.GetString(data, startOffset, maxLength) : string.Empty;
        }

        private static byte[] ReadBytesWithEndianness(BinaryReader reader, int count, bool isLittleEndian)
        {
            byte[] bytes = reader.ReadBytes(count);
            if (BitConverter.IsLittleEndian != isLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        public static long ReadInt64(BinaryReader reader, bool isLittleEndian)
        {
            return BitConverter.ToInt64(ReadBytesWithEndianness(reader, 8, isLittleEndian), 0);
        }

        public static int ReadInt32(BinaryReader reader, bool isLittleEndian)
        {
            return BitConverter.ToInt32(ReadBytesWithEndianness(reader, 4, isLittleEndian), 0);
        }

        public static ushort ReadUInt16(BinaryReader reader, bool isLittleEndian)
        {
            return BitConverter.ToUInt16(ReadBytesWithEndianness(reader, 2, isLittleEndian), 0);
        }

        public static uint ReadUInt32(BinaryReader reader, bool isLittleEndian)
        {
            return BitConverter.ToUInt32(ReadBytesWithEndianness(reader, 4, isLittleEndian), 0);
        }

        public static ulong ReadUInt64(BinaryReader reader, bool isLittleEndian)
        {
            return BitConverter.ToUInt64(ReadBytesWithEndianness(reader, 8, isLittleEndian), 0);
        }

        public static string GetTypeName(Type T, object type, string prefix)
        {
            try
            {
                if (Enum.IsDefined(T, type))
                {
                    return Enum.GetName(T, type) ?? $"{prefix}UNKNOWN({type})";
                }
            }
            catch (ArgumentException e)
            {
                MessageBox.Show(e.ToString());
            }
            return $"{prefix}UNKNOWN({type})";
        }

    }
}