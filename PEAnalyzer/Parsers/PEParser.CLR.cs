using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Resources;
using System.IO;

namespace PersonalTools
{
    /// <summary>
    /// PE文件解析器CLR信息解析模块
    /// 专门负责解析.NET程序集的CLR运行时头信息
    /// </summary>
    internal static partial class PEParserCLR
    {
        /// <summary>
        /// 解析CLR运行时头信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        internal static void ParseCLRHeaderInfo(FileStream fs, BinaryReader reader, PEInfo peInfo)
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
            catch (IOException ex)
            {
                // CLR头解析错误不中断程序执行
                Console.WriteLine($"CLR运行时头解析IO错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"CLR运行时头解析权限错误: {ex.Message}");
            }
            // 你可以根据需要添加其他具体异常类型
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
                IMAGE_COR20_HEADER clrHeader = new()
                {
                    cb = reader.ReadUInt32(),
                    MajorRuntimeVersion = reader.ReadUInt16(),
                    MinorRuntimeVersion = reader.ReadUInt16(),
                    MetaData = new IMAGEDATADIRECTORY
                    {
                        VirtualAddress = reader.ReadUInt32(),
                        Size = reader.ReadUInt32()
                    },
                    Flags = reader.ReadUInt32(),
                    EntryPointTokenOrRva = reader.ReadUInt32(),
                    Resources = new IMAGEDATADIRECTORY
                    {
                        VirtualAddress = reader.ReadUInt32(),
                        Size = reader.ReadUInt32()
                    },
                    StrongNameSignature = new IMAGEDATADIRECTORY
                    {
                        VirtualAddress = reader.ReadUInt32(),
                        Size = reader.ReadUInt32()
                    },
                    CodeManagerTable = new IMAGEDATADIRECTORY
                    {
                        VirtualAddress = reader.ReadUInt32(),
                        Size = reader.ReadUInt32()
                    },
                    VTableFixups = new IMAGEDATADIRECTORY
                    {
                        VirtualAddress = reader.ReadUInt32(),
                        Size = reader.ReadUInt32()
                    },
                    ExportAddressTableJumps = new IMAGEDATADIRECTORY
                    {
                        VirtualAddress = reader.ReadUInt32(),
                        Size = reader.ReadUInt32()
                    },
                    ManagedNativeHeader = new IMAGEDATADIRECTORY
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
            catch (IOException ex)
            {
                Console.WriteLine($"CLR头解析IO错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"CLR头解析权限错误: {ex.Message}");
            }
            // 你可以根据需要添加其他具体异常类型
        }
    }
}