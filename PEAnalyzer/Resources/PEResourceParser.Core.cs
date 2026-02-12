using PersonalTools.PEAnalyzer.Models;
using System.IO;
using System.Text;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器核心功能
    /// 包含基础的资源解析方法和通用工具方法
    /// </summary>
    internal static class PEResourceParserCore
    {
        /// <summary>
        /// 将RVA转换为文件偏移量
        /// </summary>
        /// <param name="rva">相对虚拟地址</param>
        /// <param name="sections">节头列表</param>
        /// <returns>文件偏移量</returns>
        internal static long RvaToOffset(uint rva, List<IMAGESECTIONHEADER> sections)
        {
            // 添加对RVA的基本验证
            if (rva == 0)
            {
                return -1;
            }

            foreach (IMAGESECTIONHEADER section in sections)
            {
                // 确保VirtualSize不为0，避免除零错误
                if (section.VirtualSize == 0)
                {
                    continue;
                }

                // 检查RVA是否在当前节的范围内
                // 使用VirtualSize作为节在内存中的大小
                // 使用SizeOfRawData作为节在文件中的大小
                if (rva >= section.VirtualAddress && rva < section.VirtualAddress + section.VirtualSize)
                {
                    // 计算相对于节起始地址的偏移量
                    uint relativeOffset = rva - section.VirtualAddress;

                    // 如果偏移量超出了文件中节的大小，则返回-1
                    // 这种情况常见于未初始化数据节(.bss等)
                    if (relativeOffset >= section.SizeOfRawData)
                    {
                        return -1;
                    }

                    // 确保计算结果不会溢出
                    long offset = section.PointerToRawData + relativeOffset;
                    // 确保offset不为负数且在合理范围内
                    if (offset >= 0)
                    {
                        return offset;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// 读取UNICODE字符串（带最大长度限制）
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
                // 记录异常但不中断
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                // 记录异常但不中断
                return string.Empty;
            }
            catch (ArgumentOutOfRangeException)
            {
                // 记录异常但不中断
                return string.Empty;
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
            }
        }
    }
}