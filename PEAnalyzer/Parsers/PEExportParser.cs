using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Resources;
using System.IO;
using System.Text;

namespace PersonalTools.PEAnalyzer.Parsers
{
    /// <summary>
    /// PE文件导出表解析器
    /// 专门负责解析PE文件的导出表信息
    /// </summary>
    internal static class PEExportParser
    {
        /// <summary>
        /// 解析导出表
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        internal static void ParseExportTable(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            try
            {
                // 导出表在数据目录的第1项 (IMAGE_DIRECTORY_ENTRY_EXPORT)
                if (peInfo.OptionalHeader.DataDirectory.Length > PEConstants.DirectoryExport &&
                    peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryExport].VirtualAddress != 0)
                {
                    uint exportRVA = peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryExport].VirtualAddress;
                    uint exportSize = peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryExport].Size;
                    long exportOffset = PEParserUtils.RvaToOffset(exportRVA, peInfo.SectionHeaders, PEConstants.ExportDirectorySize);

                    if (exportOffset != -1 && exportOffset < fs.Length)
                    {
                        long originalPosition = fs.Position;
                        try
                        {
                            fs.Position = exportOffset;

                            if (fs.Position + PEConstants.ExportDirectorySize > fs.Length)
                            {
                                return;
                            }

                            IMAGE_EXPORT_DIRECTORY exportDir = new()
                            {
                                Characteristics = reader.ReadUInt32(),
                                TimeDateStamp = reader.ReadUInt32(),
                                MajorVersion = reader.ReadUInt16(),
                                MinorVersion = reader.ReadUInt16(),
                                Name = reader.ReadUInt32(),
                                Base = reader.ReadUInt32(),
                                NumberOfFunctions = reader.ReadUInt32(),
                                NumberOfNames = reader.ReadUInt32(),
                                AddressOfFunctions = reader.ReadUInt32(),
                                AddressOfNames = reader.ReadUInt32(),
                                AddressOfNameOrdinals = reader.ReadUInt32()
                            };

                            List<uint> functionAddresses = ReadUInt32ListAtRva(fs, reader, peInfo.SectionHeaders, exportDir.AddressOfFunctions, exportDir.NumberOfFunctions);
                            List<uint> functionNameRVAs = ReadUInt32ListAtRva(fs, reader, peInfo.SectionHeaders, exportDir.AddressOfNames, exportDir.NumberOfNames);
                            List<ushort> nameOrdinals = ReadUInt16ListAtRva(fs, reader, peInfo.SectionHeaders, exportDir.AddressOfNameOrdinals, exportDir.NumberOfNames);
                            Dictionary<int, string> functionNames = BuildExportFunctionNameMap(fs, reader, peInfo.SectionHeaders, functionNameRVAs, nameOrdinals);

                            for (int i = 0; i < functionAddresses.Count; i++)
                            {
                                peInfo.ExportFunctions.Add(BuildExportFunction(
                                    fs, reader, peInfo.SectionHeaders, i, functionAddresses[i],
                                    exportDir.Base, functionNames, exportRVA, exportSize));
                            }
                        }
                        finally
                        {
                            fs.Position = originalPosition;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                // 忽略导出表解析错误
                PersonalTools.Utils.AppLogger.Log($"导出表解析错误: {ex.Message}");
            }
        }

        // 构造单个导出函数项（含序号、名称、转发目标）
        private static ExportFunctionInfo BuildExportFunction(FileStream fs, BinaryReader reader, List<IMAGE_SECTION_HEADER> sections, int i, uint functionRVA, uint baseOrdinal, Dictionary<int, string> functionNames, uint exportRVA, uint exportSize)
        {
            // baseOrdinal 取自不可信导出目录，i+baseOrdinal 用 long 计算避免回绕；
            // 序号超出 int 范围时夹到 int.MaxValue，防止 (int) 强转得到负序号
            long ordinal = (long)i + baseOrdinal;
            ExportFunctionInfo exportFunc = new()
            {
                Ordinal = ordinal > int.MaxValue ? int.MaxValue : (int)ordinal,
                RVA = functionRVA
            };

            exportFunc.Name = functionNames.TryGetValue(i, out string? value) ? value : $"Ordinal_{exportFunc.Ordinal}";

            // 转发导出：函数 RVA 落在导出目录范围内时指向 "DLL.Function" 字符串而非代码
            if (functionRVA != 0 && functionRVA >= exportRVA && functionRVA < (long)exportRVA + exportSize)
            {
                string forwarder = ReadForwarderString(fs, reader, sections, functionRVA);
                if (!string.IsNullOrEmpty(forwarder))
                {
                    exportFunc.Name = $"{exportFunc.Name} (forwarded -> {forwarder})";
                }
            }

            return exportFunc;
        }

        private static List<uint> ReadUInt32ListAtRva(FileStream fs, BinaryReader reader, List<IMAGE_SECTION_HEADER> sections, uint rva, uint count)
        {
            if (rva == 0 || count == 0)
            {
                return [];
            }

            // 安全：count 取自不可信导出目录，先夹到 PE 序号上限(64K)，避免畸形巨值触发海量空对象构造/界面卡死。
            int clampedCount = (int)Math.Min(count, (uint)PEConstants.MaxExportEntries);

            // 用 requiredLength = clampedCount*4 让整段必须落在同一节内，否则 RvaToOffset 返回 -1：
            // 既防越界，也避免内层循环以 fs.Length 为界跨节读到 EOF（旧实现的 DoS/误读向量）。
            long offset = PEParserUtils.RvaToOffset(rva, sections, (uint)(clampedCount * 4));
            return PEParserUtils.ReadAtOffset(fs, offset, new List<uint>(), () =>
            {
                List<uint> result = new(clampedCount);
                for (int i = 0; i < clampedCount; i++)
                {
                    result.Add(reader.ReadUInt32());
                }

                return result;
            });
        }

        private static List<ushort> ReadUInt16ListAtRva(FileStream fs, BinaryReader reader, List<IMAGE_SECTION_HEADER> sections, uint rva, uint count)
        {
            if (rva == 0 || count == 0)
            {
                return [];
            }

            int clampedCount = (int)Math.Min(count, (uint)PEConstants.MaxExportEntries);

            // 同 ReadUInt32ListAtRva：整段(每项 2 字节)须落在同一节内，否则返回 -1 降级为空表。
            long offset = PEParserUtils.RvaToOffset(rva, sections, (uint)(clampedCount * 2));
            return PEParserUtils.ReadAtOffset(fs, offset, new List<ushort>(), () =>
            {
                List<ushort> result = new(clampedCount);
                for (int i = 0; i < clampedCount; i++)
                {
                    result.Add(reader.ReadUInt16());
                }

                return result;
            });
        }

        private static string ReadForwarderString(FileStream fs, BinaryReader reader, List<IMAGE_SECTION_HEADER> sections, uint rva)
        {
            long offset = PEParserUtils.RvaToOffset(rva, sections);
            return PEParserUtils.ReadAtOffset(fs, offset, string.Empty, () => PEParserUtils.ReadNullTerminatedString(reader));
        }

        private static Dictionary<int, string> BuildExportFunctionNameMap(FileStream fs, BinaryReader reader, List<IMAGE_SECTION_HEADER> sections, List<uint> nameRVAs, List<ushort> ordinals)
        {
            Dictionary<int, string> result = [];
            int entryCount = Math.Min(nameRVAs.Count, ordinals.Count);

            for (int i = 0; i < entryCount; i++)
            {
                long nameOffset = PEParserUtils.RvaToOffset(nameRVAs[i], sections);
                string functionName = PEParserUtils.ReadAtOffset(fs, nameOffset, string.Empty, () => PEParserUtils.ReadNullTerminatedString(reader));
                if (!string.IsNullOrEmpty(functionName))
                {
                    result[ordinals[i]] = functionName;
                }
            }

            return result;
        }
    }
}