using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Resources;
using System.IO;
using System.Text;

namespace PersonalTools
{
    /// <summary>
    /// PE文件导出表解析器
    /// 专门负责解析PE文件的导出表信息
    /// </summary>
    public static partial class PEParser
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
                const int EXPORT_DIRECTORY_INDEX = 0;

                if (peInfo.OptionalHeader.DataDirectory.Length > EXPORT_DIRECTORY_INDEX &&
                    peInfo.OptionalHeader.DataDirectory[EXPORT_DIRECTORY_INDEX].VirtualAddress != 0)
                {
                    uint exportRVA = peInfo.OptionalHeader.DataDirectory[EXPORT_DIRECTORY_INDEX].VirtualAddress;
                    long exportOffset = PEResourceParserCore.RvaToOffset(exportRVA, peInfo.SectionHeaders);

                    if (exportOffset != -1 && exportOffset < fs.Length)
                    {
                        long originalPosition = fs.Position;
                        fs.Position = exportOffset;

                        // 读取导出目录
                        var exportDir = new IMAGE_EXPORT_DIRECTORY
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

                        // 获取函数地址表
                        var functionAddresses = new List<uint>();
                        long funcAddrOffset = PEResourceParserCore.RvaToOffset(exportDir.AddressOfFunctions, peInfo.SectionHeaders);
                        if (funcAddrOffset != -1 && funcAddrOffset < fs.Length)
                        {
                            long tempPosition = fs.Position;
                            fs.Position = funcAddrOffset;

                            for (int i = 0; i < exportDir.NumberOfFunctions; i++)
                            {
                                functionAddresses.Add(reader.ReadUInt32());
                            }

                            fs.Position = tempPosition;
                        }

                        // 获取函数名称表
                        var functionNameRVAs = new List<uint>();
                        long funcNameOffset = PEResourceParserCore.RvaToOffset(exportDir.AddressOfNames, peInfo.SectionHeaders);
                        if (funcNameOffset != -1 && funcNameOffset < fs.Length)
                        {
                            long tempPosition = fs.Position;
                            fs.Position = funcNameOffset;

                            for (int i = 0; i < exportDir.NumberOfNames; i++)
                            {
                                functionNameRVAs.Add(reader.ReadUInt32());
                            }

                            fs.Position = tempPosition;
                        }

                        // 获取名称序号表
                        var nameOrdinals = new List<ushort>();
                        long nameOrdinalOffset = PEResourceParserCore.RvaToOffset(exportDir.AddressOfNameOrdinals, peInfo.SectionHeaders);
                        if (nameOrdinalOffset != -1 && nameOrdinalOffset < fs.Length)
                        {
                            long tempPosition = fs.Position;
                            fs.Position = nameOrdinalOffset;

                            for (int i = 0; i < exportDir.NumberOfNames; i++)
                            {
                                nameOrdinals.Add(reader.ReadUInt16());
                            }

                            fs.Position = tempPosition;
                        }

                        // 解析函数名称
                        var functionNames = new Dictionary<int, string>();
                        for (int i = 0; i < functionNameRVAs.Count; i++)
                        {
                            uint nameRVA = functionNameRVAs[i];
                            long nameOffset = PEResourceParserCore.RvaToOffset(nameRVA, peInfo.SectionHeaders);

                            if (nameOffset != -1 && nameOffset < fs.Length)
                            {
                                long tempPosition = fs.Position;
                                fs.Position = nameOffset;

                                string functionName = ReadNullTerminatedString(reader);
                                functionNames[nameOrdinals[i]] = functionName;

                                fs.Position = tempPosition;
                            }
                        }

                        // 构建导出函数列表
                        for (int i = 0; i < functionAddresses.Count; i++)
                        {
                            uint functionRVA = functionAddresses[i];

                            // 检查是否是转发函数（RVA指向非导出节）
                            //bool isForwarded = false;
                            foreach (var section in peInfo.SectionHeaders)
                            {
                                if (functionRVA >= section.VirtualAddress &&
                                    functionRVA < section.VirtualAddress + section.VirtualSize)
                                {
                                    // 检查该节是否是导出节
                                    string sectionName = Encoding.UTF8.GetString(section.Name).Trim('\0');
                                    if (sectionName.Equals(".edata", StringComparison.OrdinalIgnoreCase))
                                    {
                                        //isForwarded = true;
                                        break;
                                    }
                                }
                            }

                            var exportFunc = new ExportFunctionInfo
                            {
                                Ordinal = (int)(i + exportDir.Base),
                                RVA = functionRVA
                            };

                            // 查找对应的函数名称
                            if (functionNames.TryGetValue(i, out string? value))
                            {
                                exportFunc.Name = value;
                            }
                            else
                            {
                                exportFunc.Name = $"Ordinal_{exportFunc.Ordinal}";
                            }

                            peInfo.ExportFunctions.Add(exportFunc);
                        }

                        fs.Position = originalPosition;
                    }
                }
            }
            catch (Exception ex)
            {
                // 忽略导出表解析错误
                Console.WriteLine($"导出表解析错误: {ex.Message}");
            }
        }
    }
}