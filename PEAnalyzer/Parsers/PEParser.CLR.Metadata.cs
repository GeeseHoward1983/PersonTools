using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Resources;
using System.IO;
using System.Text;

namespace PersonalTools
{
    /// <summary>
    /// PE文件解析器CLR元数据解析模块
    /// 专门负责解析.NET程序集的元数据信息
    /// </summary>
    public static partial class PEParserCLR
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
                long metaDataOffset = PEResourceParserCore.RvaToOffset(metaDataRVA, peInfo.SectionHeaders);
                if (metaDataOffset == -1 || metaDataOffset >= fs.Length)
                    return;

                long originalPosition = fs.Position;
                fs.Position = metaDataOffset;

                // 检查是否有足够数据读取元数据头
                if (fs.Position + 16 > fs.Length)
                    return;

                // 读取元数据头
                uint signature = reader.ReadUInt32();
                ushort majorVersion = reader.ReadUInt16();
                ushort minorVersion = reader.ReadUInt16();
                uint reserved = reader.ReadUInt32();
                uint length = reader.ReadUInt32();

                // 检查签名是否正确 (BSJB = 0x42534A42)
                if (signature != 0x42534A42)
                    return;

                // 读取版本字符串
                string versionString = PEResourceParserCore.ReadUnicodeStringWithMaxLength(reader, (int)length);

                // 跳过对齐填充
                long currentPosition = fs.Position;
                long alignedPosition = (currentPosition + 3) & ~3;
                if (alignedPosition < fs.Length)
                {
                    fs.Position = alignedPosition;
                }

                // 读取流数量
                if (fs.Position + 2 > fs.Length)
                    return;

                ushort streams = reader.ReadUInt16();

                // 读取流信息
                for (int i = 0; i < streams; i++)
                {
                    if (fs.Position + 8 > fs.Length)
                        break;

                    uint offset = reader.ReadUInt32();
                    uint size = reader.ReadUInt32();

                    // 读取流名称
                    var nameBuilder = new StringBuilder();
                    byte b;
                    while ((b = reader.ReadByte()) != 0)
                    {
                        nameBuilder.Append((char)b);
                        if (fs.Position >= fs.Length)
                            break;
                    }

                    // 对齐到4字节边界
                    currentPosition = fs.Position;
                    alignedPosition = (currentPosition + 3) & ~3;
                    if (alignedPosition < fs.Length && alignedPosition > fs.Position)
                    {
                        fs.Position = alignedPosition;
                    }

                    string streamName = nameBuilder.ToString();

                    // 如果是导出类型流 (#~ 或 #-)
                    if (streamName == "#~" || streamName == "#-")
                    {
                        // 解析元数据表以获取类型信息
                        ParseMetadataTables(fs, reader, peInfo, metaDataOffset + offset, size);
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"元数据解析错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析元数据表
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="tablesOffset">表偏移</param>
        /// <param name="size">表大小</param>
        private static void ParseMetadataTables(FileStream fs, BinaryReader reader, PEInfo peInfo, long tablesOffset, uint size)
        {
            try
            {
                if (tablesOffset >= fs.Length || tablesOffset + size > fs.Length)
                    return;

                long originalPosition = fs.Position;
                fs.Position = tablesOffset;

                // 读取表头
                uint reserved1 = reader.ReadUInt32();
                byte majorVersion = reader.ReadByte();
                byte minorVersion = reader.ReadByte();
                byte heapSizes = reader.ReadByte();
                byte reserved2 = reader.ReadByte();
                ulong maskValid = reader.ReadUInt64();
                ulong maskSorted = reader.ReadUInt64();

                // 计算有多少个表
                int tableCount = 0;
                for (int i = 0; i < 64; i++)
                {
                    if ((maskValid & ((ulong)1 << i)) != 0)
                        tableCount++;
                }

                // 读取每个表的行数
                var rowCounts = new uint[64];
                int rowIndex = 0;
                for (int i = 0; i < 64; i++)
                {
                    if ((maskValid & ((ulong)1 << i)) != 0)
                    {
                        rowCounts[i] = reader.ReadUInt32();
                        rowIndex++;
                    }
                }

                // TypeDef 表的索引是2
                const int TYPE_DEF_TABLE_INDEX = 2;
                if ((maskValid & ((ulong)1 << TYPE_DEF_TABLE_INDEX)) != 0)
                {
                    uint typeDefCount = rowCounts[TYPE_DEF_TABLE_INDEX];
                    // 解析TypeDef表获取公开类型信息
                    ParseTypeDefTable(fs, reader, peInfo, typeDefCount, tablesOffset, heapSizes, maskValid, rowCounts);
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"元数据表解析错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析TypeDef表
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="typeDefCount">类型定义数量</param>
        /// <param name="tablesOffset">元数据表偏移</param>
        /// <param name="heapSizes">堆大小标志</param>
        /// <param name="maskValid">有效表掩码</param>
        /// <param name="rowCounts">行数数组</param>
        private static void ParseTypeDefTable(FileStream fs, BinaryReader reader, PEInfo peInfo, uint typeDefCount, long tablesOffset, byte heapSizes, ulong maskValid, uint[] rowCounts)
        {
            try
            {
                // 计算String堆的偏移
                long stringHeapOffset = CalculateStringHeapOffset(tablesOffset, heapSizes, maskValid, rowCounts);

                // TypeDef表结构:
                // Flags (4 bytes)
                // TypeName (index into String heap)
                // TypeNamespace (index into String heap)
                // Extends (index into TypeDef, TypeRef, or TypeSpec table)
                // FieldList (index into Field table)
                // MethodList (index into MethodDef table)

                for (int i = 0; i < typeDefCount; i++)
                {
                    if (fs.Position + 14 > fs.Length) // 最小大小检查
                        break;

                    uint flags = reader.ReadUInt32();
                    uint typeNameIndex = reader.ReadUInt32();
                    uint typeNamespaceIndex = reader.ReadUInt32();
                    reader.ReadUInt32(); // Extends索引
                    reader.ReadUInt32(); // FieldList索引
                    reader.ReadUInt32(); // MethodList索引

                    // 检查类型是否公开 (IsPublic flag)
                    if ((flags & 0x00000001) != 0)
                    {
                        // 这是一个公开类型，获取类型名称
                        string typeName = ReadStringFromHeap(fs, reader, stringHeapOffset, typeNameIndex);

                        // 获取命名空间名称
                        string namespaceName = ReadStringFromHeap(fs, reader, stringHeapOffset, typeNamespaceIndex);

                        string fullName = string.IsNullOrEmpty(namespaceName) ? typeName : $"{namespaceName}.{typeName}";

                        var exportFunc = new ExportFunctionInfo
                        {
                            Name = fullName,
                            Ordinal = i,
                            RVA = 0 // 对于.NET程序集，RVA不适用
                        };
                        peInfo.ExportFunctions.Add(exportFunc);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TypeDef表解析错误: {ex.Message}");
            }
        }
    }
}