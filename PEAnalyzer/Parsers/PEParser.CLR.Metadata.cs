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
                long metaDataOffset = Utilities.RvaToOffset(metaDataRVA, peInfo.SectionHeaders);
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

                // 跳过版本字符串（其长度 length 已按 4 字节对齐，直接定位到 Flags/Streams）
                long versionStart = fs.Position;
                if (length > fs.Length - versionStart)
                {
                    length = (uint)(fs.Length - versionStart);
                }

                fs.Position = versionStart + length;

                // 读取 Flags(2) 与 Streams(2)
                if (fs.Position + 4 > fs.Length)
                {
                    return;
                }

                reader.ReadUInt16(); // Flags
                ushort streams = reader.ReadUInt16();

                // 先收集所有流的位置（#Strings 流可能位于 #~ 表流之后，必须先读全）
                (long tablesStreamOffset, uint tablesStreamSize, long stringHeapOffset) =
                    CollectStreamDirectory(fs, reader, streams, metaDataOffset);

                if (tablesStreamOffset != -1 && stringHeapOffset != -1)
                {
                    ParseMetadataTables(fs, reader, peInfo, tablesStreamOffset, tablesStreamSize, stringHeapOffset);
                }

                fs.Position = originalPosition;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"元数据解析IO错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"元数据解析权限错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 读取元数据流目录，定位表流（#~ / #-）与 #Strings 堆。
        /// </summary>
        /// <returns>(表流偏移, 表流大小, #Strings 堆偏移)；未找到的项为 -1 / 0。</returns>
        private static (long tablesStreamOffset, uint tablesStreamSize, long stringHeapOffset) CollectStreamDirectory(
            FileStream fs, BinaryReader reader, ushort streams, long metaDataOffset)
        {
            long tablesStreamOffset = -1;
            uint tablesStreamSize = 0;
            long stringHeapOffset = -1;

            for (int i = 0; i < streams; i++)
            {
                if (fs.Position + 8 > fs.Length)
                {
                    break;
                }

                uint offset = reader.ReadUInt32();
                uint size = reader.ReadUInt32();
                string streamName = ReadStreamName(fs, reader);

                if (streamName is "#~" or "#-")
                {
                    tablesStreamOffset = metaDataOffset + offset;
                    tablesStreamSize = size;
                }
                else if (streamName == "#Strings")
                {
                    stringHeapOffset = metaDataOffset + offset;
                }
            }

            return (tablesStreamOffset, tablesStreamSize, stringHeapOffset);
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
            long alignedPosition = Utilities.AlignTo4(fs.Position);
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
        private static void ParseMetadataTables(FileStream fs, BinaryReader reader, PEInfo peInfo, long tablesOffset, uint size, long stringHeapOffset)
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
                    ParseTypeDefTable(fs, reader, peInfo, tablesDataOffset, stringHeapOffset, heapSizes, maskValid, rowCounts);
                }

                fs.Position = originalPosition;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"元数据表解析IO错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"元数据表解析权限错误: {ex.Message}");
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
        private static void ParseTypeDefTable(FileStream fs, BinaryReader reader, PEInfo peInfo, long tablesDataOffset, long stringHeapOffset, byte heapSizes, ulong maskValid, uint[] rowCounts)
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
                    if (rowStart < 0 || rowStart + typeDefRowSize > fs.Length)
                    {
                        break;
                    }

                    fs.Position = rowStart;
                    TryCollectTypeDefRow(fs, reader, peInfo, stringHeapOffset, stringIndexSize, i);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"TypeDef表解析IO错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"TypeDef表解析权限错误: {ex.Message}");
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
        private static void TryCollectTypeDefRow(FileStream fs, BinaryReader reader, PEInfo peInfo, long stringHeapOffset, int stringIndexSize, uint i)
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

            string typeName = ReadStringFromHeap(fs, reader, stringHeapOffset, nameIndex);
            string namespaceName = ReadStringFromHeap(fs, reader, stringHeapOffset, namespaceIndex);
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
