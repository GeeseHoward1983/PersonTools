using System;
using System.IO;
using System.Text;

namespace MyTool
{
    /// <summary>
    /// PE资源解析器版本信息解析辅助模块
    /// 包含版本信息解析的辅助函数
    /// </summary>
    internal static class PEResourceParserVersionHelpers
    {
        /// <summary>
        /// 解析版本信息结构
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="size">数据大小</param>
        internal static void ParseVersionInfoStructure(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            try
            {
                long startPosition = fs.Position;

                // 读取VS_VERSIONINFO头
                if (fs.Position + 6 <= fs.Length)
                {
                    ushort wLength = reader.ReadUInt16();
                    ushort wValueLength = reader.ReadUInt16();
                    ushort wType = reader.ReadUInt16();

                    // 检查基本的有效性
                    if (wLength == 0)
                        return;

                    // 读取szKey (UNICODE字符串 "VS_VERSION_INFO")
                    string vsVersionInfoKey = PEResourceParserCore.ReadUnicodeStringWithMaxLength(reader, wLength);
                    
                    // 验证键名
                    if (!vsVersionInfoKey.Equals("VS_VERSION_INFO", StringComparison.OrdinalIgnoreCase))
                        return;

                    // 对齐到4字节边界
                    long alignedPosition = (fs.Position + 3) & ~3;

                    // 检查是否有足够的空间读取VS_FIXEDFILEINFO
                    // VS_FIXEDFILEINFO大小为52字节，但我们需要检查wValueLength是否有效
                    if (wValueLength >= 52 && alignedPosition + 52 <= fs.Length)
                    {
                        fs.Position = alignedPosition;

                        // 读取VS_FIXEDFILEINFO结构
                        var fixedFileInfo = new VS_FIXEDFILEINFO
                        {
                            dwSignature = reader.ReadUInt32(),
                            dwStrucVersion = reader.ReadUInt32(),
                            dwFileVersionMS = reader.ReadUInt32(),
                            dwFileVersionLS = reader.ReadUInt32(),
                            dwProductVersionMS = reader.ReadUInt32(),
                            dwProductVersionLS = reader.ReadUInt32(),
                            dwFileFlagsMask = reader.ReadUInt32(),
                            dwFileFlags = reader.ReadUInt32(),
                            dwFileOS = reader.ReadUInt32(),
                            dwFileType = reader.ReadUInt32(),
                            dwFileSubtype = reader.ReadUInt32(),
                            dwFileDateMS = reader.ReadUInt32(),
                            dwFileDateLS = reader.ReadUInt32()
                        };

                        // 验证签名
                        if (fixedFileInfo.dwSignature == 0xFEEF04BD) // VS_VERSIONINFO签名
                        {
                            uint fileVersionMajor = (fixedFileInfo.dwFileVersionMS >> 16) & 0xFFFF;
                            uint fileVersionMinor = fixedFileInfo.dwFileVersionMS & 0xFFFF;
                            uint fileVersionBuild = (fixedFileInfo.dwFileVersionLS >> 16) & 0xFFFF;
                            uint fileVersionRev = fixedFileInfo.dwFileVersionLS & 0xFFFF;
                            peInfo.AdditionalInfo.FileVersion = $"{fileVersionMajor}.{fileVersionMinor}.{fileVersionBuild}.{fileVersionRev}";

                            uint productVersionMajor = (fixedFileInfo.dwProductVersionMS >> 16) & 0xFFFF;
                            uint productVersionMinor = fixedFileInfo.dwProductVersionMS & 0xFFFF;
                            uint productVersionBuild = (fixedFileInfo.dwProductVersionLS >> 16) & 0xFFFF;
                            uint productVersionRev = fixedFileInfo.dwProductVersionLS & 0xFFFF;
                            peInfo.AdditionalInfo.ProductVersion = $"{productVersionMajor}.{productVersionMinor}.{productVersionBuild}.{productVersionRev}";
                        }
                        else
                        {
                            // 签名无效，记录日志
                            peInfo.AdditionalInfo.FileVersion = $"无效的版本信息签名: 0x{fixedFileInfo.dwSignature:X8}";
                        }

                        // 继续解析StringFileInfo和VarFileInfo部分
                        // 它们都紧跟在VS_FIXEDFILEINFO之后
                        long childrenStartPos = alignedPosition + 52; // 跳过FIXEDFILEINFO
                        childrenStartPos = (childrenStartPos + 3) & ~3; // 对齐到4字节边界

                        if (childrenStartPos < fs.Length && childrenStartPos < startPosition + wLength)
                        {
                            fs.Position = childrenStartPos;
                            ParseVersionChildren(fs, reader, peInfo, startPosition + wLength);
                        }
                    }
                    else
                    {
                        // wValueLength太小或没有足够的数据
                        peInfo.AdditionalInfo.FileVersion = $"版本信息数据不完整: wValueLength={wValueLength}, 需要>=52";
                    }
                }
            }
            catch (Exception ex)
            {
                peInfo.AdditionalInfo.FileVersion = $"版本信息结构解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析版本信息的子项（StringFileInfo和VarFileInfo）
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="endPosition">版本信息结构的结束位置</param>
        internal static void ParseVersionChildren(FileStream fs, BinaryReader reader, PEInfo peInfo, long endPosition)
        {
            try
            {
                // 循环解析所有子项，直到达到结束位置
                while (fs.Position < endPosition && fs.Position + 6 <= fs.Length)
                {
                    long childStartPos = fs.Position;
                    
                    // 读取子项头部
                    ushort wLength = reader.ReadUInt16();
                    ushort wValueLength = reader.ReadUInt16();
                    ushort wType = reader.ReadUInt16();
                    
                    if (wLength == 0)
                        break;
                    
                    // 读取键名
                    string key = PEResourceParserCore.ReadUnicodeStringWithMaxLength(reader, wLength);
                    
                    // 重置位置以便正确解析
                    fs.Position = childStartPos;
                    
                    // 根据键名决定如何处理
                    if (key.Equals("StringFileInfo", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseStringFileInfo(fs, reader, peInfo, endPosition);
                    }
                    else if (key.Equals("VarFileInfo", StringComparison.OrdinalIgnoreCase))
                    {
                        ParseVarFileInfo(fs, reader, peInfo, endPosition);
                    }
                    
                    // 移动到下一个子项
                    long nextChildPos = (childStartPos + wLength + 3) & ~3;
                    if (nextChildPos >= endPosition || nextChildPos < fs.Position)
                        break;
                    
                    fs.Position = nextChildPos;
                }
            }
            catch (Exception ex)
            {
                peInfo.AdditionalInfo.FileVersion += $"; 版本子项解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析StringFileInfo部分
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="endPosition">版本信息结构的结束位置</param>
        internal static void ParseStringFileInfo(FileStream fs, BinaryReader reader, PEInfo peInfo, long endPosition)
        {
            try
            {
                long startPosition = fs.Position;

                // 检查是否还有足够的数据
                if (fs.Position + 6 > fs.Length)
                    return;

                ushort wLength = reader.ReadUInt16();
                ushort wValueLength = reader.ReadUInt16();
                ushort wType = reader.ReadUInt16();

                // 读取szKey (UNICODE字符串 "StringFileInfo")
                string key = PEResourceParserCore.ReadUnicodeStringWithMaxLength(reader, wLength);

                if (key.Equals("StringFileInfo", StringComparison.OrdinalIgnoreCase))
                {
                    // 计算StringTable的位置
                    long keyLengthInBytes = (key.Length + 1) * 2; // Unicode字符串长度 + null终止符
                    long afterKeyPosition = startPosition + 6 + keyLengthInBytes; // 6是头部大小
                    long stringTablePosition = (afterKeyPosition + 3) & ~3; // 对齐到4字节边界

                    long stringFileInfoEndPosition = Math.Min(startPosition + wLength, endPosition);

                    if (stringTablePosition < fs.Length && stringTablePosition < stringFileInfoEndPosition)
                    {
                        fs.Position = stringTablePosition;
                        ParseStringTable(fs, reader, peInfo, stringFileInfoEndPosition);
                    }
                    
                    // 记录StringTable解析完成的位置，便于后续处理
                    peInfo.AdditionalInfo.StringTableParsed = true;
                    peInfo.AdditionalInfo.StringTableEndPosition = stringFileInfoEndPosition;
                }
                
                // 确保位置正确前进到下一个兄弟节点
                long nextPosition = (startPosition + wLength + 3) & ~3;
                if (nextPosition < endPosition && nextPosition > fs.Position)
                {
                    fs.Position = nextPosition;
                }
            }
            catch (Exception ex)
            {
                // 忽略StringFileInfo解析错误，但可以记录日志用于调试
                peInfo.AdditionalInfo.FileVersion += $"; StringFileInfo解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析VarFileInfo部分
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="endPosition">版本信息结构的结束位置</param>
        internal static void ParseVarFileInfo(FileStream fs, BinaryReader reader, PEInfo peInfo, long endPosition)
        {
            try
            {
                long startPosition = fs.Position;

                // 检查是否还有足够的数据
                if (fs.Position + 6 > fs.Length)
                    return;

                ushort wLength = reader.ReadUInt16();
                ushort wValueLength = reader.ReadUInt16();
                ushort wType = reader.ReadUInt16();

                // 读取szKey (UNICODE字符串 "VarFileInfo")
                string key = PEResourceParserCore.ReadUnicodeStringWithMaxLength(reader, wLength);

                if (key.Equals("VarFileInfo", StringComparison.OrdinalIgnoreCase))
                {
                    // 计算Var结构的位置
                    long keyLengthInBytes = (key.Length + 1) * 2; // Unicode字符串长度 + null终止符
                    long afterKeyPosition = startPosition + 6 + keyLengthInBytes; // 6是头部大小
                    long varPosition = (afterKeyPosition + 3) & ~3; // 对齐到4字节边界

                    long varFileInfoEndPosition = Math.Min(startPosition + wLength, endPosition);

                    if (varPosition < fs.Length && varPosition < varFileInfoEndPosition)
                    {
                        fs.Position = varPosition;
                        ParseVar(fs, reader, peInfo, varFileInfoEndPosition);
                    }
                }
                
                // 确保位置正确前进到下一个兄弟节点
                long nextPosition = (startPosition + wLength + 3) & ~3;
                if (nextPosition < endPosition && nextPosition > fs.Position)
                {
                    fs.Position = nextPosition;
                }
            }
            catch (Exception ex)
            {
                // 忽略VarFileInfo解析错误
                peInfo.AdditionalInfo.FileVersion += $"; VarFileInfo解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析Var部分（VarFileInfo的子项）
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="endPosition">Var块的结束位置</param>
        internal static void ParseVar(FileStream fs, BinaryReader reader, PEInfo peInfo, long endPosition)
        {
            try
            {
                long startPosition = fs.Position;

                // 检查是否还有足够的数据
                if (fs.Position + 6 > fs.Length)
                    return;

                ushort wLength = reader.ReadUInt16();
                ushort wValueLength = reader.ReadUInt16();
                ushort wType = reader.ReadUInt16();

                // 读取变量名（通常是"Translation"）
                string varName = PEResourceParserCore.ReadUnicodeStringWithMaxLength(reader, wLength);

                // 计算值的位置
                long keyLengthInBytes = (varName.Length + 1) * 2; // Unicode字符串长度 + null终止符
                long afterVarNamePosition = startPosition + 6 + keyLengthInBytes;
                long valuePosition = (afterVarNamePosition + 3) & ~3; // 对齐到4字节边界

                if (valuePosition >= fs.Length || valuePosition >= endPosition)
                    return;

                fs.Position = valuePosition;

                // 解析值（对于Translation，通常是语言和代码页的DWORD对）
                if (varName.Equals("Translation", StringComparison.OrdinalIgnoreCase) && wValueLength >= 4)
                {
                    // 读取语言和代码页信息
                    if (valuePosition + wValueLength <= fs.Length && valuePosition + wValueLength <= endPosition)
                    {
                        // 读取第一个DWORD对（语言ID和代码页）
                        uint translation = reader.ReadUInt32();
                        uint languageId = translation & 0xFFFF;
                        uint codePage = (translation >> 16) & 0xFFFF;
                        
                        // 存储翻译信息
                        peInfo.AdditionalInfo.TranslationInfo = $"语言ID: 0x{languageId:X4}, 代码页: {codePage}";
                    }
                }

                // 移动到下一个Var（如果有）
                long nextPosition = (startPosition + wLength + 3) & ~3;
                if (nextPosition < endPosition && nextPosition < fs.Length)
                {
                    fs.Position = nextPosition;
                    // 可以继续解析更多的Var项，但在实践中很少见
                }
            }
            catch (Exception ex)
            {
                // 忽略Var解析错误
                peInfo.AdditionalInfo.FileVersion += $"; Var解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析StringTable部分
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="endPosition">StringFileInfo块的结束位置</param>
        internal static void ParseStringTable(FileStream fs, BinaryReader reader, PEInfo peInfo, long endPosition)
        {
            try
            {
                long startPosition = fs.Position;

                // 检查是否还有足够的数据
                if (fs.Position + 6 > fs.Length)
                    return;

                ushort wLength = reader.ReadUInt16();
                ushort wValueLength = reader.ReadUInt16();
                ushort wType = reader.ReadUInt16();

                // 读取语言和代码页标识符（通常是8位十六进制字符串）
                string langId = PEResourceParserCore.ReadUnicodeStringWithMaxLength(reader, wLength); // 读取直到找到null终止符

                // 计算Strings的位置
                long keyLengthInBytes = (langId.Length + 1) * 2; // Unicode字符串长度 + null终止符
                long afterLangIdPosition = startPosition + 6 + keyLengthInBytes;
                long stringsPosition = (afterLangIdPosition + 3) & ~3; // 对齐到4字节边界

                if (stringsPosition >= fs.Length || stringsPosition >= endPosition)
                    return;

                fs.Position = stringsPosition;

                // 解析字符串对
                long tableEndPosition = Math.Min(startPosition + wLength, endPosition);

                while (fs.Position < tableEndPosition && fs.Position + 6 <= fs.Length)
                {
                    long stringStartPos = fs.Position;

                    // 先读取头部信息判断长度
                    if (fs.Position + 6 > fs.Length) break;
                    
                    ushort strLength = reader.ReadUInt16();
                    ushort strValueLength = reader.ReadUInt16();
                    ushort strType = reader.ReadUInt16();

                    if (strLength == 0)
                    {
                        fs.Position = stringStartPos;
                        break;
                    }

                    // 重新定位到开始位置继续解析
                    fs.Position = stringStartPos;
                    ParseStringPair(fs, reader, peInfo, tableEndPosition);
                }
            }
            catch (Exception ex)
            {
                // 忽略StringTable解析错误
                peInfo.AdditionalInfo.FileVersion += $"; StringTable解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析单个字符串对
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="endPosition">StringTable块的结束位置</param>
        internal static void ParseStringPair(FileStream fs, BinaryReader reader, PEInfo peInfo, long endPosition)
        {
            try
            {
                long startPosition = fs.Position;

                // 检查是否还有足够的数据
                if (fs.Position + 6 > fs.Length)
                    return;

                ushort wLength = reader.ReadUInt16();
                ushort wValueLength = reader.ReadUInt16();
                ushort wType = reader.ReadUInt16();

                if (wLength == 0 || startPosition + wLength > endPosition)
                    return;

                // 读取键名
                var keySb = new StringBuilder();
                char ch;
                while (fs.Position < fs.Length && fs.Position < endPosition && fs.Position < startPosition + wLength)
                {
                    if (fs.Position + 2 > fs.Length) break;
                    ch = (char)reader.ReadUInt16();
                    if (ch == '\0')
                        break;
                    keySb.Append(ch);
                }

                string keyValue = keySb.ToString();

                // 跳过可能的额外null字符并对齐到4字节边界
                long currentPosition = fs.Position;
                long valuePosition = (currentPosition + 3) & ~3;

                // 确保valuePosition不超过边界
                if (valuePosition >= endPosition || valuePosition >= fs.Length)
                    return;

                fs.Position = valuePosition;

                // 读取值
                var valueSb = new StringBuilder();
                long valueEndPosition = Math.Min(startPosition + wLength, endPosition);
                while (fs.Position < fs.Length && fs.Position < valueEndPosition)
                {
                    if (fs.Position + 2 > fs.Length) break;
                    ch = (char)reader.ReadUInt16();
                    if (ch == '\0')
                        break;
                    valueSb.Append(ch);
                }

                string value = valueSb.ToString();

                // 根据键名设置相应的属性
                switch (keyValue.ToLower())
                {
                    case "companyname":
                        peInfo.AdditionalInfo.CompanyName = value;
                        break;
                    case "filedescription":
                        peInfo.AdditionalInfo.FileDescription = value;
                        break;
                    case "fileversion":
                        if (string.IsNullOrEmpty(peInfo.AdditionalInfo.FileVersion) || !peInfo.AdditionalInfo.FileVersion.Contains('.'))
                            peInfo.AdditionalInfo.FileVersion = value;
                        break;
                    case "productname":
                        peInfo.AdditionalInfo.ProductName = value;
                        break;
                    case "productversion":
                        if (string.IsNullOrEmpty(peInfo.AdditionalInfo.ProductVersion) || !peInfo.AdditionalInfo.ProductVersion.Contains('.'))
                            peInfo.AdditionalInfo.ProductVersion = value;
                        break;
                    case "originalfilename":
                        peInfo.AdditionalInfo.OriginalFileName = value;
                        break;
                    case "internalname":
                        peInfo.AdditionalInfo.InternalName = value;
                        break;
                    case "legalcopyright":
                        peInfo.AdditionalInfo.LegalCopyright = value;
                        break;
                    case "legaltrademarks":
                        peInfo.AdditionalInfo.LegalTrademarks = value;
                        break;
                }

                // 移动到下一个字符串对
                fs.Position = (startPosition + wLength + 3) & ~3;

                // 确保不超出边界
                if (fs.Position > endPosition)
                    fs.Position = endPosition;
            }
            catch (Exception ex)
            {
                // 忽略单个字符串对解析错误
                peInfo.AdditionalInfo.FileVersion += $"; StringPair解析错误: {ex.Message}";
            }
        }
    }
}