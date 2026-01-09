using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    public static class ELFParserUtils
    {
        public static string FormatAddress(ulong address)
        {
            return $"0x{address:X}";
        }

        public static string FormatSize(ulong size)
        {
            return $"0x{size:X} ({size} bytes)";
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

        public static long ReadInt64LE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8);
            if (BitConverter.IsLittleEndian) return BitConverter.ToInt64(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        public static long ReadInt64BE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8);
            if (!BitConverter.IsLittleEndian) return BitConverter.ToInt64(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        public static int ReadInt32LE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian) return BitConverter.ToInt32(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static int ReadInt32BE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            if (!BitConverter.IsLittleEndian) return BitConverter.ToInt32(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static ushort ReadUInt16LE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2);
            if (BitConverter.IsLittleEndian) return BitConverter.ToUInt16(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public static ushort ReadUInt16BE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2);
            if (!BitConverter.IsLittleEndian) return BitConverter.ToUInt16(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public static uint ReadUInt32LE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian) return BitConverter.ToUInt32(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public static uint ReadUInt32BE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            if (!BitConverter.IsLittleEndian) return BitConverter.ToUInt32(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public static ulong ReadUInt64LE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8);
            if (BitConverter.IsLittleEndian) return BitConverter.ToUInt64(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        public static ulong ReadUInt64BE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8);
            if (!BitConverter.IsLittleEndian) return BitConverter.ToUInt64(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }
    }
}