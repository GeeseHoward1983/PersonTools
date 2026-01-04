using System.IO;
using System.Text;

namespace PersonalTools
{
    /// <summary>
    /// PE文件解析器CLR辅助函数模块
    /// 专门提供.NET程序集解析所需的辅助函数
    /// </summary>
    public static partial class PEParserCLR
    {
        /// <summary>
        /// 计算String堆的偏移
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="tablesOffset">表偏移</param>
        /// <param name="heapSizes">堆大小标志</param>
        /// <param name="maskValid">有效表掩码</param>
        /// <param name="rowCounts">行数数组</param>
        /// <returns>String堆的偏移</returns>
        private static long CalculateStringHeapOffset(long tablesOffset, byte heapSizes, ulong maskValid, uint[] rowCounts)
        {
            try
            {
                // 计算所有表的大小
                long position = tablesOffset + 24; // 跳过表头(24字节)

                // 跳过行数数组
                for (int i = 0; i < 64; i++)
                {
                    if ((maskValid & ((ulong)1 << i)) != 0)
                    {
                        position += 4;
                    }
                }

                // 跳过所有表的数据
                for (int i = 0; i < 64; i++)
                {
                    if ((maskValid & ((ulong)1 << i)) != 0)
                    {
                        uint rowCount = rowCounts[i];

                        // 根据表类型计算表大小
                        int rowSize = GetTableRowSize(i, heapSizes, maskValid, rowCounts);
                        position += (long)rowCount * rowSize;
                    }
                }

                return position;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// 获取表行大小
        /// </summary>
        /// <param name="tableIndex">表索引</param>
        /// <param name="heapSizes">堆大小标志</param>
        /// <param name="maskValid">有效表掩码</param>
        /// <param name="rowCounts">行数数组</param>
        /// <returns>行大小</returns>
        private static int GetTableRowSize(int tableIndex, byte heapSizes, ulong maskValid, uint[] rowCounts)
        {
            // 简化的行大小计算，实际实现需要根据ECMA-335规范
            return tableIndex switch
            {
                // Module
                0 => 2 + (IsSmallIndex(heapSizes, 1) ? 2 : 4) +
                                           (IsSmallIndex(heapSizes, 2) ? 2 : 4) +
                                           (IsSmallIndex(heapSizes, 0) ? 2 : 4) +
                                           (IsSmallIndex(heapSizes, 0) ? 2 : 4),
                // TypeRef
                1 => (IsSmallIndex(heapSizes, 1) ? 2 : 4) +
                                           (IsSmallIndex(heapSizes, 2) ? 2 : 4) +
                                           (IsSmallIndex(heapSizes, 2) ? 2 : 4),
                // TypeDef
                2 => 4 + (IsSmallIndex(heapSizes, 2) ? 2 : 4) +
                                           (IsSmallIndex(heapSizes, 2) ? 2 : 4) +
                                           GetCodedIndexSize(1, maskValid, rowCounts) + // Extends coded index
                                           GetCodedIndexSize(2, maskValid, rowCounts) + // FieldList coded index
                                           GetCodedIndexSize(2, maskValid, rowCounts),// MethodList coded index
                                                                                      // 其他表...
                _ => 16,// 默认大小
            };
        }

        /// <summary>
        /// 获取编码索引大小
        /// </summary>
        /// <param name="tagBits">标签位数</param>
        /// <param name="maskValid">有效表掩码</param>
        /// <param name="rowCounts">行数数组</param>
        /// <returns>编码索引大小</returns>
        private static int GetCodedIndexSize(int tagBits, ulong maskValid, uint[] rowCounts)
        {
            // 计算编码索引的最大值
            uint maxRowCount = 0;
            for (int i = 0; i < 64; i++)
            {
                if ((maskValid & ((ulong)1 << i)) != 0)
                {
                    maxRowCount = Math.Max(maxRowCount, rowCounts[i]);
                }
            }

            // 如果最大行数小于2^(16-tagBits)，则使用2字节；否则使用4字节
            return (maxRowCount < (1 << (16 - tagBits))) ? 2 : 4;
        }

        /// <summary>
        /// 检查是否使用小索引
        /// </summary>
        /// <param name="heapSizes">堆大小标志</param>
        /// <param name="heapIndex">堆索引</param>
        /// <returns>是否使用小索引</returns>
        private static bool IsSmallIndex(byte heapSizes, int heapIndex)
        {
            return (heapSizes & (1 << heapIndex)) == 0;
        }

        /// <summary>
        /// 从堆中读取字符串
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="heapOffset">堆偏移</param>
        /// <param name="index">索引</param>
        /// <returns>字符串</returns>
        private static string ReadStringFromHeap(FileStream fs, BinaryReader reader, long heapOffset, uint index)
        {
            try
            {
                if (heapOffset == -1 || index == 0)
                    return string.Empty;

                long originalPosition = fs.Position;
                fs.Position = heapOffset + index;

                // 读取以null结尾的字符串
                var sb = new StringBuilder();
                byte b;
                while ((b = reader.ReadByte()) != 0)
                {
                    sb.Append((char)b);
                    if (fs.Position >= fs.Length)
                        break;
                }

                fs.Position = originalPosition;
                return sb.ToString();
            }
            catch
            {
                return $"Unknown_Type_{index}";
            }
        }
    }
}