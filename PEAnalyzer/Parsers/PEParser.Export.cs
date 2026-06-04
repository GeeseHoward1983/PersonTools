using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Parsers;
using PersonalTools.PEAnalyzer.Resources;
using System.IO;
using System.Text;

namespace PersonalTools
{
    /// <summary>
    /// PE文件导出表解析器
    /// 专门负责解析PE文件的导出表信息
    /// </summary>
    internal static partial class PEParser
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
                    long exportOffset = Utilities.RvaToOffset(exportRVA, peInfo.SectionHeaders);

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

                            IMAGEEXPORTDIRECTORY exportDir = new()
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
                                uint functionRVA = functionAddresses[i];

                                ExportFunctionInfo exportFunc = new()
                                {
                                    Ordinal = (int)(i + exportDir.Base),
                                    RVA = functionRVA
                                };

                                exportFunc.Name = functionNames.TryGetValue(i, out string? value) ? value : $"Ordinal_{exportFunc.Ordinal}";
                                peInfo.ExportFunctions.Add(exportFunc);
                            }
                        }
                        finally
                        {
                            fs.Position = originalPosition;
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                // 忽略导出表解析错误
                Console.WriteLine($"导出表解析错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                // 忽略导出表解析错误
                Console.WriteLine($"导出表解析错误: {ex.Message}");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // 忽略导出表解析错误
                Console.WriteLine($"导出表解析错误: {ex.Message}");
            }
        }

        private static List<uint> ReadUInt32ListAtRva(FileStream fs, BinaryReader reader, List<IMAGESECTIONHEADER> sections, uint rva, uint count)
        {
            List<uint> result = [];
            if (rva == 0 || count == 0)
            {
                return result;
            }

            long offset = Utilities.RvaToOffset(rva, sections);
            if (offset == -1 || offset >= fs.Length)
            {
                return result;
            }

            long originalPosition = fs.Position;
            try
            {
                fs.Position = offset;
                for (int i = 0; i < count && fs.Position + 4 <= fs.Length; i++)
                {
                    result.Add(reader.ReadUInt32());
                }
            }
            finally
            {
                fs.Position = originalPosition;
            }

            return result;
        }

        private static List<ushort> ReadUInt16ListAtRva(FileStream fs, BinaryReader reader, List<IMAGESECTIONHEADER> sections, uint rva, uint count)
        {
            List<ushort> result = [];
            if (rva == 0 || count == 0)
            {
                return result;
            }

            long offset = Utilities.RvaToOffset(rva, sections);
            if (offset == -1 || offset >= fs.Length)
            {
                return result;
            }

            long originalPosition = fs.Position;
            try
            {
                fs.Position = offset;
                for (int i = 0; i < count && fs.Position + 2 <= fs.Length; i++)
                {
                    result.Add(reader.ReadUInt16());
                }
            }
            finally
            {
                fs.Position = originalPosition;
            }

            return result;
        }

        private static Dictionary<int, string> BuildExportFunctionNameMap(FileStream fs, BinaryReader reader, List<IMAGESECTIONHEADER> sections, List<uint> nameRVAs, List<ushort> ordinals)
        {
            Dictionary<int, string> result = [];
            int entryCount = Math.Min(nameRVAs.Count, ordinals.Count);

            for (int i = 0; i < entryCount; i++)
            {
                uint nameRVA = nameRVAs[i];
                long nameOffset = Utilities.RvaToOffset(nameRVA, sections);
                if (nameOffset == -1 || nameOffset >= fs.Length)
                {
                    continue;
                }

                long originalPosition = fs.Position;
                try
                {
                    fs.Position = nameOffset;
                    string functionName = Utilities.ReadNullTerminatedString(reader);
                    if (!string.IsNullOrEmpty(functionName))
                    {
                        result[ordinals[i]] = functionName;
                    }
                }
                finally
                {
                    fs.Position = originalPosition;
                }
            }

            return result;
        }
    }
}