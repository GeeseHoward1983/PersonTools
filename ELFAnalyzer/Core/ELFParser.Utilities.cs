using System.IO;
using System.Windows;

namespace PersonalTools.ELFAnalyzer.Core
{
    public static class ELFParserUtils
    {
        public static string FormatAddress(ulong address)
        {
            return $"0x{address:X}";
        }

        public static string ExtractStringFromBytes(byte[] data, int startOffset)
        {
            int endOffset = startOffset;
            while (endOffset < data.Length && data[endOffset] != 0)
            {
                endOffset++;
            }

            if (endOffset > startOffset)
            {
                return System.Text.Encoding.UTF8.GetString(data, startOffset, endOffset - startOffset);
            }
            return string.Empty;
        }

        public static string ExtractStringFromBytes(byte[] data, int startOffset, int maxLength = -1)
        {
            while (maxLength > 0 && data[startOffset + maxLength - 1] == 0)
            {
                maxLength--;
            }

            return maxLength > 0 ? System.Text.Encoding.UTF8.GetString(data, startOffset, maxLength) : string.Empty;
        }


        public static long ReadInt64(BinaryReader reader, bool isLittleEndian)
        {
            var bytes = reader.ReadBytes(8);
            if (BitConverter.IsLittleEndian != isLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        public static int ReadInt32(BinaryReader reader, bool isLittleEndian)
        {
            var bytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian != isLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static ushort ReadUInt16(BinaryReader reader, bool isLittleEndian)
        {
            var bytes = reader.ReadBytes(2);
            if (BitConverter.IsLittleEndian != isLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public static uint ReadUInt32(BinaryReader reader, bool isLittleEndian)
        {
            var bytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian != isLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public static ulong ReadUInt64(BinaryReader reader, bool isLittleEndian)
        {
            var bytes = reader.ReadBytes(8);
            if (BitConverter.IsLittleEndian != isLittleEndian)
                Array.Reverse(bytes);
            return BitConverter.ToUInt64(bytes, 0);
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
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            return $"{prefix}UNKNOWN({type})";
        }

    }
}