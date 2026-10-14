using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.ExtendedProperties;
using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Resources;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

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

                            List<uint> functionAddresses = ReadUIntListAtRva<uint>(fs, reader, peInfo.SectionHeaders, exportDir.AddressOfFunctions, exportDir.NumberOfFunctions);
                            List<uint> functionNameRVAs = ReadUIntListAtRva<uint>(fs, reader, peInfo.SectionHeaders, exportDir.AddressOfNames, exportDir.NumberOfNames);
                            List<ushort> nameOrdinals = ReadUIntListAtRva<ushort>(fs, reader, peInfo.SectionHeaders, exportDir.AddressOfNameOrdinals, exportDir.NumberOfNames);
                            Dictionary<int, string> functionNames = BuildExportFunctionNameMap(fs, reader, peInfo.SectionHeaders, functionNameRVAs, nameOrdinals);

                            for (int i = 0; i < functionAddresses.Count; i++)
                            {
                                // EAT 中 RVA==0 是未分配序号的空槽（序号空洞），并非真实导出；
                                // 跳过以免在导出列表产生「Ordinal_N, RVA 0x00000000」幻影项
                                if (functionAddresses[i] == 0)
                                {
                                    continue;
                                }

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
                Utils.AppLogger.Log($"导出表解析错误: {ex.Message}");
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

            exportFunc.Name = functionRVA switch
            {
                >0 or < 0 => (functionRVA >= exportRVA) switch
                {
                   true => (functionRVA < exportRVA + exportSize) switch
                   {
                       true => string.IsNullOrEmpty(ReadForwarderString(fs, reader, sections, functionRVA)) switch
                       {
                           true => $"{exportFunc.Name} (forwarded -> {ReadForwarderString(fs, reader, sections, functionRVA)})",
                           _ => functionNames.TryGetValue(i, out string? value) switch
                           {
                               true => value,
                               _ => $"Ordinal_{exportFunc.Ordinal}",
                           },
                       },
                       _ => functionNames.TryGetValue(i, out string? value) switch
                       {
                           true => value,
                           _ => $"Ordinal_{exportFunc.Ordinal}",
                       }
                   },
                    _ => ReadForwarderString(fs, reader, sections, functionRVA)
                },
                _ => functionNames.TryGetValue(i, out string? value) switch
                {
                    true => value,
                    _ => $"Ordinal_{exportFunc.Ordinal}",
                }
            };

            return exportFunc;
        }

        static T ReaderUInt<T>(BinaryReader reader)
        {
            return Marshal.SizeOf<T>() switch
            {
                2 => (T)(object)reader.ReadUInt16(),
                4 => (T)(object)reader.ReadUInt32(),
                _ => throw new NotSupportedException($"Unsupported type size: {Marshal.SizeOf<T>()}")
            };
        }


        static List<T> ReadFunc<T>(BinaryReader reader, int clampedCount)
        {
            List<T> result = new(clampedCount);
            for (uint i = 0; i < clampedCount; i++)
            {
                result.Add(ReaderUInt<T>(reader));
            }

            return result;
        }

        private static List<T> ReadUIntListAtRva<T>(FileStream fs, BinaryReader reader, List<IMAGE_SECTION_HEADER> sections, uint rva, uint count)
        {
            return rva switch
            {
                0 => [],
                _ => count switch
                {
                    0 => [],
                    _ => PEParserUtils.ReadAtOffset(fs, PEParserUtils.RvaToOffset(rva, sections, Math.Min(count, PEConstants.MaxExportEntries) * (uint)Marshal.SizeOf<T>()), [], () =>
                    {
                        return ReadFunc<T>(reader, (int)Math.Min(count, PEConstants.MaxExportEntries));
                    })
                }
            };
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