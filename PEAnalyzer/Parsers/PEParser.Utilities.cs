using PersonalTools.PEAnalyzer.Models;
using System.IO;
using System.Text;

namespace PersonalTools.PEAnalyzer.Parsers
{
    internal static class Utilities
    {
        public static string ReadNullTerminatedString(BinaryReader reader)
        {
            StringBuilder sb = new();
            const int MaxLength = 1024; // 防止畸形文件中无终止符导致的超长读取
            try
            {
                byte b;

                while (sb.Length < MaxLength && reader.BaseStream.Position < reader.BaseStream.Length && (b = reader.ReadByte()) != 0)
                {
                    // 确保是有效的ASCII字符
                    if (b is >= 32 and <= 126)
                    {
                        sb.Append((char)b);
                    }
                    else if (b is 9 or 10 or 13)
                    {
                        // 允许制表符、换行符和回车符
                        sb.Append((char)b);
                    }
                    else
                    {
                        // 其他字符用'?'替换
                        sb.Append('?');
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // 遇到文件末尾时返回已读取的部分字符串
            }

            return sb.ToString();
        }

        // 判断是否为64位PE
        internal static bool Is64Bit(IMAGEOPTIONALHEADER optionalHeader)
        {
            return optionalHeader.Magic == PEConstants.Pe32PlusMagic;
        }

        internal static bool Is32Bit(IMAGEOPTIONALHEADER optionalHeader)
        {
            return optionalHeader.Magic == PEConstants.Pe32Magic;
        }

        // 向上对齐到 4 字节边界（版本资源结构广泛使用）
        internal static long AlignTo4(long position)
        {
            return (position + 3) & ~3;
        }

        /// <summary>
        /// 将RVA转换为文件偏移量。该原语与位数无关（仅使用 32 位节表字段），供导入/导出/CLR/资源解析共用。
        /// </summary>
        /// <param name="rva">相对虚拟地址</param>
        /// <param name="sections">节头列表</param>
        /// <param name="requiredLength">从该 RVA 起需要读取的字节数，用于校验整段都落在同一节的文件数据内，避免跨节读取</param>
        /// <returns>文件偏移量；无法解析时返回 -1</returns>
        internal static long RvaToOffset(uint rva, List<IMAGESECTIONHEADER> sections, uint requiredLength = 1)
        {
            // 添加对RVA的基本验证
            if (rva == 0 || sections == null || sections.Count == 0)
            {
                return -1;
            }

            foreach (IMAGESECTIONHEADER section in sections)
            {
                // 确保节头有效并具有文件数据
                if (section.SizeOfRawData == 0 || section.PointerToRawData == 0)
                {
                    continue;
                }

                // VirtualSize 为 0 时（部分旧链接器合法生成）按 SizeOfRawData 作为节在内存中的大小，
                // 否则该节内的 RVA 会被误判为无法解析，静默丢失导入/导出/CLR 数据。
                long sectionVirtualSize = section.VirtualSize != 0 ? section.VirtualSize : section.SizeOfRawData;

                // 检查RVA是否在当前节的范围内
                if (rva >= section.VirtualAddress && rva < (long)section.VirtualAddress + sectionVirtualSize)
                {
                    // 计算相对于节起始地址的偏移量
                    uint relativeOffset = rva - section.VirtualAddress;

                    // 起始 + 所需长度必须都落在节的文件数据(SizeOfRawData)内，
                    // 既排除未初始化数据节(.bss)，也避免跨节读取到下一节的数据
                    if ((long)relativeOffset + requiredLength > section.SizeOfRawData)
                    {
                        return -1;
                    }

                    // 使用 long 运算避免溢出
                    long offset = (long)section.PointerToRawData + relativeOffset;
                    if (offset >= 0)
                    {
                        return offset;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// 读取UNICODE字符串（带最大字符数限制）。
        /// </summary>
        /// <param name="reader">二进制读取器</param>
        /// <param name="maxLength">最大字符数</param>
        /// <returns>读取的字符串</returns>
        internal static string ReadUnicodeStringWithMaxLength(BinaryReader reader, int maxLength)
        {
            try
            {
                StringBuilder sb = new();
                int count = 0;

                // 限制最大读取次数以防止死循环
                int maxReadAttempts = Math.Min(maxLength, 1000);

                while (count < maxReadAttempts)
                {
                    // 检查是否还有数据可读
                    if (reader.BaseStream.Position + 2 > reader.BaseStream.Length)
                    {
                        break;
                    }

                    ushort ch = reader.ReadUInt16();
                    if (ch == 0) // NULL终止符
                    {
                        break;
                    }

                    sb.Append((char)ch);
                    count++;
                }

                return sb.ToString();
            }
            catch (IOException)
            {
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return string.Empty;
            }
            catch (ArgumentOutOfRangeException)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 读取UNICODE字符串（带最大字节数限制）。
        /// </summary>
        internal static string ReadUnicodeStringWithMaxBytes(BinaryReader reader, int maxBytes)
        {
            if (maxBytes <= 0)
            {
                return string.Empty;
            }

            return ReadUnicodeStringWithMaxLength(reader, maxBytes / 2);
        }
    }
}