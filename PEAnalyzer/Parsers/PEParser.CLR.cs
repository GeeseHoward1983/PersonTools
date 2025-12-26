using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using MyTool.PEAnalyzer.Resources;
using MyTool.PEAnalyzer.Models;

namespace MyTool
{
    /// <summary>
    /// PE文件解析器CLR信息解析模块
    /// 专门负责解析.NET程序集的CLR运行时头信息
    /// </summary>
    public static partial class PEParserCLR
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
    }
}