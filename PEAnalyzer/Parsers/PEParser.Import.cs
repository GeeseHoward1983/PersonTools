using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Parsers;
using PersonalTools.PEAnalyzer.Resources;
using System.IO;
using System.Reflection.PortableExecutable;

namespace PersonalTools
{
    /// <summary>
    /// PE文件导入表解析器
    /// 专门负责解析PE文件的导入表信息
    /// </summary>
    internal static partial class PEParser
    {
        /// <summary>
        /// 解析导入表
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        internal static void ParseImportTable(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            try
            {
                // 导入表在数据目录的第2项 (IMAGE_DIRECTORY_ENTRY_IMPORT)
                const int IMPORT_DIRECTORY_INDEX = 1;

                if (peInfo.OptionalHeader.DataDirectory.Length > IMPORT_DIRECTORY_INDEX &&
                    peInfo.OptionalHeader.DataDirectory[IMPORT_DIRECTORY_INDEX].VirtualAddress != 0)
                {
                    uint importRVA = peInfo.OptionalHeader.DataDirectory[IMPORT_DIRECTORY_INDEX].VirtualAddress;
                    long importOffset = PEResourceParserCore.RvaToOffset(importRVA, peInfo.SectionHeaders);

                    if (importOffset != -1 && importOffset < fs.Length)
                    {
                        long originalPosition = fs.Position;
                        fs.Position = importOffset;

                        // 循环读取导入描述符直到遇到全零的描述符
                        while (fs.Position + 20 <= fs.Length) // IMAGE_IMPORT_DESCRIPTOR大小为20字节
                        {
                            IMAGEIMPORTDESCRIPTOR importDesc = new()
                            {
                                OriginalFirstThunk = reader.ReadUInt32(),
                                TimeDateStamp = reader.ReadUInt32(),
                                ForwarderChain = reader.ReadUInt32(),
                                Name = reader.ReadUInt32(),
                                FirstThunk = reader.ReadUInt32()
                            };

                            // 检查是否是终止描述符（全零）
                            if (importDesc.OriginalFirstThunk == 0 &&
                                importDesc.TimeDateStamp == 0 &&
                                importDesc.ForwarderChain == 0 &&
                                importDesc.Name == 0 &&
                                importDesc.FirstThunk == 0)
                            {
                                break;
                            }

                            // 获取DLL名称
                            long nameOffset = PEResourceParserCore.RvaToOffset(importDesc.Name, peInfo.SectionHeaders);
                            if (nameOffset != -1 && nameOffset < fs.Length)
                            {
                                long tempPosition = fs.Position;
                                fs.Position = nameOffset;

                                string dllName = Utilties.ReadNullTerminatedString(reader);
                                fs.Position = tempPosition;

                                // 添加到依赖信息列表
                                if (!string.IsNullOrEmpty(dllName))
                                {
                                    // 检查是否已存在相同的依赖项
                                    bool alreadyExists = false;
                                    foreach (DependencyInfo dep in peInfo.Dependencies)
                                    {
                                        if (dep.Name.Equals(dllName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            alreadyExists = true;
                                            break;
                                        }
                                    }

                                    // 如果不存在，则添加新的依赖项
                                    if (!alreadyExists)
                                    {
                                        DependencyInfo dependency = new() { Name = dllName };
                                        peInfo.Dependencies.Add(dependency);
                                    }
                                }

                                // 解析导入函数
                                ParseImportFunctions(fs, reader, peInfo, importDesc, dllName);
                            }
                        }

                        fs.Position = originalPosition;
                    }
                }

                // 解析延迟加载导入表
                List<ImportFunctionInfo> delayLoadedImports = ParseDelayLoadImportTable(fs, reader, peInfo);
                // 将延迟加载的导入函数添加到主列表中
                peInfo.ImportFunctions.AddRange(delayLoadedImports);
            }
            catch (IOException ex)
            {
                // 忽略导入表解析错误
                Console.WriteLine($"导入表解析错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                // 忽略导入表解析错误
                Console.WriteLine($"导入表解析错误: {ex.Message}");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // 忽略导入表解析错误
                Console.WriteLine($"导入表解析错误: {ex.Message}");
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 解析导入函数
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="importDesc">导入描述符</param>
        /// <param name="dllName">DLL名称</param>
        internal static void ParseImportFunctions(FileStream fs, BinaryReader reader, PEInfo peInfo, IMAGEIMPORTDESCRIPTOR importDesc, string dllName)
        {
            try
            {
                // 使用OriginalFirstThunk或FirstThunk来获取导入地址表
                uint thunkRVA = importDesc.OriginalFirstThunk != 0 ? importDesc.OriginalFirstThunk : importDesc.FirstThunk;
                long thunkOffset = PEResourceParserCore.RvaToOffset(thunkRVA, peInfo.SectionHeaders);

                if (thunkOffset != -1 && thunkOffset < fs.Length)
                {
                    long originalPosition = fs.Position;
                    fs.Position = thunkOffset;

                    // 判断是32位还是64位
                    bool is64Bit = Utilties.Is64Bit(peInfo.OptionalHeader);
                    int thunkSize = is64Bit ? 8 : 4;

                    while (fs.Position + thunkSize <= fs.Length)
                    {
                        ulong thunkValue = is64Bit ? reader.ReadUInt64() : reader.ReadUInt32();

                        // 检查是否是终止项
                        if (thunkValue == 0)
                        {
                            break;
                        }

                        ImportFunctionInfo importFunc = new()
                        {
                            DllName = dllName
                        };

                        // 检查是否是序号导入（最高位为1）
                        if ((thunkValue & (is64Bit ? 0x8000000000000000UL : 0x80000000U)) != 0)
                        {
                            importFunc.IsOrdinalImport = true;
                            importFunc.Ordinal = (int)(thunkValue & (is64Bit ? 0x7FFFFFFFUL : 0x7FFFFFFFU));
                            importFunc.FunctionName = $"#{importFunc.Ordinal}";
                        }
                        else
                        {
                            // 通过Hint/Name表获取函数名称
                            uint nameRVA = (uint)thunkValue;
                            long nameOffset = PEResourceParserCore.RvaToOffset(nameRVA, peInfo.SectionHeaders);

                            if (nameOffset != -1 && nameOffset < fs.Length)
                            {
                                long tempPosition = fs.Position;
                                fs.Position = nameOffset;

                                // 读取Hint字段（2字节），Hint就是函数的序号
                                ushort hint = reader.ReadUInt16();
                                importFunc.Ordinal = hint;

                                importFunc.FunctionName = Utilties.ReadNullTerminatedString(reader);
                                fs.Position = tempPosition;
                            }
                            else
                            {
                                importFunc.FunctionName = $"UNKNOWN_FUNC_0x{nameRVA:X8}";
                            }
                        }

                        peInfo.ImportFunctions.Add(importFunc);
                    }

                    fs.Position = originalPosition;
                }
            }
            catch (IOException ex)
            {
                // 忽略导入函数解析错误
                Console.WriteLine($"导入函数解析错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                // 忽略导入函数解析错误
                Console.WriteLine($"导入函数解析错误: {ex.Message}");
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // 忽略导入函数解析错误
                Console.WriteLine($"导入函数解析错误: {ex.Message}");
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
            }
        }

        private static void ImportByName(List<IMAGESECTIONHEADER> sections, ulong thunkRva, FileStream fs, BinaryReader reader, ImportFunctionInfo importFunc)
        {
            long nameOffset = PEResourceParserCore.RvaToOffset((uint)thunkRva, sections);
            if (nameOffset != -1 && nameOffset < fs.Length)
            {
                try
                {
                    // 检查是否有足够空间读取 hint 和名称
                    long remainingLength = fs.Length - nameOffset;
                    if (remainingLength > 2) // 至少需要2字节的hint
                    {
                        long savePos = fs.Position; // 保存当前位置
                        fs.Position = nameOffset;
                        // 读取Hint字段（2字节）
                        ushort hint = reader.ReadUInt16();
                        // 读取函数名称
                        string functionName = Utilties.ReadNullTerminatedString(reader);
                        fs.Position = savePos; // 恢复位置

                        importFunc.FunctionName = !string.IsNullOrEmpty(functionName) ? functionName : $"EMPTY_NAME";
                        // 使用Hint字段作为序号
                        importFunc.Ordinal = hint;
                        importFunc.IsOrdinalImport = false;
                    }
                    else
                    {
                        importFunc.FunctionName = $"NAME_TOO_SHORT";
                        importFunc.Ordinal = 0;
                        importFunc.IsOrdinalImport = false;
                    }
                }
                catch (IOException ex)
                {
                    importFunc.FunctionName = $"READ_ERROR: {ex.Message}";
                    importFunc.Ordinal = 0;
                    importFunc.IsOrdinalImport = false;
                }
                catch (UnauthorizedAccessException ex)
                {
                    importFunc.FunctionName = $"READ_ERROR: {ex.Message}";
                    importFunc.Ordinal = 0;
                    importFunc.IsOrdinalImport = false;
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    importFunc.FunctionName = $"READ_ERROR: {ex.Message}";
                    importFunc.Ordinal = 0;
                    importFunc.IsOrdinalImport = false;
                }
            }
            else
            {
                importFunc.FunctionName = $"INVALID_RVA_{thunkRva:X8}";
                importFunc.Ordinal = 0;
                importFunc.IsOrdinalImport = false;
            }
        }

        /// <summary>
        /// 解析延迟加载导入表
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <returns>延迟加载的导入函数列表</returns>
        private static List<ImportFunctionInfo> ParseDelayLoadImportTable(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            List<ImportFunctionInfo> delayLoadImportFunctions = [];

            // 延迟加载导入表通常在数据目录的第14项（索引为13）
            // IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT = 13 (从0开始计数为索引13)
            const int DELAY_LOAD_IMPORT_INDEX = 13;
            bool is64Bit = Utilties.Is64Bit(peInfo.OptionalHeader);
            bool is32Bit = Utilties.Is32Bit(peInfo.OptionalHeader);
            if (peInfo.OptionalHeader.DataDirectory.Length > DELAY_LOAD_IMPORT_INDEX &&
                peInfo.OptionalHeader.DataDirectory[DELAY_LOAD_IMPORT_INDEX].VirtualAddress != 0)
            {
                uint delayLoadImportRVA = peInfo.OptionalHeader.DataDirectory[DELAY_LOAD_IMPORT_INDEX].VirtualAddress;
                long delayLoadImportOffset = PEResourceParserCore.RvaToOffset(delayLoadImportRVA, peInfo.SectionHeaders);

                if (delayLoadImportOffset != -1 && delayLoadImportOffset < fs.Length)
                {
                    long delayLoadImportStartOffset = delayLoadImportOffset; // 记录起始位置
                    int descriptorCount = 0;

                    while (delayLoadImportStartOffset + (descriptorCount + 1) * 32 <= fs.Length)
                    {
                        // 定位到当前描述符位置
                        fs.Position = delayLoadImportStartOffset + descriptorCount * 32;

                        IMAGEDELAYLOADDESCRIPTOR delayLoadDesc = new()
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

                        descriptorCount++;

                        // 获取DLL名称
                        string dllName = "";
                        if (delayLoadDesc.DllNameRVA != 0)
                        {
                            long nameOffset = PEResourceParserCore.RvaToOffset(delayLoadDesc.DllNameRVA, peInfo.SectionHeaders);
                            if (nameOffset != -1 && nameOffset < fs.Length)
                            {
                                long savePos = fs.Position;
                                fs.Position = nameOffset;
                                dllName = Utilties.ReadNullTerminatedString(reader);
                                fs.Position = savePos; // 恢复位置
                            }
                        }
                        else
                        {
                            break; // 如果DLL名称RVA为0，说明没有更多的描述符了
                        }

                        // 如果DLL名称为空，跳过这个延迟加载描述符
                        if (string.IsNullOrEmpty(dllName))
                        {
                            continue;
                        }

                        // 添加到依赖信息列表（如果尚未存在）
                        foreach (DependencyInfo dep in peInfo.Dependencies)
                        {
                            if (dep.Name.Equals(dllName, StringComparison.OrdinalIgnoreCase))
                            {
                                break;
                            }
                            else
                            {
                                DependencyInfo dependency = new() { Name = dllName };
                                peInfo.Dependencies.Add(dependency);
                            }
                        }

                        // 解析延迟加载导入函数 - 使用ImportNameTableRVA
                        if (delayLoadDesc.ImportNameTableRVA != 0)
                        {
                            long nameTableOffset = PEResourceParserCore.RvaToOffset(delayLoadDesc.ImportNameTableRVA, peInfo.SectionHeaders);
                            if (nameTableOffset != -1 && nameTableOffset < fs.Length)
                            {
                                long nameTableStartPos = nameTableOffset;
                                fs.Position = nameTableOffset;
                                int thunkCount = 0;

                                while (fs.Position + (is32Bit ? 4 : 8) <= fs.Length)
                                {
                                    ulong thunkRva = (is32Bit) ?
                                        reader.ReadUInt32() : reader.ReadUInt64();

                                    thunkCount++;

                                    if (thunkRva == 0)
                                    {
                                        break;
                                    }

                                    ImportFunctionInfo importFunc = new()
                                    {
                                        DllName = dllName,
                                        IsDelayLoaded = true  // 标记为延迟加载
                                    };

                                    if ((is32Bit && (thunkRva & 0x80000000) != 0) ||
                                        (is64Bit && (thunkRva & 0x8000000000000000) != 0))
                                    {
                                        // 按序号导入
                                        importFunc.Ordinal = (int)(thunkRva & 0xFFFF);
                                        importFunc.FunctionName = $"#{importFunc.Ordinal}";
                                        importFunc.IsOrdinalImport = true;
                                    }
                                    else
                                    {
                                        // 按名称导入
                                        ImportByName(peInfo.SectionHeaders, thunkRva, fs, reader, importFunc);
                                    }

                                    delayLoadImportFunctions.Add(importFunc);

                                    // 恢复到name table的下一个位置
                                    fs.Position = nameTableStartPos + (peInfo.OptionalHeader.Magic == 0x10b ? 4 : 8) * (thunkCount + 1);
                                }
                            }
                        }
                    }
                }
            }

            return delayLoadImportFunctions;
        }
    }
}