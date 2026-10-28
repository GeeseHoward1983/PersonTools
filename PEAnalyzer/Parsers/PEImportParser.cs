using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Parsers
{
    /// <summary>
    /// PE文件导入表解析器
    /// 专门负责解析PE文件的导入表信息
    /// </summary>
    internal static class PEImportParser
    {
        /// <summary>
        /// 解析导入表（标准导入表 + 延迟加载导入表）。
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        internal static void ParseImportTable(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
                ParseStandardImportTable(fs, reader, peInfo);
                // 解析延迟加载导入表并合并到主列表
                peInfo.ImportFunctions.AddRange(ParseDelayLoadImportTable(fs, reader, peInfo));
        }


        private static bool ParseStandardImportTableCheck(long fslength, int DataDirectoryLength, uint VirtualAddress, long importOffset)
        {
            return (DataDirectoryLength <= PEConstants.DirectoryImport) switch
            {
                true => false,
                _ => VirtualAddress switch
                {
                    0 => false,
                    _ => importOffset switch
                    {
                        -1 => false,
                        _ => importOffset < fslength
                    }
                }
            };
        }

        private static IMAGE_IMPORT_DESCRIPTOR GetImportDesc(BinaryReader reader)
        {
            return new()
            {
                OriginalFirstThunk = reader.ReadUInt32(),
                TimeDateStamp = reader.ReadUInt32(),
                ForwarderChain = reader.ReadUInt32(),
                Name = reader.ReadUInt32(),
                FirstThunk = reader.ReadUInt32()
            };
        }
        /// <summary>
        /// 解析标准导入表（IMAGE_DIRECTORY_ENTRY_IMPORT）。
        /// </summary>
        private static void ParseStandardImportTable(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            if (ParseStandardImportTableCheck(fs.Length, peInfo.OptionalHeader.DataDirectory.Length, peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryImport].VirtualAddress, PEParserUtils.RvaToOffset(peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryImport].VirtualAddress, peInfo.SectionHeaders)))
            {
                ParseStandardImportTable2(fs, reader, peInfo);
            }
        }

        private static void ParseStandardImportTable2(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            fs.Position = PEParserUtils.RvaToOffset(peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryImport].VirtualAddress, peInfo.SectionHeaders);
            ParseStandardImportTable3(fs, reader, peInfo);
        }

        private static void ParseStandardImportTable3(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            for (IMAGE_IMPORT_DESCRIPTOR importDesc; fs.Position + PEConstants.ImportDescriptorSize <= fs.Length &&
                (!IsTerminatorDescriptor(importDesc = GetImportDesc(reader)));)
            {
                ParseStandardImportTable3(fs, reader, peInfo, importDesc, ReadStringAtRva(fs, reader, peInfo, importDesc.Name));
            }
        }

        private static void ParseStandardImportTable3(FileStream fs, BinaryReader reader, PEInfo peInfo, IMAGE_IMPORT_DESCRIPTOR importDesc, string dllName)
        {
            AddDependencyIfMissing(peInfo, dllName);
            ParseImportFunctions(fs, reader, peInfo, importDesc, dllName);
        }



        private static bool IsTerminatorDescriptor(IMAGE_IMPORT_DESCRIPTOR d)
        {
            return d.OriginalFirstThunk switch
            {
                0 => d.TimeDateStamp switch
                {
                    0 => d.ForwarderChain switch
                    {
                        0 => d.Name switch
                        {
                            0 => d.FirstThunk switch
                            {
                                0 => true,
                                _ => false
                            },
                            _ => false
                        },
                        _ => false
                    },
                    _ => false
                },
                _ => false
            };
        }

        // 延迟加载导入表的全零终止符（与标准导入表 IsTerminatorDescriptor 对称）
        private static bool IsTerminatorDelayLoadDescriptor(IMAGE_DELAYLOAD_DESCRIPTOR d)
        {
            return d.Attributes == 0 && d.DllNameRVA == 0 && d.ModuleHandleRVA == 0 &&
                   d.ImportAddressTableRVA == 0 && d.ImportNameTableRVA == 0 &&
                   d.BoundImportAddressTableRVA == 0 && d.UnloadInformationTableRVA == 0 &&
                   d.TimeDateStamp == 0;
        }

        /// <summary>
        /// 将 RVA 解析为文件偏移并读取以 null 结尾的字符串（内部保存并恢复文件位置）。
        /// 偏移无效时返回 (-1, string.Empty)。
        /// </summary>
        private static string ReadStringAtRva(FileStream fs, BinaryReader reader, PEInfo peInfo, uint rva)
        {
            return ReadStringAtRvaCore(PEParserUtils.RvaToOffset(rva, peInfo.SectionHeaders), fs, reader);
        }

        private static string ReadStringAtRvaCore(long offset, FileStream fs, BinaryReader reader)
        {
            return ReadStringAtRvaCore2(offset, fs.Length, PEParserUtils.ReadAtOffset(fs, offset, string.Empty, () => PEParserUtils.ReadNullTerminatedString(reader)));
        }

        private static string ReadStringAtRvaCore2(long offset, long fsLength, string value)
        {
            return CheckStartOffset(offset, fsLength) switch
            {
                true => (value),
                _ => (string.Empty)
            };
        }

        private static uint GetRVA(IMAGE_IMPORT_DESCRIPTOR importDesc)
        {
            return importDesc.OriginalFirstThunk != 0 ? importDesc.OriginalFirstThunk : importDesc.FirstThunk;
        }

        /// <summary>
        /// 解析某个 DLL 的导入函数（基于 ILT/IAT thunk 表）。
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="importDesc">导入描述符</param>
        /// <param name="dllName">DLL名称</param>
        private static void ParseImportFunctions(FileStream fs, BinaryReader reader, PEInfo peInfo, IMAGE_IMPORT_DESCRIPTOR importDesc, string dllName)
        {
            try
            {
                ParseImportFunctionsCore(PEParserUtils.RvaToOffset(GetRVA(importDesc), peInfo.SectionHeaders), fs, reader, peInfo, dllName);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                Utils.AppLogger.Log($"导入函数解析错误: {ex.Message}");
            }
        }

        private static void ParseImportFunctionsCore(long thunkOffset, FileStream fs, BinaryReader reader, PEInfo peInfo, string dllName)
        {
            if (CheckStartOffset(thunkOffset, fs.Length))
            {
                peInfo.ImportFunctions.AddRange(PEParserUtils.ReadAtOffset(fs, thunkOffset, [],
                    () => WalkThunkTable(fs, reader, peInfo, thunkOffset, PEParserUtils.Is64Bit(peInfo.OptionalHeader), dllName, false)));
            }
        }


        /// <summary>
        /// 统一的 thunk 表遍历器：顺序读取每个 thunk（32 位 4 字节 / 64 位 8 字节），
        /// 区分序号导入与名称导入并解析为导入函数列表。标准导入与延迟加载导入共用此实现，
        /// 从而避免在两处重复 thunk 步长/序号位逻辑（曾导致延迟加载名表步长 off-by-one）。
        /// </summary>
        private static List<ImportFunctionInfo> WalkThunkTable(FileStream fs, BinaryReader reader, PEInfo peInfo, long thunkTableOffset, bool is64Bit, string dllName, bool isDelayLoaded)
        {
            List<ImportFunctionInfo> functions = [];
            int thunkSize = is64Bit ? 8 : 4;
            ulong ordinalFlag = is64Bit ? PEConstants.OrdinalFlag64 : PEConstants.OrdinalFlag32;

            fs.Position = thunkTableOffset;
            // 硬上限防御：畸形 thunk 表若无 0 终止符会一路读到 EOF，每个名称导入还触发 RvaToOffset + 字符串读取，
            // 让导入项膨胀到数十万项卡死解析线程（与导入描述符循环的 MaxImportDescriptors 同款防御）。
            int thunkCount = 0;
            while (fs.Position + thunkSize <= fs.Length && thunkCount < PEConstants.MaxThunksPerModule)
            {
                thunkCount++;
                ulong thunkValue = is64Bit ? reader.ReadUInt64() : reader.ReadUInt32();
                if (thunkValue == 0)
                {
                    break;
                }

                ImportFunctionInfo importFunc = new()
                {
                    DllName = dllName,
                    IsDelayLoaded = isDelayLoaded
                };

                if ((thunkValue & ordinalFlag) != 0)
                {
                    // 序号导入（最高位为 1）
                    importFunc.IsOrdinalImport = true;
                    importFunc.Ordinal = (int)(thunkValue & 0xFFFF);
                    importFunc.FunctionName = $"#{importFunc.Ordinal}";
                }
                else
                {
                    // 名称导入：通过 Hint/Name 表解析函数名（内部保存并恢复文件位置）
                    ImportByName(peInfo.SectionHeaders, thunkValue, fs, reader, importFunc);
                }

                functions.Add(importFunc);
            }

            return functions;
        }

        private static string GetSafeName(string dllName)
        {
            return string.IsNullOrEmpty(dllName) switch
            {
                true => string.Empty,
                _ => Utils.PathSafety.IsBareFileName(dllName) switch
                {
                    true => dllName,
                    _ => Path.GetFileName(dllName)
                }
            };
        }

        private static bool CheckSafeName(PEInfo peInfo, string safeName)
        {
            return !string.IsNullOrEmpty(safeName) && !peInfo.Dependencies.Exists(dep => dep.Name.Equals(safeName, StringComparison.OrdinalIgnoreCase));
        }

        private static void AddDependencyIfMissingCore(PEInfo peInfo, string safeName)
        {
            if (CheckSafeName(peInfo, safeName))
            {
                peInfo.Dependencies.Add(new DependencyInfo { Name = safeName });
            }
        }


        private static void AddDependencyIfMissing(PEInfo peInfo, string dllName)
        {
            AddDependencyIfMissingCore(peInfo, GetSafeName(dllName));
        }

        private static void ImportByName(List<IMAGE_SECTION_HEADER> sections, ulong thunkRva, FileStream fs, BinaryReader reader, ImportFunctionInfo importFunc)
        {
            (importFunc.FunctionName, importFunc.Ordinal, importFunc.IsOrdinalImport) = (thunkRva > uint.MaxValue) switch
            {
                true => ($"INVALID_RVA_{thunkRva:X}", 0, false),
                _ => ImportByName2(PEParserUtils.RvaToOffset((uint)thunkRva, sections, 2), thunkRva, fs, reader)
            };
        }

        private static (string, int, bool) ImportByName2(long nameOffset, ulong thunkRva, FileStream fs, BinaryReader reader)
        {
            return nameOffset switch
            {
                -1 => ($"INVALID_RVA_{thunkRva:X8}", 0, false),
                _ => (nameOffset >= fs.Length) switch
                {
                    true => ($"INVALID_RVA_{thunkRva:X8}", 0, false),
                    _ => ImportByName3(nameOffset, fs, reader)
                }
            };
        }

        private static (string, int, bool) ImportByName3(long nameOffset, FileStream fs, BinaryReader reader)
        {
            long savePos = fs.Position;
            try
            {
                return ImportByName3Core(nameOffset, fs, reader);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                return ($"READ_ERROR: {ex.Message}", 0, false);
            }
            finally
            {
                fs.Position = savePos;
            }
        }

        private static (string, int, bool) ImportByName3Core(long nameOffset, FileStream fs, BinaryReader reader)
        {
            // 检查是否有足够空间读取 hint 和名称（至少需要 2 字节的 hint）
            return (fs.Length - nameOffset) switch
            {
                <= 2 => ("NAME_TOO_SHORT", 0, false),
                _ => ImportByName4(nameOffset, fs, reader)
            };
        }

        private static (string, int, bool) ImportByName4(long nameOffset, FileStream fs, BinaryReader reader)
        {
            fs.Position = nameOffset;
            return ImportByName5(reader.ReadUInt16(), PEParserUtils.ReadNullTerminatedString(reader));
        }

        private static (string, int, bool) ImportByName5(ushort hint, string functionName)
        {
            return (string.IsNullOrEmpty(functionName) switch
            {
                false => functionName,
                _ => "EMPTY_NAME"
            }, hint, false);
        }


        private static bool CheckStartOffset(long startOffset, long fsLength)
        {
            return startOffset switch
            {
                -1 => false,
                _ => startOffset >= fsLength
            };
        }
        /// <summary>
        /// 解析延迟加载导入表（IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT）。
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <returns>延迟加载的导入函数列表</returns>
        private static List<ImportFunctionInfo> ParseDelayLoadImportTable(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            return (peInfo.OptionalHeader.DataDirectory.Length <= PEConstants.DirectoryDelayImport) switch
            {
                true => [],
                _ => peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryDelayImport].VirtualAddress switch
                {
                    0 => [],
                    _ => CheckStartOffset(PEParserUtils.RvaToOffset(peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryDelayImport].VirtualAddress, peInfo.SectionHeaders), fs.Length) switch
                    {
                        true => ParseDelayLoadImportTableCore(fs, reader, peInfo, PEParserUtils.RvaToOffset(peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryDelayImport].VirtualAddress, peInfo.SectionHeaders)),
                        _ => []
                    }
                }
            };
        }

        private static bool CheckPositionCore(long fsPosition, long fsLength)
        {
            return fsPosition + PEConstants.DelayLoadDescriptorSize <= fsLength;
        }

        private static List<ImportFunctionInfo> ParseDelayLoadImportTableCore(FileStream fs, BinaryReader reader, PEInfo peInfo, long startOffset)
        {
            return ParseDelayLoadImportTableCore2(fs, reader, peInfo, startOffset, []);
        }

        private static bool CheckPosition(long fsPosition, long fsLength, ref IMAGE_DELAYLOAD_DESCRIPTOR delayLoadDesc, BinaryReader reader)
        {
            return !CheckPositionCore(fsPosition, fsLength) && !IsTerminatorDelayLoadDescriptor(ReadDelayLoadDescriptor(reader, delayLoadDesc));
        }

        private static List<ImportFunctionInfo> ParseDelayLoadImportTableCore2(FileStream fs, BinaryReader reader, PEInfo peInfo, long startOffset, List<ImportFunctionInfo> delayLoadImportFunctions)
        {
            IMAGE_DELAYLOAD_DESCRIPTOR delayLoadDesc = new();
            for (fs.Position = startOffset; CheckPosition(fs.Position, fs.Length, ref delayLoadDesc, reader);)
            {
                ParseDelayLoadImportTableCore3(ReadStringAtRva(fs, reader, peInfo, delayLoadDesc.DllNameRVA), fs, reader, peInfo, delayLoadDesc, delayLoadImportFunctions);
            }

            return delayLoadImportFunctions;
        }

        private static void ParseDelayLoadImportTableCore3(string dllName, FileStream fs, BinaryReader reader, PEInfo peInfo, IMAGE_DELAYLOAD_DESCRIPTOR delayLoadDesc, List<ImportFunctionInfo> delayLoadImportFunctions)
        {
            AddDependencyIfMissing(peInfo, dllName);
            delayLoadImportFunctions.AddRange(
                ParseDelayLoadFunctions(fs, reader, peInfo, delayLoadDesc.ImportNameTableRVA, dllName));
        }



        /// <summary>
        /// 解析单个延迟加载描述符的导入名表（ImportNameTableRVA），返回其导入函数列表。
        /// </summary>
        private static List<ImportFunctionInfo> ParseDelayLoadFunctions(FileStream fs, BinaryReader reader, PEInfo peInfo, uint importNameTableRVA, string dllName)
        {
            return importNameTableRVA switch
            {
                0 => [],
                _ => ParseDelayLoadFunctions2(fs, reader, peInfo, dllName, PEParserUtils.RvaToOffset(importNameTableRVA, peInfo.SectionHeaders))
            };
        }

        private static List<ImportFunctionInfo> ParseDelayLoadFunctions2(FileStream fs, BinaryReader reader, PEInfo peInfo, string dllName, long nameTableOffset)
        {
            return nameTableOffset switch
            {
                -1 => [],
                _ when nameTableOffset >= fs.Length => [],
                _ => WalkThunkTable(fs, reader, peInfo, nameTableOffset, PEParserUtils.Is64Bit(peInfo.OptionalHeader), dllName, isDelayLoaded: true)
            };
        }

        private static IMAGE_DELAYLOAD_DESCRIPTOR ReadDelayLoadDescriptor(BinaryReader reader, IMAGE_DELAYLOAD_DESCRIPTOR delayLoadDesc)
        {
            (delayLoadDesc.Attributes, delayLoadDesc.DllNameRVA, delayLoadDesc.ModuleHandleRVA, delayLoadDesc.ImportNameTableRVA, delayLoadDesc.ImportNameTableRVA, delayLoadDesc.BoundImportAddressTableRVA, delayLoadDesc.UnloadInformationTableRVA, delayLoadDesc.TimeDateStamp) = (reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());
            return delayLoadDesc;

        }
    }
}
