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
            try
            {
                ParseStandardImportTable(fs, reader, peInfo);

                // 解析延迟加载导入表并合并到主列表
                peInfo.ImportFunctions.AddRange(ParseDelayLoadImportTable(fs, reader, peInfo));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                Console.WriteLine($"导入表解析错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析标准导入表（IMAGE_DIRECTORY_ENTRY_IMPORT）。
        /// </summary>
        private static void ParseStandardImportTable(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            if (peInfo.OptionalHeader.DataDirectory.Length <= PEConstants.DirectoryImport ||
                peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryImport].VirtualAddress == 0)
            {
                return;
            }

            uint importRVA = peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryImport].VirtualAddress;
            long importOffset = Utilities.RvaToOffset(importRVA, peInfo.SectionHeaders);
            if (importOffset == -1 || importOffset >= fs.Length)
            {
                return;
            }

            long originalPosition = fs.Position;
            try
            {
                fs.Position = importOffset;

                // 循环读取导入描述符直到遇到全零的描述符
                while (fs.Position + PEConstants.ImportDescriptorSize <= fs.Length)
                {
                    IMAGEIMPORTDESCRIPTOR importDesc = new()
                    {
                        OriginalFirstThunk = reader.ReadUInt32(),
                        TimeDateStamp = reader.ReadUInt32(),
                        ForwarderChain = reader.ReadUInt32(),
                        Name = reader.ReadUInt32(),
                        FirstThunk = reader.ReadUInt32()
                    };

                    if (IsTerminatorDescriptor(importDesc))
                    {
                        break;
                    }

                    // 解析 DLL 名称；名称 RVA 无法解析时跳过该描述符
                    (long nameOffset, string dllName) = ReadStringAtRva(fs, reader, peInfo, importDesc.Name);
                    if (nameOffset == -1)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(dllName))
                    {
                        AddDependencyIfMissing(peInfo, dllName);
                    }

                    ParseImportFunctions(fs, reader, peInfo, importDesc, dllName);
                }
            }
            finally
            {
                fs.Position = originalPosition;
            }
        }

        private static bool IsTerminatorDescriptor(IMAGEIMPORTDESCRIPTOR d)
        {
            return d.OriginalFirstThunk == 0 && d.TimeDateStamp == 0 && d.ForwarderChain == 0 &&
                   d.Name == 0 && d.FirstThunk == 0;
        }

        /// <summary>
        /// 将 RVA 解析为文件偏移并读取以 null 结尾的字符串（内部保存并恢复文件位置）。
        /// 偏移无效时返回 (-1, string.Empty)。
        /// </summary>
        private static (long offset, string value) ReadStringAtRva(FileStream fs, BinaryReader reader, PEInfo peInfo, uint rva)
        {
            long offset = Utilities.RvaToOffset(rva, peInfo.SectionHeaders);
            if (offset == -1 || offset >= fs.Length)
            {
                return (-1, string.Empty);
            }

            string value = Utilities.ReadAtOffset(fs, offset, string.Empty, () => Utilities.ReadNullTerminatedString(reader));
            return (offset, value);
        }

        /// <summary>
        /// 解析某个 DLL 的导入函数（基于 ILT/IAT thunk 表）。
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="importDesc">导入描述符</param>
        /// <param name="dllName">DLL名称</param>
        private static void ParseImportFunctions(FileStream fs, BinaryReader reader, PEInfo peInfo, IMAGEIMPORTDESCRIPTOR importDesc, string dllName)
        {
            try
            {
                // 优先使用 OriginalFirstThunk（ILT），否则回退到 FirstThunk（IAT）
                uint thunkRVA = importDesc.OriginalFirstThunk != 0 ? importDesc.OriginalFirstThunk : importDesc.FirstThunk;
                long thunkOffset = Utilities.RvaToOffset(thunkRVA, peInfo.SectionHeaders);
                if (thunkOffset == -1 || thunkOffset >= fs.Length)
                {
                    return;
                }

                bool is64Bit = Utilities.Is64Bit(peInfo.OptionalHeader);
                peInfo.ImportFunctions.AddRange(Utilities.ReadAtOffset(fs, thunkOffset, new List<ImportFunctionInfo>(),
                    () => WalkThunkTable(fs, reader, peInfo, thunkOffset, is64Bit, dllName, isDelayLoaded: false)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                Console.WriteLine($"导入函数解析错误: {ex.Message}");
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
            while (fs.Position + thunkSize <= fs.Length)
            {
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

        private static void AddDependencyIfMissing(PEInfo peInfo, string dllName)
        {
            if (!peInfo.Dependencies.Exists(dep => dep.Name.Equals(dllName, StringComparison.OrdinalIgnoreCase)))
            {
                peInfo.Dependencies.Add(new DependencyInfo { Name = dllName });
            }
        }

        private static void SetImportFunc(ImportFunctionInfo importFunc, string functionNm, int ordinal, bool isOrdinalImport)
        {
            importFunc.FunctionName = functionNm;
            importFunc.Ordinal = ordinal;
            importFunc.IsOrdinalImport = isOrdinalImport;
        }

        private static void ImportByName(List<IMAGESECTIONHEADER> sections, ulong thunkRva, FileStream fs, BinaryReader reader, ImportFunctionInfo importFunc)
        {
            long nameOffset = Utilities.RvaToOffset((uint)thunkRva, sections);
            if (nameOffset == -1 || nameOffset >= fs.Length)
            {
                SetImportFunc(importFunc, $"INVALID_RVA_{thunkRva:X8}", 0, false);
                return;
            }

            try
            {
                // 检查是否有足够空间读取 hint 和名称（至少需要 2 字节的 hint）
                if (fs.Length - nameOffset <= 2)
                {
                    SetImportFunc(importFunc, "NAME_TOO_SHORT", 0, false);
                    return;
                }

                long savePos = fs.Position;
                fs.Position = nameOffset;
                // 读取Hint字段（2字节）后读取函数名称
                ushort hint = reader.ReadUInt16();
                string functionName = Utilities.ReadNullTerminatedString(reader);
                fs.Position = savePos;

                SetImportFunc(importFunc, !string.IsNullOrEmpty(functionName) ? functionName : "EMPTY_NAME", hint, false);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                SetImportFunc(importFunc, $"READ_ERROR: {ex.Message}", 0, false);
            }
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
            List<ImportFunctionInfo> delayLoadImportFunctions = [];

            if (peInfo.OptionalHeader.DataDirectory.Length <= PEConstants.DirectoryDelayImport)
            {
                return delayLoadImportFunctions;
            }

            uint delayLoadImportRVA = peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryDelayImport].VirtualAddress;
            if (delayLoadImportRVA == 0)
            {
                return delayLoadImportFunctions;
            }

            long startOffset = Utilities.RvaToOffset(delayLoadImportRVA, peInfo.SectionHeaders);
            if (startOffset == -1 || startOffset >= fs.Length)
            {
                return delayLoadImportFunctions;
            }

            bool is64Bit = Utilities.Is64Bit(peInfo.OptionalHeader);
            int descriptorCount = 0;

            while (startOffset + ((long)descriptorCount + 1) * PEConstants.DelayLoadDescriptorSize <= fs.Length)
            {
                fs.Position = startOffset + (long)descriptorCount * PEConstants.DelayLoadDescriptorSize;
                IMAGEDELAYLOADDESCRIPTOR delayLoadDesc = ReadDelayLoadDescriptor(reader);
                descriptorCount++;

                // 解析 DLL 名称；名称 RVA 无效则结束扫描，名称为空则跳过该描述符
                (long nameOffset, string dllName) = ReadStringAtRva(fs, reader, peInfo, delayLoadDesc.DllNameRVA);
                if (nameOffset == -1)
                {
                    break;
                }

                if (string.IsNullOrEmpty(dllName))
                {
                    continue;
                }

                AddDependencyIfMissing(peInfo, dllName);

                // 使用 ImportNameTableRVA 解析延迟加载导入函数
                delayLoadImportFunctions.AddRange(
                    ParseDelayLoadFunctions(fs, reader, peInfo, delayLoadDesc.ImportNameTableRVA, is64Bit, dllName));
            }

            return delayLoadImportFunctions;
        }

        /// <summary>
        /// 解析单个延迟加载描述符的导入名表（ImportNameTableRVA），返回其导入函数列表。
        /// </summary>
        private static List<ImportFunctionInfo> ParseDelayLoadFunctions(FileStream fs, BinaryReader reader, PEInfo peInfo, uint importNameTableRVA, bool is64Bit, string dllName)
        {
            if (importNameTableRVA == 0)
            {
                return [];
            }

            long nameTableOffset = Utilities.RvaToOffset(importNameTableRVA, peInfo.SectionHeaders);
            if (nameTableOffset == -1 || nameTableOffset >= fs.Length)
            {
                return [];
            }

            return WalkThunkTable(fs, reader, peInfo, nameTableOffset, is64Bit, dllName, isDelayLoaded: true);
        }

        private static IMAGEDELAYLOADDESCRIPTOR ReadDelayLoadDescriptor(BinaryReader reader)
        {
            return new IMAGEDELAYLOADDESCRIPTOR
            {
                Attributes = reader.ReadUInt32(),
                DllNameRVA = reader.ReadUInt32(),
                ModuleHandleRVA = reader.ReadUInt32(),
                ImportAddressTableRVA = reader.ReadUInt32(),
                ImportNameTableRVA = reader.ReadUInt32(),
                BoundImportAddressTableRVA = reader.ReadUInt32(),
                UnloadInformationTableRVA = reader.ReadUInt32(),
                TimeDateStamp = reader.ReadUInt32()
            };
        }
    }
}
