using PersonalTools.PEAnalyzer.Models;
using System.IO;
using System.Reflection.Metadata;
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
                if (metaDataOffset switch
                {
                    -1 => false,
                    _ when metaDataOffset < fs.Length => true,
                    _ => false
                })
                {
                    ParseMetaData2(fs, reader, peInfo, metaDataOffset);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Utils.AppLogger.Log($"元数据解析错误: {ex.Message}");
            }
        }

        private static bool CheckSignature(BinaryReader reader)
        {
            // 读取元数据头
            uint signature = reader.ReadUInt32();

            // 检查签名是否正确（"BSJB" 字节序列 42 53 4A 42 按小端 ReadUInt32 读出为 0x424A5342）
            return signature == 0x424A5342;
        }

        private static void ParseMetaData2(FileStream fs, BinaryReader reader, PEInfo peInfo, long metaDataOffset)
        {
            long originalPosition = fs.Position;
            fs.Position = metaDataOffset;

            // 检查是否有足够数据读取元数据头
            if (fs.Position + 16 <= fs.Length && CheckSignature(reader))
            {
                ParseMetaData3(fs, reader, peInfo, metaDataOffset);
            }
            fs.Position = originalPosition;
        }

        private static void ParseMetaData3(FileStream fs, BinaryReader reader, PEInfo peInfo, long metaDataOffset)
        {
            (ulong _, uint length) = (reader.ReadUInt64(), reader.ReadUInt32());

            // 跳过版本字符串：length 应按 4 字节对齐，显式对齐后再定位，避免畸形非对齐 length 导致 Flags/Streams 错位。
            // 全程用 long 运算：4GB+ 文件 (fs.Length-versionStart) 截 uint 会丢高位；(length+3) 用 long 也不会回绕。
            (long versionStart, long remaining, long alignedLength) = (fs.Position, fs.Length - fs.Position, (Math.Min(length, fs.Length - fs.Position) + 3L) & ~3L);
            alignedLength = Math.Min(alignedLength, remaining);
            fs.Position = versionStart + alignedLength;

            // 读取 Flags(2) 与 Streams(2)
            if (fs.Position + 4 > fs.Length)
            {
                return;
            }

            (ushort _, ushort streams) = (reader.ReadUInt16(), reader.ReadUInt16());

            // 先收集所有流的位置（#Strings 流可能位于 #~ 表流之后，必须先读全）
            (long tablesStreamOffset, uint tablesStreamSize, long stringHeapOffset, uint stringHeapSize) =
                CollectStreamDirectory(fs, reader, streams, metaDataOffset);

            if (tablesStreamOffset != -1 && stringHeapOffset != -1)
            {
                ParseMetadataTables(fs, reader, peInfo, tablesStreamOffset, tablesStreamSize, stringHeapOffset, stringHeapSize);
            }
        }


        /// <summary>
        /// 读取元数据流目录，定位表流（#~ / #-）与 #Strings 堆。
        /// </summary>
        /// <returns>(表流偏移, 表流大小, #Strings 堆偏移, #Strings 堆大小)；未找到的项为 -1 / 0。</returns>
        private static (long tablesStreamOffset, uint tablesStreamSize, long stringHeapOffset, uint stringHeapSize) CollectStreamDirectory(
            FileStream fs, BinaryReader reader, ushort streams, long metaDataOffset)
        {
            (long tablesStreamOffset, uint tablesStreamSize, long stringHeapOffset, uint stringHeapSize, int streamCount) = ( -1, 0, -1, 0, Math.Min((int)streams, 16));
            for (int i = 0; i < streamCount && fs.Position + 8 <= fs.Length; i++)
            {
                (uint offset, uint _, string streamName) = (reader.ReadUInt32(), reader.ReadUInt32(), ReadStreamName(fs, reader));
                // 同名流只取首次出现的项，跳过后续重复项而非静默覆盖
                tablesStreamOffset = streamName switch
                {
                    "#~" or "#-" or "#Strings" when tablesStreamOffset == -1 => metaDataOffset + offset,
                    _ => tablesStreamOffset
                };
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
            char b;
            while (fs.Position < fs.Length && nameBuilder.Length < 32 && (b = reader.ReadChar()) != 0)
            {
                nameBuilder.Append(b);
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
            bool checkResult = (fs.Length - tablesOffset) switch
            {
                < 0 => false,
                _ => (fs.Length - tablesOffset - size) switch
                {
                    < 0 => false,
                    _ => size switch
                    {
                        < 24 => false,
                        _ => true
                    }
                }
            };

            if(checkResult)
            {
                ParseMetadataTablesCore(fs, reader, peInfo, tablesOffset, size, stringHeapOffset, stringHeapSize);
            }
        }

        private static void ParseMetadataTablesCore(FileStream fs, BinaryReader reader, PEInfo peInfo, long tablesOffset, uint size, long stringHeapOffset, uint stringHeapSize)
        {
            try
            {
                (long originalPosition, fs.Position, uint _, byte _, byte _, byte heapSizes, byte _, ulong maskValid, ulong _, uint[] rowCounts) = (fs.Position, tablesOffset, reader.ReadUInt32(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadUInt64(), reader.ReadUInt64(), new uint[64]);
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

                // HeapSizes bit6(0x40)=ExtraData：置位时行数数组之后紧跟一个额外的 4 字节字段(ECMA-335)，
                // 须跳过，否则 tablesDataOffset 提前 4 字节、TypeDef 及后续表偏移全部错位
                if ((heapSizes & 0x40) != 0)
                {
                    if (fs.Position + 4 > fs.Length)
                    {
                        return;
                    }

                    fs.Position += 4;
                }

                // 行数数组之后即为表数据起始位置
                if (IsTablePresent(maskValid, 2))
                {
                    ParseTypeDefTable(fs, reader, peInfo, fs.Position, fs.Position, stringHeapSize, heapSizes, maskValid, rowCounts, Math.Min(tablesOffset + size, fs.Length));
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Utils.AppLogger.Log($"元数据表解析错误: {ex.Message}");
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
                (long moduleRowSize, long typeRefRowSize, long typeDefRowSize) = (2L + stringIndexSize + (3L * HeapIndexSize(heapSizes, 1)), CodedIndexSize(2, rowCounts, ResolutionScopeTables) + (2L * stringIndexSize), 4L + (2L * stringIndexSize) + CodedIndexSize(2, rowCounts, TypeDefOrRefTables) + SimpleIndexSize(rowCounts, FieldTableIndex) + SimpleIndexSize(rowCounts, MethodDefTableIndex));
                long typeDefStart = ComputeTypeDefStart(tablesDataOffset, maskValid, rowCounts, moduleRowSize, typeRefRowSize);
                for (long i = 0, rowStart = typeDefStart + i * typeDefRowSize; i < Math.Min(rowCounts[2], (uint)PEConstants.MaxExportEntries) && rowStart >= 0 && rowStart + typeDefRowSize <= tablesEnd; i++, rowStart += typeDefStart + i * typeDefRowSize)
                {
                    fs.Position = rowStart;
                    TryCollectTypeDefRow(fs, reader, peInfo, stringHeapOffset, stringHeapSize, stringIndexSize, (uint)i);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Utils.AppLogger.Log($"TypeDef表解析错误: {ex.Message}");
            }
        }

        private static long GetOffsetPlus(uint[] rowCounts, ulong maskValid, int tableIdx, long rowSize)
        {
            return IsTablePresent(maskValid, tableIdx) switch
            {
                true => rowCounts[tableIdx] * rowSize,
                _ => 0
            };
        }
        // 定位 TypeDef 表起始：跳过其前的 Module(0)、TypeRef(1) 表
        private static long ComputeTypeDefStart(long tablesDataOffset, ulong maskValid, uint[] rowCounts, long moduleRowSize, long typeRefRowSize)
        {
            return tablesDataOffset + GetOffsetPlus(rowCounts, maskValid, 0, moduleRowSize) + GetOffsetPlus(rowCounts, maskValid, 1, typeRefRowSize);
        }

        private static string GetfullName(string typeName, string namespaceName)
        {
            return string.IsNullOrEmpty(namespaceName) switch
            {
                true => typeName,
                _ => $"{namespaceName}.{typeName}"
            };
        }
        // 读取一行 TypeDef，仅收集公开可见类型（Public=1 / NestedPublic=2）到导出列表
        private static void TryCollectTypeDefRow(FileStream fs, BinaryReader reader, PEInfo peInfo, long stringHeapOffset, uint stringHeapSize, int stringIndexSize, uint i)
        {
            // Extends/FieldList/MethodList 无需读取：下一行由 rowStart 推进
            (uint flags, uint nameIndex, uint namespaceIndex) = (reader.ReadUInt32(), ReadHeapIndex(reader, stringIndexSize), ReadHeapIndex(reader, stringIndexSize));
            if ((flags & 0x7) is (1u or 2u))
            {
                TryCollectTypeDefRowCore(fs, reader, peInfo, stringHeapOffset, stringHeapSize, namespaceIndex, nameIndex, i);
            }
        }

        private static void TryCollectTypeDefRowCore(FileStream fs, BinaryReader reader, PEInfo peInfo, long stringHeapOffset, uint stringHeapSize, uint namespaceIndex, uint nameIndex, uint i)
        {
            string fullName = GetfullName(ReadStringFromHeap(fs, reader, stringHeapOffset, stringHeapSize, nameIndex), ReadStringFromHeap(fs, reader, stringHeapOffset, stringHeapSize, namespaceIndex));

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
