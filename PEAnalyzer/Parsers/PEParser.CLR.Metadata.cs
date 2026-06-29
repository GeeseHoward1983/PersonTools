using PersonalTools.PEAnalyzer.Models;
using System.IO;
using System.Text;

namespace PersonalTools.PEAnalyzer.Parsers
{
    /// <summary>
    /// PE文件解析器CLR元数据解析模块
    /// 专门负责解析.NET程序集的元数据信息
    /// </summary>
    internal static partial class PEParserCLR
    {
        /// <summary>
        /// 解析.NET元数据
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="metaDataRVA">元数据RVA</param>
        private static void ParseMetaData(FileStream fs, BinaryReader reader, PEInfo peInfo, uint metaDataRVA)
        {
            try
            {
                long metaDataOffset = PEParserUtils.RvaToOffset(metaDataRVA, peInfo.SectionHeaders);
                if (metaDataOffset == -1 || metaDataOffset >= fs.Length)
                {
                    return;
                }

                long originalPosition = fs.Position;
                fs.Position = metaDataOffset;

                // 检查是否有足够数据读取元数据头
                if (fs.Position + 16 > fs.Length)
                {
                    return;
                }

                // 读取元数据头
                uint signature = reader.ReadUInt32();
                reader.ReadUInt16(); // MajorVersion
                reader.ReadUInt16(); // MinorVersion
                reader.ReadUInt32(); // Reserved
                uint length = reader.ReadUInt32();

                // 检查签名是否正确 (BSJB = 0x42534A42)
                if (signature != 0x42534A42)
                {
                    return;
                }

                // 跳过版本字符串：length 应按 4 字节对齐，显式对齐后再定位，避免畸形非对齐 length 导致 Flags/Streams 错位。
                // 全程用 long 运算：4GB+ 文件 (fs.Length-versionStart) 截 uint 会丢高位；(length+3) 用 long 也不会回绕。
                long versionStart = fs.Position;
                long remaining = fs.Length - versionStart;
                long alignedLength = Math.Min(length, remaining);
                alignedLength = (alignedLength + 3L) & ~3L; // 向上 4 字节对齐
                if (alignedLength > remaining)
                {
                    alignedLength = remaining;
                }

                fs.Position = versionStart + alignedLength;

                // 读取 Flags(2) 与 Streams(2)
                if (fs.Position + 4 > fs.Length)
                {
                    return;
                }

                reader.ReadUInt16(); // Flags
                ushort streams = reader.ReadUInt16();

                // 先收集所有流的位置（#Strings 流可能位于 #~ 表流之后，必须先读全）
                (long tablesStreamOffset, uint tablesStreamSize, long stringHeapOffset, uint stringHeapSize) =
                    CollectStreamDirectory(fs, reader, streams, metaDataOffset);

                if (tablesStreamOffset != -1 && stringHeapOffset != -1)
                {
                    ParseMetadataTables(fs, reader, peInfo, tablesStreamOffset, tablesStreamSize, stringHeapOffset, stringHeapSize);
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                PersonalTools.Utils.AppLogger.Log($"元数据解析错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 读取元数据流目录，定位表流（#~ / #-）与 #Strings 堆。
        /// </summary>
        /// <returns>(表流偏移, 表流大小, #Strings 堆偏移, #Strings 堆大小)；未找到的项为 -1 / 0。</returns>
        private static (long tablesStreamOffset, uint tablesStreamSize, long stringHeapOffset, uint stringHeapSize) CollectStreamDirectory(
            FileStream fs, BinaryReader reader, ushort streams, long metaDataOffset)
        {
            long tablesStreamOffset = -1;
            uint tablesStreamSize = 0;
            long stringHeapOffset = -1;
            uint stringHeapSize = 0;

            // 流数量上限：真实 .NET 程序集流数 ≤ ~6（#~/#-、#Strings、#US、#GUID、#Blob、#Pdb），
            // 用宽松上限 16 夹紧不可信的 streams，防止畸形巨值导致过多迭代。
            int streamCount = Math.Min((int)streams, 16);
            for (int i = 0; i < streamCount; i++)
            {
                if (fs.Position + 8 > fs.Length)
                {
                    break;
                }

                uint offset = reader.ReadUInt32();
                uint size = reader.ReadUInt32();
                string streamName = ReadStreamName(fs, reader);

                // 同名流只取首次出现的项，跳过后续重复项而非静默覆盖
                if (streamName is "#~" or "#-")
                {
                    if (tablesStreamOffset == -1)
                    {
                        tablesStreamOffset = metaDataOffset + offset;
                        tablesStreamSize = size;
                    }
                }
                else if (streamName == "#Strings")
                {
                    if (stringHeapOffset == -1)
                    {
                        stringHeapOffset = metaDataOffset + offset;
                        stringHeapSize = size;
                    }
                }
            }

            return (tablesStreamOffset, tablesStreamSize, stringHeapOffset, stringHeapSize);
        }

        /// <summary>
        /// 读取一个元数据流目录项中的流名称（以 null 结尾，按 4 字节对齐）。
        /// </summary>
        private static string ReadStreamName(FileStream fs, BinaryReader reader)
        {
            StringBuilder nameBuilder = new();
            // 元数据流名称按 ECMA-335 最长 32 字节，限制以防畸形数据无终止符
            while (fs.Position < fs.Length && nameBuilder.Length < 32)
            {
                byte b = reader.ReadByte();
                if (b == 0)
                {
                    break;
                }

                nameBuilder.Append((char)b);
            }

            // 对齐到4字节边界
            long alignedPosition = PEParserUtils.AlignTo4(fs.Position);
            if (alignedPosition < fs.Length && alignedPosition > fs.Position)
            {
                fs.Position = alignedPosition;
            }

            return nameBuilder.ToString();
        }

        /// <summary>
        /// 解析元数据表流（#~ / #-）。
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="tablesOffset">表流偏移</param>
        /// <param name="size">表流大小</param>
        /// <param name="stringHeapOffset">#Strings 堆的文件偏移</param>
        private static void ParseMetadataTables(FileStream fs, BinaryReader reader, PEInfo peInfo, long tablesOffset, uint size, long stringHeapOffset, uint stringHeapSize)
        {
            try
            {
                if (tablesOffset >= fs.Length || tablesOffset + size > fs.Length || size < 24)
                {
                    return;
                }

                long originalPosition = fs.Position;
                fs.Position = tablesOffset;

                // 读取表头（24 字节）
                if (fs.Position + 24 > fs.Length)
                {
                    return;
                }

                reader.ReadUInt32();              // Reserved
                reader.ReadByte();                // MajorVersion
                reader.ReadByte();                // MinorVersion
                byte heapSizes = reader.ReadByte();
                reader.ReadByte();                // Reserved
                ulong maskValid = reader.ReadUInt64();
                reader.ReadUInt64();              // MaskSorted

                // 读取每个有效表的行数
                uint[] rowCounts = new uint[64];
                for (int i = 0; i < 64; i++)
                {
                    if (IsTablePresent(maskValid, i))
                    {
                        if (fs.Position + 4 > fs.Length)
                        {
                            return;
                        }

                        rowCounts[i] = reader.ReadUInt32();
                    }
                }

                // 行数数组之后即为表数据起始位置
                long tablesDataOffset = fs.Position;

                const int TypeDefTableIndex = 2;
                if (IsTablePresent(maskValid, TypeDefTableIndex))
                {
                    // 表数据上界用表流声明大小(tablesOffset+size)，而非整个文件长度，
                    // 防止畸形 rowCount 让 TypeDef 行读到表流之外、把无关字节当作伪造导出类型
                    long tablesEnd = Math.Min(tablesOffset + size, fs.Length);
                    ParseTypeDefTable(fs, reader, peInfo, tablesDataOffset, stringHeapOffset, stringHeapSize, heapSizes, maskValid, rowCounts, tablesEnd);
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                PersonalTools.Utils.AppLogger.Log($"元数据表解析错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析 TypeDef 表，收集公开可见类型的全名。
        /// 依据 ECMA-335 计算各表的可变行宽，跳过位于 TypeDef 之前的 Module/TypeRef 表，
        /// 并以正确的索引宽度读取每一行。
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="tablesDataOffset">表数据起始偏移</param>
        /// <param name="stringHeapOffset">#Strings 堆偏移</param>
        /// <param name="heapSizes">堆大小标志</param>
        /// <param name="maskValid">有效表掩码</param>
        /// <param name="rowCounts">行数数组</param>
        private static void ParseTypeDefTable(FileStream fs, BinaryReader reader, PEInfo peInfo, long tablesDataOffset, long stringHeapOffset, uint stringHeapSize, byte heapSizes, ulong maskValid, uint[] rowCounts, long tablesEnd)
        {
            try
            {
                int stringIndexSize = HeapIndexSize(heapSizes, 0);
                int guidIndexSize = HeapIndexSize(heapSizes, 1);
                int resolutionScopeSize = CodedIndexSize(2, rowCounts, ResolutionScopeTables);
                int typeDefOrRefSize = CodedIndexSize(2, rowCounts, TypeDefOrRefTables);
                int fieldIndexSize = SimpleIndexSize(rowCounts, FieldTableIndex);
                int methodIndexSize = SimpleIndexSize(rowCounts, MethodDefTableIndex);

                // 各相关表的行大小（ECMA-335 II.22）
                long moduleRowSize = 2L + stringIndexSize + (3L * guidIndexSize);
                long typeRefRowSize = (long)resolutionScopeSize + (2L * stringIndexSize);
                long typeDefRowSize = 4L + (2L * stringIndexSize) + typeDefOrRefSize + fieldIndexSize + methodIndexSize;

                // 跳过位于 TypeDef(2) 之前的 Module(0)、TypeRef(1) 表，定位到 TypeDef 表数据起始
                long typeDefStart = ComputeTypeDefStart(tablesDataOffset, maskValid, rowCounts, moduleRowSize, typeRefRowSize);

                uint typeDefCount = rowCounts[2];
                for (uint i = 0; i < typeDefCount; i++)
                {
                    long rowStart = typeDefStart + (long)i * typeDefRowSize;
                    // 上界用表流声明结束位置 tablesEnd（≤ fs.Length），而非整个文件，避免越表读伪造类型
                    if (rowStart < 0 || rowStart + typeDefRowSize > tablesEnd)
                    {
                        break;
                    }

                    fs.Position = rowStart;
                    TryCollectTypeDefRow(fs, reader, peInfo, stringHeapOffset, stringHeapSize, stringIndexSize, i);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                PersonalTools.Utils.AppLogger.Log($"TypeDef表解析错误: {ex.Message}");
            }
        }

        // 定位 TypeDef 表起始：跳过其前的 Module(0)、TypeRef(1) 表
        private static long ComputeTypeDefStart(long tablesDataOffset, ulong maskValid, uint[] rowCounts, long moduleRowSize, long typeRefRowSize)
        {
            long typeDefStart = tablesDataOffset;
            if (IsTablePresent(maskValid, 0))
            {
                typeDefStart += rowCounts[0] * moduleRowSize;
            }
            if (IsTablePresent(maskValid, 1))
            {
                typeDefStart += rowCounts[1] * typeRefRowSize;
            }
            return typeDefStart;
        }

        // 读取一行 TypeDef，仅收集公开可见类型（Public=1 / NestedPublic=2）到导出列表
        private static void TryCollectTypeDefRow(FileStream fs, BinaryReader reader, PEInfo peInfo, long stringHeapOffset, uint stringHeapSize, int stringIndexSize, uint i)
        {
            uint flags = reader.ReadUInt32();
            uint nameIndex = ReadHeapIndex(reader, stringIndexSize);
            uint namespaceIndex = ReadHeapIndex(reader, stringIndexSize);
            // Extends/FieldList/MethodList 无需读取：下一行由 rowStart 推进

            uint visibility = flags & 0x7; // VisibilityMask
            if (visibility is not (1u or 2u))
            {
                return;
            }

            string typeName = ReadStringFromHeap(fs, reader, stringHeapOffset, stringHeapSize, nameIndex);
            string namespaceName = ReadStringFromHeap(fs, reader, stringHeapOffset, stringHeapSize, namespaceIndex);
            string fullName = string.IsNullOrEmpty(namespaceName) ? typeName : $"{namespaceName}.{typeName}";

            if (!string.IsNullOrEmpty(fullName))
            {
                peInfo.ExportFunctions.Add(new ExportFunctionInfo
                {
                    Name = fullName,
                    Ordinal = (int)i,
                    RVA = 0 // 对于.NET程序集，RVA不适用
                });
            }
        }
    }
}
