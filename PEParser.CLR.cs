using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace MyTool
{
    /// <summary>
    /// PE文件解析器CLR信息解析模块
    /// 专门负责解析.NET程序集的CLR运行时头信息
    /// </summary>
    public static class PEParserCLR
    {
        /// <summary>
        /// 解析CLR运行时头信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        public static void ParseCLRHeaderInfo(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            try
            {
                // CLR运行时头在数据目录中的索引是14 (从0开始计数)
                const int CLR_RUNTIME_HEADER_INDEX = 14;

                // 检查是否存在CLR运行时头
                if (peInfo.OptionalHeader.DataDirectory.Length > CLR_RUNTIME_HEADER_INDEX &&
                    peInfo.OptionalHeader.DataDirectory[CLR_RUNTIME_HEADER_INDEX].VirtualAddress != 0)
                {
                    uint clrHeaderRVA = peInfo.OptionalHeader.DataDirectory[CLR_RUNTIME_HEADER_INDEX].VirtualAddress;
                    long clrHeaderOffset = PEResourceParserCore.RvaToOffset(clrHeaderRVA, peInfo.SectionHeaders);

                    if (clrHeaderOffset != -1 && clrHeaderOffset < fs.Length)
                    {
                        // 解析CLR运行时头
                        ParseCLRHeader(fs, reader, peInfo, clrHeaderOffset);
                    }
                }
            }
            catch (Exception ex)
            {
                // CLR头解析错误不中断程序执行
                Console.WriteLine($"CLR运行时头解析错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析CLR运行时头
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="clrHeaderOffset">CLR头偏移</param>
        private static void ParseCLRHeader(FileStream fs, BinaryReader reader, PEInfo peInfo, long clrHeaderOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = clrHeaderOffset;

                // 检查是否有足够的数据读取IMAGE_COR20_HEADER
                if (fs.Position + 72 > fs.Length) // IMAGE_COR20_HEADER最小大小为72字节
                {
                    return;
                }

                // 读取CLR运行时头
                var clrHeader = new IMAGE_COR20_HEADER
                {
                    cb = reader.ReadUInt32(),
                    MajorRuntimeVersion = reader.ReadUInt16(),
                    MinorRuntimeVersion = reader.ReadUInt16(),
                    MetaData = new IMAGE_DATA_DIRECTORY
                    {
                        VirtualAddress = reader.ReadUInt32(),
                        Size = reader.ReadUInt32()
                    },
                    Flags = reader.ReadUInt32(),
                    EntryPointTokenOrRva = reader.ReadUInt32(),
                    Resources = new IMAGE_DATA_DIRECTORY
                    {
                        VirtualAddress = reader.ReadUInt32(),
                        Size = reader.ReadUInt32()
                    },
                    StrongNameSignature = new IMAGE_DATA_DIRECTORY
                    {
                        VirtualAddress = reader.ReadUInt32(),
                        Size = reader.ReadUInt32()
                    },
                    CodeManagerTable = new IMAGE_DATA_DIRECTORY
                    {
                        VirtualAddress = reader.ReadUInt32(),
                        Size = reader.ReadUInt32()
                    },
                    VTableFixups = new IMAGE_DATA_DIRECTORY
                    {
                        VirtualAddress = reader.ReadUInt32(),
                        Size = reader.ReadUInt32()
                    },
                    ExportAddressTableJumps = new IMAGE_DATA_DIRECTORY
                    {
                        VirtualAddress = reader.ReadUInt32(),
                        Size = reader.ReadUInt32()
                    },
                    ManagedNativeHeader = new IMAGE_DATA_DIRECTORY
                    {
                        VirtualAddress = reader.ReadUInt32(),
                        Size = reader.ReadUInt32()
                    }
                };

                // 保存CLR信息到PEInfo
                peInfo.CLRInfo = new CLRInfo
                {
                    MajorRuntimeVersion = clrHeader.MajorRuntimeVersion,
                    MinorRuntimeVersion = clrHeader.MinorRuntimeVersion,
                    Flags = clrHeader.Flags,
                    EntryPointTokenOrRva = clrHeader.EntryPointTokenOrRva,
                    HasMetaData = clrHeader.MetaData.VirtualAddress != 0,
                    HasResources = clrHeader.Resources.VirtualAddress != 0,
                    HasStrongNameSignature = clrHeader.StrongNameSignature.VirtualAddress != 0,
                    IsILonly = (clrHeader.Flags & 0x00000001) != 0,
                    Is32BitRequired = (clrHeader.Flags & 0x00000002) != 0,
                    Is32BitPreferred = (clrHeader.Flags & 0x00040000) != 0,
                    IsStrongNameSigned = (clrHeader.Flags & 0x00000008) != 0,
                    PEMachineType = peInfo.NtHeaders.FileHeader.Machine // 保存PE头中的Machine字段
                };

                // 解析元数据以获取导出类信息
                if (clrHeader.MetaData.VirtualAddress != 0)
                {
                    ParseMetaData(fs, reader, peInfo, clrHeader.MetaData.VirtualAddress);
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CLR头解析错误: {ex.Message}");
            }
        }

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
                long stringHeapOffset = CalculateStringHeapOffset(fs, tablesOffset, heapSizes, maskValid, rowCounts);
                
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
        
        /// <summary>
        /// 计算String堆的偏移
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="tablesOffset">表偏移</param>
        /// <param name="heapSizes">堆大小标志</param>
        /// <param name="maskValid">有效表掩码</param>
        /// <param name="rowCounts">行数数组</param>
        /// <returns>String堆的偏移</returns>
        private static long CalculateStringHeapOffset(FileStream fs, long tablesOffset, byte heapSizes, ulong maskValid, uint[] rowCounts)
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
            switch (tableIndex)
            {
                case 0: // Module
                    return 2 + (IsSmallIndex(heapSizes, 1) ? 2 : 4) + 
                           (IsSmallIndex(heapSizes, 2) ? 2 : 4) + 
                           (IsSmallIndex(heapSizes, 0) ? 2 : 4) + 
                           (IsSmallIndex(heapSizes, 0) ? 2 : 4);
                case 1: // TypeRef
                    return (IsSmallIndex(heapSizes, 1) ? 2 : 4) + 
                           (IsSmallIndex(heapSizes, 2) ? 2 : 4) + 
                           (IsSmallIndex(heapSizes, 2) ? 2 : 4);
                case 2: // TypeDef
                    return 4 + (IsSmallIndex(heapSizes, 2) ? 2 : 4) + 
                           (IsSmallIndex(heapSizes, 2) ? 2 : 4) + 
                           GetCodedIndexSize(1, maskValid, rowCounts) + // Extends coded index
                           GetCodedIndexSize(2, maskValid, rowCounts) + // FieldList coded index
                           GetCodedIndexSize(2, maskValid, rowCounts);  // MethodList coded index
                // 其他表...
                default:
                    return 16; // 默认大小
            }
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

    /// <summary>
    /// CLR运行时头结构
    /// </summary>
    public struct IMAGE_COR20_HEADER
    {
        public uint cb;                              // 结构大小
        public ushort MajorRuntimeVersion;           // 主版本号
        public ushort MinorRuntimeVersion;           // 次版本号
        public IMAGE_DATA_DIRECTORY MetaData;        // 元数据
        public uint Flags;                           // 标志位
        public uint EntryPointTokenOrRva;            // 入口点标记或RVA
        public IMAGE_DATA_DIRECTORY Resources;       // 资源
        public IMAGE_DATA_DIRECTORY StrongNameSignature; // 强名称签名
        public IMAGE_DATA_DIRECTORY CodeManagerTable;    // 代码管理器表
        public IMAGE_DATA_DIRECTORY VTableFixups;        // V表修复
        public IMAGE_DATA_DIRECTORY ExportAddressTableJumps; // 导出地址表跳转
        public IMAGE_DATA_DIRECTORY ManagedNativeHeader;     // 托管本地头
    }

    /// <summary>
    /// CLR信息
    /// </summary>
    public class CLRInfo
    {
        public ushort MajorRuntimeVersion { get; set; }
        public ushort MinorRuntimeVersion { get; set; }
        public uint Flags { get; set; }
        public uint EntryPointTokenOrRva { get; set; }
        public bool HasMetaData { get; set; }
        public bool HasResources { get; set; }
        public bool HasStrongNameSignature { get; set; }
        public bool IsILonly { get; set; }
        public bool Is32BitRequired { get; set; }
        public bool Is32BitPreferred { get; set; }
        public bool IsStrongNameSigned { get; set; }
        
        // 保存PE头中的Machine字段，用于更准确地判断架构
        public ushort PEMachineType { get; set; }
        
        // 获取运行时版本描述
        public string RuntimeVersion => $"{MajorRuntimeVersion}.{MinorRuntimeVersion}";
        
        /// <summary>
        /// 获取.NET程序的架构类型
        /// </summary>
        public string Architecture
        {
            get
            {
                // 首先根据CLR头中的标志位判断.NET程序的目标架构类型
                if (Is32BitRequired)
                {
                    return "x86"; // 明确要求32位运行
                }
                else if (!Is32BitRequired && Is32BitPreferred)
                {
                    return "x86"; // 32位首选（在64位系统上通过WoW64运行）
                }
                else if (!Is32BitRequired && !Is32BitPreferred)
                {
                    return "Any CPU"; // 可以在任何CPU架构上运行
                }
                
                return "Unknown"; // 无法确定的架构
            }
        }
        
        // 获取标志位描述
        public List<string> FlagDescriptions
        {
            get
            {
                var descriptions = new List<string>();
                if (IsILonly) descriptions.Add("IL Only");
                if (Is32BitRequired) descriptions.Add("32-Bit Required");
                if (Is32BitPreferred) descriptions.Add("32-Bit Preferred");
                if (IsStrongNameSigned) descriptions.Add("Strong Name Signed");
                return descriptions;
            }
        }
    }
}