using System.IO;
using System.Text;

namespace PersonalTools.PEAnalyzer.Parsers
{
    /// <summary>
    /// PE文件解析器CLR辅助函数模块
    /// 依据 ECMA-335 提供 .NET 元数据表解析所需的索引宽度计算与堆字符串读取。
    /// </summary>
    internal static partial class PEParserCLR
    {
        // ECMA-335 II.24.2.6 编码索引族：用于计算可变索引宽度
        private static readonly int[] ResolutionScopeTables = [0, 26, 35, 1]; // Module, ModuleRef, AssemblyRef, TypeRef
        private static readonly int[] TypeDefOrRefTables = [2, 1, 27];        // TypeDef, TypeRef, TypeSpec

        // 简单索引涉及的表
        private const int FieldTableIndex = 4;
        private const int MethodDefTableIndex = 6;

        /// <summary>
        /// 堆索引宽度：heapSizes 对应位为 0 时为 2 字节，否则为 4 字节。
        /// bit0 = #Strings，bit1 = #GUID，bit2 = #Blob。
        /// </summary>
        private static int HeapIndexSize(byte heapSizes, int heapBit)
        {
            return (heapSizes & (1 << heapBit)) switch
            {
                0 => 2,
                _ => 4
            };
        }

        /// <summary>
        /// 简单表索引宽度：目标表行数 &lt; 2^16 时为 2 字节，否则为 4 字节。
        /// </summary>
        private static int SimpleIndexSize(uint[] rowCounts, int tableIndex)
        {
            return rowCounts[tableIndex] switch
            {
                < 0x10000 => 2,
                _ => 4
            };
        }

        /// <summary>
        /// 编码索引宽度：当索引族中各表的最大行数 &lt; 2^(16 - tagBits) 时为 2 字节，否则为 4 字节。
        /// </summary>
        private static int CodedIndexSize(int tagBits, uint[] rowCounts, int[] familyTables)
        {
            uint maxRowCount = 0;
            foreach (int table in familyTables)
            {
                maxRowCount = table switch
                {
                    >= 0 when table < rowCounts.Length => Math.Max(maxRowCount, rowCounts[table]),
                    _ => maxRowCount
                };
            }

            return maxRowCount < (1u << (16 - tagBits)) ? 2 : 4;

        }

        /// <summary>
        /// 按给定宽度读取一个索引（2 或 4 字节）。
        /// </summary>
        private static uint ReadHeapIndex(BinaryReader reader, int indexSize)
        {
            return indexSize switch
            {
                2 => reader.ReadUInt16(),
                _ => reader.ReadUInt32()
            };
        }

        /// <summary>
        /// 判断指定索引的元数据表是否存在于有效表掩码中。
        /// </summary>
        private static bool IsTablePresent(ulong maskValid, int tableIndex)
        {
            return (maskValid & ((ulong)1 << tableIndex)) != 0;
        }

        /// <summary>
        /// 从 #Strings 堆中读取以 null 结尾的字符串。
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="heapOffset">#Strings 堆的文件偏移</param>
        /// <param name="heapSize">#Strings 堆声明大小（0 属畸形声明——合法堆至少含 index 0 的空串——一律拒绝读取）</param>
        /// <param name="index">堆内索引</param>
        /// <returns>字符串</returns>
        private static string ReadStringFromHeap(FileStream fs, BinaryReader reader, long heapOffset, uint heapSize, uint index)
        {
            return heapOffset switch
            {
                < 0 => string.Empty,
                _ => index switch
                {
                    0 => string.Empty,
                    _ when index < heapSize => ReadStringFromHeap2(fs, reader, heapOffset, heapSize, index),
                    _ => string.Empty
                }
            };
        }

        private static string ReadStringFromHeap2(FileStream fs, BinaryReader reader, long heapOffset, uint heapSize, uint index)
        {
            (long targetPosition, long heapEnd) = (heapOffset + index, Math.Min(heapOffset + heapSize, fs.Length));
            return targetPosition switch
            {
                < 0 => string.Empty,
                _ when targetPosition < heapEnd => ReadStringFromHeapCore(fs, reader, targetPosition, heapEnd),
                _ => string.Empty
            };
        }

        private static string ReadStringFromHeapCore(FileStream fs, BinaryReader reader, long targetPosition, long heapEnd)
        {
            long originalPosition = fs.Position;
            fs.Position = targetPosition;

            // #Strings 堆为 UTF-8 编码，按 null 结尾读取并限制最大长度；同时不越过堆结束边界
            List<byte> bytes = [];
            byte b;
            while (fs.Position < heapEnd && bytes.Count < 1024 && (b = reader.ReadByte()) != 0)
            {
                bytes.Add(b);
            }

            fs.Position = originalPosition;
            return Encoding.UTF8.GetString([.. bytes]);
        }
    }
}
