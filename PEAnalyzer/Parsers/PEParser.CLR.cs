using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Resources;
using System.IO;

namespace PersonalTools.PEAnalyzer.Parsers
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

                // 检查是否存在CLR运行时头
                if (peInfo.OptionalHeader.DataDirectory.Length > PEConstants.DirectoryClrHeader &&
                    peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryClrHeader].VirtualAddress != 0)
                {
                    uint clrHeaderRVA = peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryClrHeader].VirtualAddress;
                    long clrHeaderOffset = PEParserUtils.RvaToOffset(clrHeaderRVA, peInfo.SectionHeaders);

                    if (clrHeaderOffset != -1 && clrHeaderOffset < fs.Length)
                    {
                        // 解析CLR运行时头
                        ParseCLRHeader(fs, reader, peInfo, clrHeaderOffset);
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
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
                if (fs.Position + PEConstants.Cor20HeaderMinSize > fs.Length) // IMAGE_COR20_HEADER最小大小为72字节
                {
                    return;
                }

                // 读取CLR运行时头
                IMAGE_COR20_HEADER clrHeader = ReadCor20Header(reader);

                // 保存CLR信息到PEInfo
                peInfo.CLRInfo = BuildClrInfo(clrHeader, peInfo.NtHeaders.FileHeader.Machine);

                // 解析元数据以获取导出类信息
                if (clrHeader.MetaData.VirtualAddress != 0)
                {
                    ParseMetaData(fs, reader, peInfo, clrHeader.MetaData.VirtualAddress);
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Console.WriteLine($"CLR头解析错误: {ex.Message}");
            }
        }

        // 从当前位置读取 IMAGE_COR20_HEADER（72 字节：cb + 运行时版本 + 8 个数据目录 + Flags/EntryPoint）
        private static IMAGE_COR20_HEADER ReadCor20Header(BinaryReader reader)
        {
            return new IMAGE_COR20_HEADER
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
        }

        // 将 IMAGE_COR20_HEADER 与 PE 机器类型映射为展示用 CLRInfo（标志位按 ECMA-335 / COR 头解释）
        private static CLRInfo BuildClrInfo(IMAGE_COR20_HEADER clrHeader, ushort machine)
        {
            return new CLRInfo
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
                Is32BitPreferred = (clrHeader.Flags & 0x00020000) != 0, // COMIMAGE_FLAGS_32BITPREFERRED
                IsStrongNameSigned = (clrHeader.Flags & 0x00000008) != 0,
                PEMachineType = machine // 保存PE头中的Machine字段
            };
        }
    }
}