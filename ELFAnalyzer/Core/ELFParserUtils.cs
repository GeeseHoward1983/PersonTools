using System.Buffers.Binary;
using System.Globalization;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFParserUtils
    {
        public static string FormatAddress(ulong address)
        {
            return $"0x{address:X}";
        }

        /// <summary>
        /// 校验 [offset, offset+size) 是否完整落在长度为 <paramref name="length"/> 的缓冲/文件内。
        /// 用减法式判断（先判 offset 越界，再判 size 超出剩余），避免 offset+size 两个 ulong 相加回绕绕过校验。
        /// 各解析器(动态段/符号表/节头/程序头/GOT/版本)统一调用，消除重复且形态不一的边界检查。
        /// </summary>
        public static bool IsRangeWithin(ulong offset, ulong size, ulong length)
        {
            return offset <= length && size <= length - offset;
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

        public static string ExtractStringFromBytes(byte[] data, int startOffset, int maxLength)
        {
            if (data == null || startOffset < 0 || startOffset >= data.Length)
            {
                return string.Empty;
            }

            // 夹紧到剩余可读字节，防止 namesz/descsz 等越界导致 IndexOutOfRangeException
            maxLength = Math.Min(maxLength, data.Length - startOffset);
            while (maxLength > 0 && data[startOffset + maxLength - 1] == 0)
            {
                maxLength--;
            }

            return maxLength > 0 ? System.Text.Encoding.UTF8.GetString(data, startOffset, maxLength) : string.Empty;
        }

        // ---- Stream-based readers (sequential parse) ----

        public static long ReadInt64(BinaryReader reader, bool isLittleEndian)
        {
            Span<byte> buffer = stackalloc byte[8];
            reader.BaseStream.ReadExactly(buffer);
            return isLittleEndian ? BinaryPrimitives.ReadInt64LittleEndian(buffer) : BinaryPrimitives.ReadInt64BigEndian(buffer);
        }

        public static int ReadInt32(BinaryReader reader, bool isLittleEndian)
        {
            Span<byte> buffer = stackalloc byte[4];
            reader.BaseStream.ReadExactly(buffer);
            return isLittleEndian ? BinaryPrimitives.ReadInt32LittleEndian(buffer) : BinaryPrimitives.ReadInt32BigEndian(buffer);
        }

        public static ushort ReadUInt16(BinaryReader reader, bool isLittleEndian)
        {
            Span<byte> buffer = stackalloc byte[2];
            reader.BaseStream.ReadExactly(buffer);
            return isLittleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(buffer) : BinaryPrimitives.ReadUInt16BigEndian(buffer);
        }

        public static uint ReadUInt32(BinaryReader reader, bool isLittleEndian)
        {
            Span<byte> buffer = stackalloc byte[4];
            reader.BaseStream.ReadExactly(buffer);
            return isLittleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(buffer) : BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }

        public static ulong ReadUInt64(BinaryReader reader, bool isLittleEndian)
        {
            Span<byte> buffer = stackalloc byte[8];
            reader.BaseStream.ReadExactly(buffer);
            return isLittleEndian ? BinaryPrimitives.ReadUInt64LittleEndian(buffer) : BinaryPrimitives.ReadUInt64BigEndian(buffer);
        }

        // ---- Buffer-based readers (already-loaded data, no mutation of the source) ----
        // 集中边界校验：offset 越界(负/超出剩余字节)时返回 0 而非让 AsSpan 抛异常，
        // 使任何新增调用点对畸形截断数据都安全降级，而非崩溃中止整份分析。

        private static bool InBounds(byte[] data, int offset, int size)
        {
            return data != null && offset >= 0 && offset <= data.Length - size;
        }

        public static ushort ReadUInt16(byte[] data, int offset, bool isLittleEndian)
        {
            if (!InBounds(data, offset, 2))
            {
                return 0;
            }
            ReadOnlySpan<byte> span = data.AsSpan(offset, 2);
            return isLittleEndian ? BinaryPrimitives.ReadUInt16LittleEndian(span) : BinaryPrimitives.ReadUInt16BigEndian(span);
        }

        public static uint ReadUInt32(byte[] data, int offset, bool isLittleEndian)
        {
            if (!InBounds(data, offset, 4))
            {
                return 0;
            }
            ReadOnlySpan<byte> span = data.AsSpan(offset, 4);
            return isLittleEndian ? BinaryPrimitives.ReadUInt32LittleEndian(span) : BinaryPrimitives.ReadUInt32BigEndian(span);
        }

        public static ulong ReadUInt64(byte[] data, int offset, bool isLittleEndian)
        {
            if (!InBounds(data, offset, 8))
            {
                return 0;
            }
            ReadOnlySpan<byte> span = data.AsSpan(offset, 8);
            return isLittleEndian ? BinaryPrimitives.ReadUInt64LittleEndian(span) : BinaryPrimitives.ReadUInt64BigEndian(span);
        }

        public static int ReadInt32(byte[] data, int offset, bool isLittleEndian)
        {
            if (!InBounds(data, offset, 4))
            {
                return 0;
            }
            ReadOnlySpan<byte> span = data.AsSpan(offset, 4);
            return isLittleEndian ? BinaryPrimitives.ReadInt32LittleEndian(span) : BinaryPrimitives.ReadInt32BigEndian(span);
        }

        public static long ReadInt64(byte[] data, int offset, bool isLittleEndian)
        {
            if (!InBounds(data, offset, 8))
            {
                return 0;
            }
            ReadOnlySpan<byte> span = data.AsSpan(offset, 8);
            return isLittleEndian ? BinaryPrimitives.ReadInt64LittleEndian(span) : BinaryPrimitives.ReadInt64BigEndian(span);
        }

        // 按虚拟地址(sh_addr)查找节（多处版本/动态解析共用）
        public static Models.ELFSectionHeader? FindSectionByAddress(ELFParser parser, ulong address)
        {
            if (parser.SectionHeaders == null)
            {
                return null;
            }

            foreach (Models.ELFSectionHeader section in parser.SectionHeaders)
            {
                if (section.sh_addr == address)
                {
                    return section;
                }
            }
            return null;
        }

        // 解析某节通过 sh_link 关联的字符串表数据（verdef/verneed 的解析与格式化共用）。
        // SectionHeaders 为空或 sh_link 索引越界返回 false 且 strTabData 置空；isLittleEndian 始终给出。
        public static bool TryGetLinkedStringTable(ELFParser parser, Models.ELFSectionHeader section, out byte[] strTabData, out bool isLittleEndian)
        {
            strTabData = [];
            isLittleEndian = parser.Header.IsLittleEndian();
            if (parser.SectionHeaders == null)
            {
                return false;
            }

            int strTabIdx = (int)section.sh_link;
            if (strTabIdx >= parser.SectionHeaders.Count)
            {
                return false;
            }

            strTabData = parser.GetSectionData(strTabIdx);
            return true;
        }

        public static string GetTypeName(Type enumType, object type, string prefix)
        {
            try
            {
                // 将原始数值转换为枚举的底层类型，避免 Enum.IsDefined 因类型不匹配抛异常（同时省去每次调用的异常开销）
                object value = Convert.ChangeType(type, Enum.GetUnderlyingType(enumType), CultureInfo.InvariantCulture);
                if (Enum.IsDefined(enumType, value))
                {
                    return Enum.GetName(enumType, value) ?? $"{prefix}UNKNOWN({type})";
                }
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidCastException or OverflowException)
            {
                // 底层类型不匹配 / 无法转换 / 超出枚举底层类型范围，统一按未知处理
            }
            return $"{prefix}UNKNOWN({type})";
        }
    }
}
