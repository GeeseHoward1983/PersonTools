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
                PersonalTools.Utils.AppLogger.Log($"导入表解析错误: {ex.Message}");
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
            long importOffset = PEParserUtils.RvaToOffset(importRVA, peInfo.SectionHeaders);
            if (importOffset == -1 || importOffset >= fs.Length)
            {
                return;
            }

            long originalPosition = fs.Position;
            try
            {
                fs.Position = importOffset;

                // 循环读取导入描述符直到遇到全零的描述符。
                // 硬上限防御：满构造的大文件可让循环跑 fs.Length/20 次、每次再读字符串/解析函数，
                // 故对描述符数量设上限（远超真实 PE 的依赖 DLL 数），避免 UI 线程长时间卡死。
                int descriptorCount = 0;
                while (fs.Position + PEConstants.ImportDescriptorSize <= fs.Length
                    && descriptorCount < PEConstants.MaxImportDescriptors)
                {
                    descriptorCount++;
                    IMAGE_IMPORT_DESCRIPTOR importDesc = new()
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

                    // 全零终止符已在上方 IsTerminatorDescriptor 判过，到此描述符必非表尾。
                    // 其 Name RVA 指向 .bss/跨节时 RvaToOffset 返回 -1，仅说明这一项名称取不出，
                    // 不代表表已结束；用 continue 跳过本项继续扫描后续合法 DLL（循环有 MaxImportDescriptors
                    // 上限保护，不会无限循环），避免误当表尾 break 漏掉其后所有依赖。
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

        private static bool IsTerminatorDescriptor(IMAGE_IMPORT_DESCRIPTOR d)
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
            long offset = PEParserUtils.RvaToOffset(rva, peInfo.SectionHeaders);
            if (offset == -1 || offset >= fs.Length)
            {
                return (-1, string.Empty);
            }

            string value = PEParserUtils.ReadAtOffset(fs, offset, string.Empty, () => PEParserUtils.ReadNullTerminatedString(reader));
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
        private static void ParseImportFunctions(FileStream fs, BinaryReader reader, PEInfo peInfo, IMAGE_IMPORT_DESCRIPTOR importDesc, string dllName)
        {
            try
            {
                // 优先使用 OriginalFirstThunk（ILT），否则回退到 FirstThunk（IAT）
                uint thunkRVA = importDesc.OriginalFirstThunk != 0 ? importDesc.OriginalFirstThunk : importDesc.FirstThunk;
                long thunkOffset = PEParserUtils.RvaToOffset(thunkRVA, peInfo.SectionHeaders);
                if (thunkOffset == -1 || thunkOffset >= fs.Length)
                {
                    return;
                }

                bool is64Bit = PEParserUtils.Is64Bit(peInfo.OptionalHeader);
                peInfo.ImportFunctions.AddRange(PEParserUtils.ReadAtOffset(fs, thunkOffset, new List<ImportFunctionInfo>(),
                    () => WalkThunkTable(fs, reader, peInfo, thunkOffset, is64Bit, dllName, isDelayLoaded: false)));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                PersonalTools.Utils.AppLogger.Log($"导入函数解析错误: {ex.Message}");
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

        private static void AddDependencyIfMissing(PEInfo peInfo, string dllName)
        {
            // 纵深防御：DLL 名取自不可信导入表。已是裸文件名则原样记录；含路径成分时仅保留文件名部分，
            // 避免伪造名传播到 DependencyResolver 的路径拼接（与 DependencyResolver 的 IsBareFileName 入口校验形成双重防护）。
            string safeName = PersonalTools.Utils.PathSafety.IsBareFileName(dllName)
                ? dllName
                : Path.GetFileName(dllName);
            if (string.IsNullOrEmpty(safeName))
            {
                return;
            }

            if (!peInfo.Dependencies.Exists(dep => dep.Name.Equals(safeName, StringComparison.OrdinalIgnoreCase)))
            {
                peInfo.Dependencies.Add(new DependencyInfo { Name = safeName });
            }
        }

        private static void SetImportFunc(ImportFunctionInfo importFunc, string functionNm, int ordinal, bool isOrdinalImport)
        {
            importFunc.FunctionName = functionNm;
            importFunc.Ordinal = ordinal;
            importFunc.IsOrdinalImport = isOrdinalImport;
        }

        private static void ImportByName(List<IMAGE_SECTION_HEADER> sections, ulong thunkRva, FileStream fs, BinaryReader reader, ImportFunctionInfo importFunc)
        {
            // RVA 为 32 位概念；name import 的 thunk 高 32 位本应为 0，非零说明畸形数据，避免 (uint) 截断后碰巧落入有效节读出错误名称
            if (thunkRva > uint.MaxValue)
            {
                SetImportFunc(importFunc, $"INVALID_RVA_{thunkRva:X}", 0, false);
                return;
            }

            // requiredLength=2 至少覆盖 Hint 字段，保证 hint 与函数名起始落在同一节内，减少跨节误读
            long nameOffset = PEParserUtils.RvaToOffset((uint)thunkRva, sections, 2);
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
                string functionName = PEParserUtils.ReadNullTerminatedString(reader);
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

            long startOffset = PEParserUtils.RvaToOffset(delayLoadImportRVA, peInfo.SectionHeaders);
            if (startOffset == -1 || startOffset >= fs.Length)
            {
                return delayLoadImportFunctions;
            }

            bool is64Bit = PEParserUtils.Is64Bit(peInfo.OptionalHeader);
            int descriptorCount = 0;

            // 硬上限防御：与标准导入表的 MaxImportDescriptors 对称。畸形大文件可让每个描述符的 DllNameRVA
            // 都解析为有效名称，使本循环跑到 fs.Length/32 次并对每个再遍历 thunk 表，膨胀导入项卡死解析线程。
            while (descriptorCount < PEConstants.MaxImportDescriptors
                && startOffset + ((long)descriptorCount + 1) * PEConstants.DelayLoadDescriptorSize <= fs.Length)
            {
                fs.Position = startOffset + (long)descriptorCount * PEConstants.DelayLoadDescriptorSize;
                IMAGE_DELAYLOAD_DESCRIPTOR delayLoadDesc = ReadDelayLoadDescriptor(reader);
                descriptorCount++;

                // 解析 DLL 名称；Name RVA 解析失败仅说明这一项名称取不出（如指向 .bss/跨节），
                // 描述符循环本身按 descriptorCount/DelayLoadDescriptorSize 步进且有 MaxImportDescriptors
                // 上限，不依赖名称作表尾，故用 continue 跳过本项继续扫描后续合法 DLL（与标准导入表一致），
                // 避免误当表尾 break 漏掉其后所有延迟加载依赖。名称为空也跳过该描述符。
                (long nameOffset, string dllName) = ReadStringAtRva(fs, reader, peInfo, delayLoadDesc.DllNameRVA);
                if (nameOffset == -1)
                {
                    continue;
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

            long nameTableOffset = PEParserUtils.RvaToOffset(importNameTableRVA, peInfo.SectionHeaders);
            if (nameTableOffset == -1 || nameTableOffset >= fs.Length)
            {
                return [];
            }

            return WalkThunkTable(fs, reader, peInfo, nameTableOffset, is64Bit, dllName, isDelayLoaded: true);
        }

        private static IMAGE_DELAYLOAD_DESCRIPTOR ReadDelayLoadDescriptor(BinaryReader reader)
        {
            return new IMAGE_DELAYLOAD_DESCRIPTOR
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
