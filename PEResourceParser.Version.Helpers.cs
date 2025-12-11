using System;
using System.Globalization;
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
                        
                        // 存储翻译信息（转换为可读格式）
                        peInfo.AdditionalInfo.TranslationInfo = GetReadableTranslationInfo(languageId, codePage);
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
        /// 将语言ID和代码页转换为可读的文本信息
        /// </summary>
        /// <param name="languageId">语言ID</param>
        /// <param name="codePage">代码页</param>
        /// <returns>可读的翻译信息</returns>
        private static string GetReadableTranslationInfo(uint languageId, uint codePage)
        {
            // 获取语言名称
            string languageName = GetLanguageName(languageId);
            
            // 获取代码页名称
            string codePageName = GetCodePageName(codePage);
            
            // 返回格式化的字符串
            return $"{languageName} (0x{languageId:X4}), {codePageName} ({codePage})";
        }

        /// <summary>
        /// 根据语言ID获取语言名称
        /// </summary>
        /// <param name="languageId">语言ID</param>
        /// <returns>语言名称</returns>
        private static string GetLanguageName(uint languageId)
        {
            // 检查系统语言环境
            string cultureName = CultureInfo.CurrentCulture.Name;
            bool isSimplifiedChinese = cultureName.Equals("zh-CN", StringComparison.OrdinalIgnoreCase);
            bool isTraditionalChinese = cultureName.Equals("zh-TW", StringComparison.OrdinalIgnoreCase) || 
                                       cultureName.Equals("zh-HK", StringComparison.OrdinalIgnoreCase) ||
                                       cultureName.Equals("zh-MO", StringComparison.OrdinalIgnoreCase);

            // 常见的语言ID映射
            switch (languageId)
            {
                case 0x0400: return isTraditionalChinese ? "進程默認語言" : (isSimplifiedChinese ? "进程默认语言" : "Process Default Language");
                case 0x0401: return isTraditionalChinese ? "阿拉伯語（沙特阿拉伯）" : (isSimplifiedChinese ? "阿拉伯语（沙特阿拉伯）" : "Arabic (Saudi Arabia)");
                case 0x0402: return isTraditionalChinese ? "保加利亞語" : (isSimplifiedChinese ? "保加利亚语" : "Bulgarian");
                case 0x0403: return isTraditionalChinese ? "加泰羅尼亞語" : (isSimplifiedChinese ? "加泰罗尼亚语" : "Catalan");
                case 0x0404: return isTraditionalChinese ? "中文（繁體）" : (isSimplifiedChinese ? "中文（繁体）" : "Chinese (Traditional)");
                case 0x0405: return isTraditionalChinese ? "捷克語" : (isSimplifiedChinese ? "捷克语" : "Czech");
                case 0x0406: return isTraditionalChinese ? "丹麥語" : (isSimplifiedChinese ? "丹麦语" : "Danish");
                case 0x0407: return isTraditionalChinese ? "德語" : (isSimplifiedChinese ? "德语" : "German");
                case 0x0408: return isTraditionalChinese ? "希臘語" : (isSimplifiedChinese ? "希腊语" : "Greek");
                case 0x0409: return isTraditionalChinese ? "英語（美國）" : (isSimplifiedChinese ? "英语（美国）" : "English (United States)");
                case 0x040A: return isTraditionalChinese ? "西班牙語（傳統排序）" : (isSimplifiedChinese ? "西班牙语（传统排序）" : "Spanish (Traditional Sort)");
                case 0x040B: return isTraditionalChinese ? "芬蘭語" : (isSimplifiedChinese ? "芬兰语" : "Finnish");
                case 0x040C: return isTraditionalChinese ? "法語（標準）" : (isSimplifiedChinese ? "法语（标准）" : "French (Standard)");
                case 0x040D: return isTraditionalChinese ? "希伯來語" : (isSimplifiedChinese ? "希伯来语" : "Hebrew");
                case 0x040E: return isTraditionalChinese ? "匈牙利語" : (isSimplifiedChinese ? "匈牙利语" : "Hungarian");
                case 0x040F: return isTraditionalChinese ? "冰島語" : (isSimplifiedChinese ? "冰岛语" : "Icelandic");
                case 0x0410: return isTraditionalChinese ? "意大利語" : (isSimplifiedChinese ? "意大利语" : "Italian");
                case 0x0411: return isTraditionalChinese ? "日語" : (isSimplifiedChinese ? "日语" : "Japanese");
                case 0x0412: return isTraditionalChinese ? "韓語" : (isSimplifiedChinese ? "韩语" : "Korean");
                case 0x0413: return isTraditionalChinese ? "荷蘭語" : (isSimplifiedChinese ? "荷兰语" : "Dutch");
                case 0x0414: return isTraditionalChinese ? "挪威語（博克馬爾）" : (isSimplifiedChinese ? "挪威语（博克马尔）" : "Norwegian (Bokmal)");
                case 0x0415: return isTraditionalChinese ? "波蘭語" : (isSimplifiedChinese ? "波兰语" : "Polish");
                case 0x0416: return isTraditionalChinese ? "葡萄牙語（巴西）" : (isSimplifiedChinese ? "葡萄牙语（巴西）" : "Portuguese (Brazilian)");
                case 0x0417: return isTraditionalChinese ? "羅曼什語" : (isSimplifiedChinese ? "罗曼什语" : "Romansh");
                case 0x0418: return isTraditionalChinese ? "羅馬尼亞語" : (isSimplifiedChinese ? "罗马尼亚语" : "Romanian");
                case 0x0419: return isTraditionalChinese ? "俄語" : (isSimplifiedChinese ? "俄语" : "Russian");
                case 0x041A: return isTraditionalChinese ? "克羅地亞語" : (isSimplifiedChinese ? "克罗地亚语" : "Croatian");
                case 0x041B: return isTraditionalChinese ? "斯洛伐克語" : (isSimplifiedChinese ? "斯洛伐克语" : "Slovak");
                case 0x041C: return isTraditionalChinese ? "阿爾巴尼亞語" : (isSimplifiedChinese ? "阿尔巴尼亚语" : "Albanian");
                case 0x041D: return isTraditionalChinese ? "瑞典語" : (isSimplifiedChinese ? "瑞典语" : "Swedish");
                case 0x041E: return isTraditionalChinese ? "泰語" : (isSimplifiedChinese ? "泰语" : "Thai");
                case 0x041F: return isTraditionalChinese ? "土耳其語" : (isSimplifiedChinese ? "土耳其语" : "Turkish");
                case 0x0420: return isTraditionalChinese ? "烏爾都語" : (isSimplifiedChinese ? "乌尔都语" : "Urdu");
                case 0x0421: return isTraditionalChinese ? "印度尼西亞語" : (isSimplifiedChinese ? "印度尼西亚语" : "Indonesian");
                case 0x0422: return isTraditionalChinese ? "烏克蘭語" : (isSimplifiedChinese ? "乌克兰语" : "Ukrainian");
                case 0x0423: return isTraditionalChinese ? "白俄羅斯語" : (isSimplifiedChinese ? "白俄罗斯语" : "Belarusian");
                case 0x0424: return isTraditionalChinese ? "斯洛文尼亞語" : (isSimplifiedChinese ? "斯洛文尼亚语" : "Slovenian");
                case 0x0425: return isTraditionalChinese ? "愛沙尼亞語" : (isSimplifiedChinese ? "爱沙尼亚语" : "Estonian");
                case 0x0426: return isTraditionalChinese ? "拉脫維亞語" : (isSimplifiedChinese ? "拉脱维亚语" : "Latvian");
                case 0x0427: return isTraditionalChinese ? "立陶宛語" : (isSimplifiedChinese ? "立陶宛语" : "Lithuanian");
                case 0x0428: return isTraditionalChinese ? "塔吉克語" : (isSimplifiedChinese ? "塔吉克语" : "Tajik");
                case 0x0429: return isTraditionalChinese ? "波斯語（法爾斯語）" : (isSimplifiedChinese ? "波斯语（法尔斯语）" : "Persian (Farsi)");
                case 0x042A: return isTraditionalChinese ? "越南語" : (isSimplifiedChinese ? "越南语" : "Vietnamese");
                case 0x042B: return isTraditionalChinese ? "亞美尼亞語" : (isSimplifiedChinese ? "亚美尼亚语" : "Armenian");
                case 0x042C: return isTraditionalChinese ? "阿塞拜疆語（拉丁文）" : (isSimplifiedChinese ? "阿塞拜疆语（拉丁文）" : "Azerbaijani (Latin)");
                case 0x042D: return isTraditionalChinese ? "巴斯克語" : (isSimplifiedChinese ? "巴斯克语" : "Basque");
                case 0x042E: return isTraditionalChinese ? "上索布語" : (isSimplifiedChinese ? "上索布语" : "Upper Sorbian");
                case 0x042F: return isTraditionalChinese ? "馬其頓語" : (isSimplifiedChinese ? "马其顿语" : "Macedonian");
                case 0x0432: return isTraditionalChinese ? "茨瓦納語" : (isSimplifiedChinese ? "茨瓦纳语" : "Setswana");
                case 0x0436: return isTraditionalChinese ? "南非荷蘭語" : (isSimplifiedChinese ? "南非荷兰语" : "Afrikaans");
                case 0x0437: return isTraditionalChinese ? "格魯吉亞語" : (isSimplifiedChinese ? "格鲁吉亚语" : "Georgian");
                case 0x0438: return isTraditionalChinese ? "法羅語" : (isSimplifiedChinese ? "法罗语" : "Faroese");
                case 0x0439: return isTraditionalChinese ? "印地語" : (isSimplifiedChinese ? "印地语" : "Hindi");
                case 0x043A: return isTraditionalChinese ? "馬耳他語" : (isSimplifiedChinese ? "马耳他语" : "Maltese");
                case 0x043B: return isTraditionalChinese ? "薩米語（北部）" : (isSimplifiedChinese ? "萨米语（北部）" : "Sami (Northern)");
                case 0x043E: return isTraditionalChinese ? "馬來語" : (isSimplifiedChinese ? "马来语" : "Malay");
                case 0x043F: return isTraditionalChinese ? "哈薩克語" : (isSimplifiedChinese ? "哈萨克语" : "Kazakh");
                case 0x0440: return isTraditionalChinese ? "吉爾吉斯語" : (isSimplifiedChinese ? "吉尔吉斯语" : "Kyrgyz");
                case 0x0441: return isTraditionalChinese ? "斯瓦希里語" : (isSimplifiedChinese ? "斯瓦希里语" : "Swahili");
                case 0x0442: return isTraditionalChinese ? "土庫曼語" : (isSimplifiedChinese ? "土库曼语" : "Turkmen");
                case 0x0443: return isTraditionalChinese ? "烏茲別克語（拉丁文）" : (isSimplifiedChinese ? "乌兹别克语（拉丁文）" : "Uzbek (Latin)");
                case 0x0444: return isTraditionalChinese ? "韃靼語" : (isSimplifiedChinese ? "鞑靼语" : "Tatar");
                case 0x0445: return isTraditionalChinese ? "孟加拉語" : (isSimplifiedChinese ? "孟加拉语" : "Bengali");
                case 0x0446: return isTraditionalChinese ? "旁遮普語" : (isSimplifiedChinese ? "旁遮普语" : "Punjabi");
                case 0x0447: return isTraditionalChinese ? "古吉拉特語" : (isSimplifiedChinese ? "古吉拉特语" : "Gujarati");
                case 0x0448: return isTraditionalChinese ? "奧里雅語" : (isSimplifiedChinese ? "奥里雅语" : "Odia");
                case 0x0449: return isTraditionalChinese ? "泰米爾語" : (isSimplifiedChinese ? "泰米尔语" : "Tamil");
                case 0x044A: return isTraditionalChinese ? "泰盧固語" : (isSimplifiedChinese ? "泰卢固语" : "Telugu");
                case 0x044B: return isTraditionalChinese ? "卡納達語" : (isSimplifiedChinese ? "卡纳达语" : "Kannada");
                case 0x044C: return isTraditionalChinese ? "馬拉雅拉姆語" : (isSimplifiedChinese ? "马拉雅拉姆语" : "Malayalam");
                case 0x044D: return isTraditionalChinese ? "阿薩姆語" : (isSimplifiedChinese ? "阿萨姆语" : "Assamese");
                case 0x044E: return isTraditionalChinese ? "馬拉地語" : (isSimplifiedChinese ? "马拉地语" : "Marathi");
                case 0x044F: return isTraditionalChinese ? "梵語" : (isSimplifiedChinese ? "梵语" : "Sanskrit");
                case 0x0450: return isTraditionalChinese ? "蒙古語（西里爾文）" : (isSimplifiedChinese ? "蒙古语（西里尔文）" : "Mongolian (Cyrillic)");
                case 0x0451: return isTraditionalChinese ? "藏語" : (isSimplifiedChinese ? "藏语" : "Tibetan");
                case 0x0452: return isTraditionalChinese ? "威爾士語" : (isSimplifiedChinese ? "威尔士语" : "Welsh");
                case 0x045A: return isTraditionalChinese ? "敘利亞語" : (isSimplifiedChinese ? "叙利亚语" : "Syriac");
                case 0x045B: return isTraditionalChinese ? "僧伽羅語" : (isSimplifiedChinese ? "僧伽罗语" : "Sinhala");
                case 0x0461: return isTraditionalChinese ? "尼泊爾語" : (isSimplifiedChinese ? "尼泊尔语" : "Nepali");
                case 0x0462: return isTraditionalChinese ? "弗裡西亞語" : (isSimplifiedChinese ? "弗里西亚语" : "Frisian");
                case 0x0463: return isTraditionalChinese ? "普什圖語" : (isSimplifiedChinese ? "普什图语" : "Pashto");
                case 0x0464: return isTraditionalChinese ? "菲律賓語" : (isSimplifiedChinese ? "菲律宾语" : "Filipino");
                case 0x0465: return isTraditionalChinese ? "迪維希語" : (isSimplifiedChinese ? "迪维希语" : "Divehi");
                case 0x0468: return isTraditionalChinese ? "豪薩語" : (isSimplifiedChinese ? "豪萨语" : "Hausa");
                case 0x046A: return isTraditionalChinese ? "約魯巴語" : (isSimplifiedChinese ? "约鲁巴语" : "Yoruba");
                case 0x046B: return isTraditionalChinese ? "克丘亞語（玻利維亞）" : (isSimplifiedChinese ? "克丘亚语（玻利维亚）" : "Quechua (Bolivia)");
                case 0x046C: return isTraditionalChinese ? "北索托語" : (isSimplifiedChinese ? "北索托语" : "Sepedi");
                case 0x046D: return isTraditionalChinese ? "巴什基爾語" : (isSimplifiedChinese ? "巴什基尔语" : "Bashkir");
                case 0x046E: return isTraditionalChinese ? "盧森堡語" : (isSimplifiedChinese ? "卢森堡语" : "Luxembourgish");
                case 0x046F: return isTraditionalChinese ? "格陵蘭語" : (isSimplifiedChinese ? "格陵兰语" : "Greenlandic");
                case 0x0470: return isTraditionalChinese ? "伊博語" : (isSimplifiedChinese ? "伊博语" : "Igbo");
                case 0x0473: return isTraditionalChinese ? "提格利尼亞語" : (isSimplifiedChinese ? "提格利尼亚语" : "Tigrinya");
                case 0x0475: return isTraditionalChinese ? "夏威夷語" : (isSimplifiedChinese ? "夏威夷语" : "Hawaiian");
                case 0x0478: return isTraditionalChinese ? "彝語" : (isSimplifiedChinese ? "彝语" : "Yi");
                case 0x047A: return isTraditionalChinese ? "馬普切語" : (isSimplifiedChinese ? "马普切语" : "Mapudungun");
                case 0x047C: return isTraditionalChinese ? "莫霍克語" : (isSimplifiedChinese ? "莫霍克语" : "Mohawk");
                case 0x047E: return isTraditionalChinese ? "布列塔尼語" : (isSimplifiedChinese ? "布列塔尼语" : "Breton");
                case 0x0480: return isTraditionalChinese ? "維吾爾語" : (isSimplifiedChinese ? "维吾尔语" : "Uyghur");
                case 0x0481: return isTraditionalChinese ? "毛利語" : (isSimplifiedChinese ? "毛利语" : "Maori");
                case 0x0482: return isTraditionalChinese ? "奧克西坦語" : (isSimplifiedChinese ? "奥克西坦语" : "Occitan");
                case 0x0483: return isTraditionalChinese ? "科西嘉語" : (isSimplifiedChinese ? "科西嘉语" : "Corsican");
                case 0x0484: return isTraditionalChinese ? "阿爾薩斯語" : (isSimplifiedChinese ? "阿尔萨斯语" : "Alsatian");
                case 0x0485: return isTraditionalChinese ? "雅庫特語" : (isSimplifiedChinese ? "雅库特语" : "Sakha");
                case 0x0486: return isTraditionalChinese ? "盧旺達語" : (isSimplifiedChinese ? "卢旺达语" : "Kinyarwanda");
                case 0x0487: return isTraditionalChinese ? "沃洛夫語" : (isSimplifiedChinese ? "沃洛夫语" : "Wolof");
                case 0x0488: return isTraditionalChinese ? "達里語" : (isSimplifiedChinese ? "达里语" : "Dari");
                case 0x048C: return isTraditionalChinese ? "斯瓦蒂語" : (isSimplifiedChinese ? "斯瓦蒂语" : "SiSwati");
                case 0x048F: return isTraditionalChinese ? "低地德語" : (isSimplifiedChinese ? "低地德语" : "Low German");
                case 0x0491: return isTraditionalChinese ? "雅庫特語" : (isSimplifiedChinese ? "雅库特语" : "Yakut");
                case 0x0493: return isTraditionalChinese ? "土庫曼語（西里爾文）" : (isSimplifiedChinese ? "土库曼语（西里尔文）" : "Turkmen (Cyrillic)");
                case 0x0800: return isTraditionalChinese ? "中文（簡體）" : (isSimplifiedChinese ? "中文（简体）" : "Chinese (Simplified)");
                case 0x0801: return isTraditionalChinese ? "阿拉伯語（伊拉克）" : (isSimplifiedChinese ? "阿拉伯语（伊拉克）" : "Arabic (Iraq)");
                case 0x0804: return isTraditionalChinese ? "中文（簡體）" : (isSimplifiedChinese ? "中文（简体）" : "Chinese (Simplified)");
                case 0x0807: return isTraditionalChinese ? "德語（瑞士）" : (isSimplifiedChinese ? "德语（瑞士）" : "German (Switzerland)");
                case 0x0809: return isTraditionalChinese ? "英語（英國）" : (isSimplifiedChinese ? "英语（英国）" : "English (United Kingdom)");
                case 0x080A: return isTraditionalChinese ? "西班牙語（墨西哥）" : (isSimplifiedChinese ? "西班牙语（墨西哥）" : "Spanish (Mexico)");
                case 0x080C: return isTraditionalChinese ? "法語（比利時）" : (isSimplifiedChinese ? "法语（比利时）" : "French (Belgian)");
                case 0x0810: return isTraditionalChinese ? "意大利語（瑞士）" : (isSimplifiedChinese ? "意大利语（瑞士）" : "Italian (Switzerland)");
                case 0x0813: return isTraditionalChinese ? "荷蘭語（比利時）" : (isSimplifiedChinese ? "荷兰语（比利时）" : "Dutch (Belgium)");
                case 0x0814: return isTraditionalChinese ? "挪威語（尼諾斯克）" : (isSimplifiedChinese ? "挪威语（尼诺斯克）" : "Norwegian (Nynorsk)");
                case 0x0816: return isTraditionalChinese ? "葡萄牙語（標準）" : (isSimplifiedChinese ? "葡萄牙语（标准）" : "Portuguese (Standard)");
                case 0x0818: return isTraditionalChinese ? "羅馬尼亞語（摩爾多瓦）" : (isSimplifiedChinese ? "罗马尼亚语（摩尔多瓦）" : "Romanian (Moldova)");
                case 0x0819: return isTraditionalChinese ? "俄語（摩爾多瓦）" : (isSimplifiedChinese ? "俄语（摩尔多瓦）" : "Russian (Moldova)");
                case 0x081A: return isTraditionalChinese ? "塞爾維亞語（拉丁文）" : (isSimplifiedChinese ? "塞尔维亚语（拉丁文）" : "Serbian (Latin)");
                case 0x081D: return isTraditionalChinese ? "瑞典語（芬蘭）" : (isSimplifiedChinese ? "瑞典语（芬兰）" : "Swedish (Finland)");
                case 0x0820: return isTraditionalChinese ? "烏爾都語（印度）" : (isSimplifiedChinese ? "乌尔都语（印度）" : "Urdu (India)");
                case 0x0827: return isTraditionalChinese ? "立陶宛語（經典）" : (isSimplifiedChinese ? "立陶宛语（经典）" : "Lithuanian (Classic)");
                case 0x082C: return isTraditionalChinese ? "阿塞拜疆語（西里爾文）" : (isSimplifiedChinese ? "阿塞拜疆语（西里尔文）" : "Azerbaijani (Cyrillic)");
                case 0x082E: return isTraditionalChinese ? "下索布語" : (isSimplifiedChinese ? "下索布语" : "Lower Sorbian");
                case 0x083B: return isTraditionalChinese ? "薩米語（北部，芬蘭）" : (isSimplifiedChinese ? "萨米语（北部，芬兰）" : "Sami (Northern, Finland)");
                case 0x083E: return isTraditionalChinese ? "馬來語（汶萊達魯薩蘭國）" : (isSimplifiedChinese ? "马来语（文莱达鲁萨兰国）" : "Malay (Brunei Darussalam)");
                case 0x0843: return isTraditionalChinese ? "烏茲別克語（西里爾文）" : (isSimplifiedChinese ? "乌兹别克语（西里尔文）" : "Uzbek (Cyrillic)");
                case 0x0845: return isTraditionalChinese ? "孟加拉語（印度）" : (isSimplifiedChinese ? "孟加拉语（印度）" : "Bengali (India)");
                case 0x0846: return isTraditionalChinese ? "旁遮普語（印度）" : (isSimplifiedChinese ? "旁遮普语（印度）" : "Punjabi (India)");
                case 0x0849: return isTraditionalChinese ? "泰米爾語（印度）" : (isSimplifiedChinese ? "泰米尔语（印度）" : "Tamil (India)");
                case 0x0850: return isTraditionalChinese ? "蒙古語（傳統）" : (isSimplifiedChinese ? "蒙古语（传统）" : "Mongolian (Traditional)");
                case 0x0851: return isTraditionalChinese ? "藏語（中國）" : (isSimplifiedChinese ? "藏语（中国）" : "Tibetan (PRC)");
                case 0x085D: return isTraditionalChinese ? "因紐特語（拉丁文）" : (isSimplifiedChinese ? "因纽特语（拉丁文）" : "Inuktitut (Latin)");
                case 0x0860: return isTraditionalChinese ? "克什米爾語（天城文）" : (isSimplifiedChinese ? "克什米尔语（天城文）" : "Kashmiri (Devanagari)");
                case 0x0861: return isTraditionalChinese ? "尼泊爾語（印度）" : (isSimplifiedChinese ? "尼泊尔语（印度）" : "Nepali (India)");
                case 0x086B: return isTraditionalChinese ? "克丘亞語（厄瓜多爾）" : (isSimplifiedChinese ? "克丘亚语（厄瓜多尔）" : "Quechua (Ecuador)");
                case 0x0873: return isTraditionalChinese ? "提格利尼亞語（厄立特裡亞）" : (isSimplifiedChinese ? "提格利尼亚语（厄立特里亚）" : "Tigrinya (Eritrea)");
                case 0x0C04: return isTraditionalChinese ? "中文（香港特別行政區）" : (isSimplifiedChinese ? "中文（香港特别行政区）" : "Chinese (Hong Kong S.A.R.)");
                case 0x0C09: return isTraditionalChinese ? "英語（澳大利亞）" : (isSimplifiedChinese ? "英语（澳大利亚）" : "English (Australia)");
                case 0x0C0A: return isTraditionalChinese ? "西班牙語（現代排序）" : (isSimplifiedChinese ? "西班牙语（现代排序）" : "Spanish (Modern Sort)");
                case 0x0C0C: return isTraditionalChinese ? "法語（加拿大）" : (isSimplifiedChinese ? "法语（加拿大）" : "French (Canada)");
                case 0x0C1A: return isTraditionalChinese ? "塞爾維亞語（西里爾文）" : (isSimplifiedChinese ? "塞尔维亚语（西里尔文）" : "Serbian (Cyrillic)");
                case 0x0C6B: return isTraditionalChinese ? "克丘亞語（秘魯）" : (isSimplifiedChinese ? "克丘亚语（秘鲁）" : "Quechua (Peru)");
                case 0x1004: return isTraditionalChinese ? "中文（新加坡）" : (isSimplifiedChinese ? "中文（新加坡）" : "Chinese (Singapore)");
                case 0x1009: return isTraditionalChinese ? "英語（加拿大）" : (isSimplifiedChinese ? "英语（加拿大）" : "English (Canada)");
                case 0x100A: return isTraditionalChinese ? "西班牙語（危地馬拉）" : (isSimplifiedChinese ? "西班牙语（危地马拉）" : "Spanish (Guatemala)");
                case 0x100C: return isTraditionalChinese ? "法語（瑞士）" : (isSimplifiedChinese ? "法语（瑞士）" : "French (Switzerland)");
                case 0x101A: return isTraditionalChinese ? "克羅地亞語（拉丁文）" : (isSimplifiedChinese ? "克罗地亚语（拉丁文）" : "Croatian (Latin)");
                case 0x106B: return isTraditionalChinese ? "克丘亞語（玻利維亞）" : (isSimplifiedChinese ? "克丘亚语（玻利维亚）" : "Quechua (Bolivia)");
                case 0x1401: return isTraditionalChinese ? "阿拉伯語（利比亞）" : (isSimplifiedChinese ? "阿拉伯语（利比亚）" : "Arabic (Libya)");
                case 0x1404: return isTraditionalChinese ? "中文（澳門特別行政區）" : (isSimplifiedChinese ? "中文（澳门特别行政区）" : "Chinese (Macao S.A.R.)");
                case 0x1409: return isTraditionalChinese ? "英語（新西蘭）" : (isSimplifiedChinese ? "英语（新西兰）" : "English (New Zealand)");
                case 0x140A: return isTraditionalChinese ? "西班牙語（哥斯達黎加）" : (isSimplifiedChinese ? "西班牙语（哥斯达黎加）" : "Spanish (Costa Rica)");
                case 0x140C: return isTraditionalChinese ? "法語（盧森堡）" : (isSimplifiedChinese ? "法语（卢森堡）" : "French (Luxembourg)");
                case 0x141A: return isTraditionalChinese ? "波斯尼亞語（拉丁文）" : (isSimplifiedChinese ? "波斯尼亚语（拉丁文）" : "Bosnian (Latin)");
                case 0x1801: return isTraditionalChinese ? "阿拉伯語（阿爾及利亞）" : (isSimplifiedChinese ? "阿拉伯语（阿尔及利亚）" : "Arabic (Algeria)");
                case 0x1809: return isTraditionalChinese ? "英語（愛爾蘭）" : (isSimplifiedChinese ? "英语（爱尔兰）" : "English (Ireland)");
                case 0x180A: return isTraditionalChinese ? "西班牙語（巴拿馬）" : (isSimplifiedChinese ? "西班牙语（巴拿马）" : "Spanish (Panama)");
                case 0x180C: return isTraditionalChinese ? "法語（摩納哥）" : (isSimplifiedChinese ? "法语（摩纳哥）" : "French (Monaco)");
                case 0x1C01: return isTraditionalChinese ? "阿拉伯語（摩洛哥）" : (isSimplifiedChinese ? "阿拉伯语（摩洛哥）" : "Arabic (Morocco)");
                case 0x1C09: return isTraditionalChinese ? "英語（南非）" : (isSimplifiedChinese ? "英语（南非）" : "English (South Africa)");
                case 0x1C0A: return isTraditionalChinese ? "西班牙語（多米尼加共和國）" : (isSimplifiedChinese ? "西班牙语（多米尼加共和国）" : "Spanish (Dominican Republic)");
                case 0x1C0C: return isTraditionalChinese ? "法語（加蓬）" : (isSimplifiedChinese ? "法语（加蓬）" : "French (West Indies)");
                case 0x2001: return isTraditionalChinese ? "阿拉伯語（突尼斯）" : (isSimplifiedChinese ? "阿拉伯语（突尼斯）" : "Arabic (Tunisia)");
                case 0x2009: return isTraditionalChinese ? "英語（牙買加）" : (isSimplifiedChinese ? "英语（牙买加）" : "English (Jamaica)");
                case 0x200A: return isTraditionalChinese ? "西班牙語（委內瑞拉）" : (isSimplifiedChinese ? "西班牙语（委内瑞拉）" : "Spanish (Venezuela)");
                case 0x200C: return isTraditionalChinese ? "法語（剛果）" : (isSimplifiedChinese ? "法语（刚果）" : "French (Congo)");
                case 0x2401: return isTraditionalChinese ? "阿拉伯語（阿曼）" : (isSimplifiedChinese ? "阿拉伯语（阿曼）" : "Arabic (Oman)");
                case 0x2409: return isTraditionalChinese ? "英語（加勒比海地區）" : (isSimplifiedChinese ? "英语（加勒比海地区）" : "English (Caribbean)");
                case 0x240A: return isTraditionalChinese ? "西班牙語（哥倫比亞）" : (isSimplifiedChinese ? "西班牙语（哥伦比亚）" : "Spanish (Colombia)");
                case 0x240C: return isTraditionalChinese ? "法語（塞內加爾）" : (isSimplifiedChinese ? "法语（塞内加尔）" : "French (Senegal)");
                case 0x2801: return isTraditionalChinese ? "阿拉伯語（也門）" : (isSimplifiedChinese ? "阿拉伯语（也门）" : "Arabic (Yemen)");
                case 0x2809: return isTraditionalChinese ? "英語（伯利茲）" : (isSimplifiedChinese ? "英语（伯利兹）" : "English (Belize)");
                case 0x280A: return isTraditionalChinese ? "西班牙語（秘魯）" : (isSimplifiedChinese ? "西班牙语（秘鲁）" : "Spanish (Peru)");
                case 0x280C: return isTraditionalChinese ? "法語（馬里）" : (isSimplifiedChinese ? "法语（马里）" : "French (Mali)");
                case 0x2C01: return isTraditionalChinese ? "阿拉伯語（約旦）" : (isSimplifiedChinese ? "阿拉伯语（约旦）" : "Arabic (Jordan)");
                case 0x2C09: return isTraditionalChinese ? "英語（特立尼達和多巴哥）" : (isSimplifiedChinese ? "英语（特立尼达和多巴哥）" : "English (Trinidad)");
                case 0x2C0A: return isTraditionalChinese ? "西班牙語（阿根廷）" : (isSimplifiedChinese ? "西班牙语（阿根廷）" : "Spanish (Argentina)");
                case 0x2C0C: return isTraditionalChinese ? "法語（科特迪瓦）" : (isSimplifiedChinese ? "法语（科特迪瓦）" : "French (Côte d'Ivoire)");
                case 0x3001: return isTraditionalChinese ? "阿拉伯語（黎巴嫩）" : (isSimplifiedChinese ? "阿拉伯语（黎巴嫩）" : "Arabic (Lebanon)");
                case 0x3009: return isTraditionalChinese ? "英語（津巴布韋）" : (isSimplifiedChinese ? "英语（津巴布韦）" : "English (Zimbabwe)");
                case 0x300A: return isTraditionalChinese ? "西班牙語（厄瓜多爾）" : (isSimplifiedChinese ? "西班牙语（厄瓜多尔）" : "Spanish (Ecuador)");
                case 0x300C: return isTraditionalChinese ? "法語（布基納法索）" : (isSimplifiedChinese ? "法语（布基纳法索）" : "French (Burkina Faso)");
                case 0x3401: return isTraditionalChinese ? "阿拉伯語（科威特）" : (isSimplifiedChinese ? "阿拉伯语（科威特）" : "Arabic (Kuwait)");
                case 0x3409: return isTraditionalChinese ? "英語（菲律賓）" : (isSimplifiedChinese ? "英语（菲律宾）" : "English (Philippines)");
                case 0x340A: return isTraditionalChinese ? "西班牙語（智利）" : (isSimplifiedChinese ? "西班牙语（智利）" : "Spanish (Chile)");
                case 0x340C: return isTraditionalChinese ? "法語（貝寧）" : (isSimplifiedChinese ? "法语（贝宁）" : "French (Benin)");
                case 0x3801: return isTraditionalChinese ? "阿拉伯語（阿聯酋）" : (isSimplifiedChinese ? "阿拉伯语（阿联酋）" : "Arabic (U.A.E.)");
                case 0x380A: return isTraditionalChinese ? "西班牙語（烏拉圭）" : (isSimplifiedChinese ? "西班牙语（乌拉圭）" : "Spanish (Uruguay)");
                case 0x380C: return isTraditionalChinese ? "法語（尼日爾）" : (isSimplifiedChinese ? "法语（尼日尔）" : "French (Niger)");
                case 0x3C01: return isTraditionalChinese ? "阿拉伯語（巴林）" : (isSimplifiedChinese ? "阿拉伯语（巴林）" : "Arabic (Bahrain)");
                case 0x3C09: return isTraditionalChinese ? "英語（印度尼西亞）" : (isSimplifiedChinese ? "英语（印度尼西亚）" : "English (Indonesia)");
                case 0x3C0A: return isTraditionalChinese ? "西班牙語（巴拉圭）" : (isSimplifiedChinese ? "西班牙语（巴拉圭）" : "Spanish (Paraguay)");
                case 0x3C0C: return isTraditionalChinese ? "法語（多哥）" : (isSimplifiedChinese ? "法语（多哥）" : "French (Togo)");
                case 0x4001: return isTraditionalChinese ? "阿拉伯語（卡塔爾）" : (isSimplifiedChinese ? "阿拉伯语（卡塔尔）" : "Arabic (Qatar)");
                case 0x4009: return isTraditionalChinese ? "英語（馬來西亞）" : (isSimplifiedChinese ? "英语（马来西亚）" : "English (Malaysia)");
                case 0x400A: return isTraditionalChinese ? "西班牙語（薩爾瓦多）" : (isSimplifiedChinese ? "西班牙语（萨尔瓦多）" : "Spanish (El Salvador)");
                case 0x400C: return isTraditionalChinese ? "法語（乍得）" : (isSimplifiedChinese ? "法语（乍得）" : "French (Chad)");
                case 0x4401: return isTraditionalChinese ? "阿拉伯語（敘利亞）" : (isSimplifiedChinese ? "阿拉伯语（叙利亚）" : "Arabic (Syria)");
                case 0x4409: return isTraditionalChinese ? "英語（新加坡）" : (isSimplifiedChinese ? "英语（新加坡）" : "English (Singapore)");
                case 0x440A: return isTraditionalChinese ? "西班牙語（洪都拉斯）" : (isSimplifiedChinese ? "西班牙语（洪都拉斯）" : "Spanish (Honduras)");
                case 0x440C: return isTraditionalChinese ? "法語（中非共和國）" : (isSimplifiedChinese ? "法语（中非共和国）" : "French (Central African Republic)");
                case 0x4809: return isTraditionalChinese ? "英語（阿拉伯聯合酋長國）" : (isSimplifiedChinese ? "英语（阿拉伯联合酋长国）" : "English (U.A.E.)");
                case 0x480A: return isTraditionalChinese ? "西班牙語（尼加拉瓜）" : (isSimplifiedChinese ? "西班牙语（尼加拉瓜）" : "Spanish (Nicaragua)");
                case 0x480C: return isTraditionalChinese ? "法語（剛果共和國）" : (isSimplifiedChinese ? "法语（刚果共和国）" : "French (Congo [DRC])");
                case 0x4C0A: return isTraditionalChinese ? "西班牙語（波多黎各）" : (isSimplifiedChinese ? "西班牙语（波多黎各）" : "Spanish (Puerto Rico)");
                case 0x4C0C: return isTraditionalChinese ? "法語（喀麥隆）" : (isSimplifiedChinese ? "法语（喀麦隆）" : "French (Cameroon)");
                case 0x500A: return isTraditionalChinese ? "西班牙語（美國）" : (isSimplifiedChinese ? "西班牙语（美国）" : "Spanish (United States)");
                case 0x500C: return isTraditionalChinese ? "法語（剛果民主共和國）" : (isSimplifiedChinese ? "法语（刚果民主共和国）" : "French (Congo [Republic])");
                case 0x540A: return isTraditionalChinese ? "西班牙語（拉丁美洲）" : (isSimplifiedChinese ? "西班牙语（拉丁美洲）" : "Spanish (Latin America)");
                case 0x540C: return isTraditionalChinese ? "法語（留尼汪）" : (isSimplifiedChinese ? "法语（留尼汪）" : "French (Réunion)");
                case 0x580A: return isTraditionalChinese ? "西班牙語（古巴）" : (isSimplifiedChinese ? "西班牙语（古巴）" : "Spanish (Cuba)");
                case 0x580C: return isTraditionalChinese ? "法語（馬約特）" : (isSimplifiedChinese ? "法语（马约特）" : "French (Mayotte)");
                case 0x6009: return isTraditionalChinese ? "英語（印度）" : (isSimplifiedChinese ? "英语（印度）" : "English (India)");
                case 0x640A: return isTraditionalChinese ? "西班牙語（多米尼加共和國）" : (isSimplifiedChinese ? "西班牙语（多米尼加共和国）" : "Spanish (Dominican Republic)");
                case 0x640C: return isTraditionalChinese ? "法語（馬格里布）" : (isSimplifiedChinese ? "法语（马格里布）" : "French (Maghreb)");
                case 0x6809: return isTraditionalChinese ? "英語（馬來西亞）" : (isSimplifiedChinese ? "英语（马来西亚）" : "English (Malaysia)");
                case 0x6C0A: return isTraditionalChinese ? "西班牙語（玻利維亞）" : (isSimplifiedChinese ? "西班牙语（玻利维亚）" : "Spanish (Bolivia)");
                case 0x7009: return isTraditionalChinese ? "英語（新加坡）" : (isSimplifiedChinese ? "英语（新加坡）" : "English (Singapore)");
                case 0x700A: return isTraditionalChinese ? "西班牙語（巴拉圭）" : (isSimplifiedChinese ? "西班牙语（巴拉圭）" : "Spanish (Paraguay)");
                case 0x700C: return isTraditionalChinese ? "法語（赤道幾內亞）" : (isSimplifiedChinese ? "法语（赤道几内亚）" : "French (Equatorial Guinea)");
                case 0x740A: return isTraditionalChinese ? "西班牙語（秘魯）" : (isSimplifiedChinese ? "西班牙语（秘鲁）" : "Spanish (Peru)");
                case 0x740C: return isTraditionalChinese ? "法語（瑞士）" : (isSimplifiedChinese ? "法语（瑞士）" : "French (Switzerland)");
                case 0x780A: return isTraditionalChinese ? "西班牙語（烏拉圭）" : (isSimplifiedChinese ? "西班牙语（乌拉圭）" : "Spanish (Uruguay)");
                case 0x780C: return isTraditionalChinese ? "法語（盧旺達）" : (isSimplifiedChinese ? "法语（卢旺达）" : "French (Rwanda)");
                case 0x7C0A: return isTraditionalChinese ? "西班牙語（委內瑞拉）" : (isSimplifiedChinese ? "西班牙语（委内瑞拉）" : "Spanish (Venezuela)");
                case 0x7C0C: return isTraditionalChinese ? "法語（北非）" : (isSimplifiedChinese ? "法语（北非）" : "French (North Africa)");
                default: return isTraditionalChinese ? $"未知語言 (0x{languageId:X4})" : (isSimplifiedChinese ? $"未知语言 (0x{languageId:X4})" : $"Unknown Language (0x{languageId:X4})");
            }
        }

        /// <summary>
        /// 根据代码页获取代码页名称
        /// </summary>
        /// <param name="codePage">代码页</param>
        /// <returns>代码页名称</returns>
        private static string GetCodePageName(uint codePage)
        {
            // 检查系统语言环境
            string cultureName = CultureInfo.CurrentCulture.Name;
            bool isSimplifiedChinese = cultureName.Equals("zh-CN", StringComparison.OrdinalIgnoreCase);
            bool isTraditionalChinese = cultureName.Equals("zh-TW", StringComparison.OrdinalIgnoreCase) || 
                                       cultureName.Equals("zh-HK", StringComparison.OrdinalIgnoreCase) ||
                                       cultureName.Equals("zh-MO", StringComparison.OrdinalIgnoreCase);

            // 常见的代码页映射
            switch (codePage)
            {
                case 0: return isTraditionalChinese ? "默認代碼頁" : (isSimplifiedChinese ? "默认代码页" : "Default Code Page");
                case 37: return isTraditionalChinese ? "IBM EBCDIC 美國/加拿大" : (isSimplifiedChinese ? "IBM EBCDIC 美国/加拿大" : "IBM EBCDIC US-Canada");
                case 437: return isTraditionalChinese ? "OEM 美國" : (isSimplifiedChinese ? "OEM 美国" : "OEM United States");
                case 500: return isTraditionalChinese ? "IBM EBCDIC 國際" : (isSimplifiedChinese ? "IBM EBCDIC 国际" : "IBM EBCDIC International");
                case 708: return isTraditionalChinese ? "阿拉伯語 (ASMO 708)" : (isSimplifiedChinese ? "阿拉伯语 (ASMO 708)" : "Arabic (ASMO 708)");
                case 720: return isTraditionalChinese ? "阿拉伯語 (DOS)" : (isSimplifiedChinese ? "阿拉伯语 (DOS)" : "Arabic (DOS)");
                case 737: return isTraditionalChinese ? "希臘語 (DOS)" : (isSimplifiedChinese ? "希腊语 (DOS)" : "Greek (DOS)");
                case 775: return isTraditionalChinese ? "波羅的海語 (DOS)" : (isSimplifiedChinese ? "波罗的海语 (DOS)" : "Baltic (DOS)");
                case 850: return isTraditionalChinese ? "西歐 (DOS)" : (isSimplifiedChinese ? "西欧 (DOS)" : "Western European (DOS)");
                case 852: return isTraditionalChinese ? "中歐 (DOS)" : (isSimplifiedChinese ? "中欧 (DOS)" : "Central European (DOS)");
                case 855: return isTraditionalChinese ? "OEM 西里爾文" : (isSimplifiedChinese ? "OEM 西里尔文" : "OEM Cyrillic");
                case 857: return isTraditionalChinese ? "土耳其語 (DOS)" : (isSimplifiedChinese ? "土耳其语 (DOS)" : "Turkish (DOS)");
                case 858: return isTraditionalChinese ? "OEM 多語言拉丁語 I" : (isSimplifiedChinese ? "OEM 多语言拉丁语 I" : "OEM Multilingual Latin I");
                case 860: return isTraditionalChinese ? "葡萄牙語 (DOS)" : (isSimplifiedChinese ? "葡萄牙语 (DOS)" : "Portuguese (DOS)");
                case 861: return isTraditionalChinese ? "冰島語 (DOS)" : (isSimplifiedChinese ? "冰岛语 (DOS)" : "Icelandic (DOS)");
                case 862: return isTraditionalChinese ? "希伯來語 (DOS)" : (isSimplifiedChinese ? "希伯来语 (DOS)" : "Hebrew (DOS)");
                case 863: return isTraditionalChinese ? "法語加拿大 (DOS)" : (isSimplifiedChinese ? "法语加拿大 (DOS)" : "French Canadian (DOS)");
                case 864: return isTraditionalChinese ? "阿拉伯語 (864)" : (isSimplifiedChinese ? "阿拉伯语 (864)" : "Arabic (864)");
                case 865: return isTraditionalChinese ? "北歐 (DOS)" : (isSimplifiedChinese ? "北欧 (DOS)" : "Nordic (DOS)");
                case 866: return isTraditionalChinese ? "西里爾文 (DOS)" : (isSimplifiedChinese ? "西里尔文 (DOS)" : "Cyrillic (DOS)");
                case 869: return isTraditionalChinese ? "希臘語，現代 (DOS)" : (isSimplifiedChinese ? "希腊语，现代 (DOS)" : "Greek, Modern (DOS)");
                case 874: return isTraditionalChinese ? "泰語 (Windows)" : (isSimplifiedChinese ? "泰语 (Windows)" : "Thai (Windows)");
                case 932: return isTraditionalChinese ? "日語 (Shift-JIS)" : (isSimplifiedChinese ? "日语 (Shift-JIS)" : "Japanese (Shift-JIS)");
                case 936: return isTraditionalChinese ? "中文簡體 (GB2312)" : (isSimplifiedChinese ? "中文简体 (GB2312)" : "Chinese Simplified (GB2312)");
                case 949: return isTraditionalChinese ? "韓語" : (isSimplifiedChinese ? "韩语" : "Korean");
                case 950: return isTraditionalChinese ? "中文繁體 (Big5)" : (isSimplifiedChinese ? "中文繁体 (Big5)" : "Chinese Traditional (Big5)");
                case 1200: return isTraditionalChinese ? "UTF-16 (小端序)" : (isSimplifiedChinese ? "UTF-16 (小端序)" : "UTF-16 (Little endian)");
                case 1201: return isTraditionalChinese ? "UTF-16 (大端序)" : (isSimplifiedChinese ? "UTF-16 (大端序)" : "UTF-16 (Big endian)");
                case 1250: return isTraditionalChinese ? "中歐 (Windows)" : (isSimplifiedChinese ? "中欧 (Windows)" : "Central European (Windows)");
                case 1251: return isTraditionalChinese ? "西里爾文 (Windows)" : (isSimplifiedChinese ? "西里尔文 (Windows)" : "Cyrillic (Windows)");
                case 1252: return isTraditionalChinese ? "西歐 (Windows)" : (isSimplifiedChinese ? "西欧 (Windows)" : "Western European (Windows)");
                case 1253: return isTraditionalChinese ? "希臘語 (Windows)" : (isSimplifiedChinese ? "希腊语 (Windows)" : "Greek (Windows)");
                case 1254: return isTraditionalChinese ? "土耳其語 (Windows)" : (isSimplifiedChinese ? "土耳其语 (Windows)" : "Turkish (Windows)");
                case 1255: return isTraditionalChinese ? "希伯來語 (Windows)" : (isSimplifiedChinese ? "希伯来语 (Windows)" : "Hebrew (Windows)");
                case 1256: return isTraditionalChinese ? "阿拉伯語 (Windows)" : (isSimplifiedChinese ? "阿拉伯语 (Windows)" : "Arabic (Windows)");
                case 1257: return isTraditionalChinese ? "波羅的海語 (Windows)" : (isSimplifiedChinese ? "波罗的海语 (Windows)" : "Baltic (Windows)");
                case 1258: return isTraditionalChinese ? "越南語 (Windows)" : (isSimplifiedChinese ? "越南语 (Windows)" : "Vietnamese (Windows)");
                case 10000: return isTraditionalChinese ? "西歐 (Mac)" : (isSimplifiedChinese ? "西欧 (Mac)" : "Western European (Mac)");
                case 10001: return isTraditionalChinese ? "日語 (Mac)" : (isSimplifiedChinese ? "日语 (Mac)" : "Japanese (Mac)");
                case 10002: return isTraditionalChinese ? "中文繁體 (Mac)" : (isSimplifiedChinese ? "中文繁体 (Mac)" : "Chinese Traditional (Mac)");
                case 10003: return isTraditionalChinese ? "韓語 (Mac)" : (isSimplifiedChinese ? "韩语 (Mac)" : "Korean (Mac)");
                case 10004: return isTraditionalChinese ? "阿拉伯語 (Mac)" : (isSimplifiedChinese ? "阿拉伯语 (Mac)" : "Arabic (Mac)");
                case 10005: return isTraditionalChinese ? "希伯來語 (Mac)" : (isSimplifiedChinese ? "希伯来语 (Mac)" : "Hebrew (Mac)");
                case 10006: return isTraditionalChinese ? "希臘語 (Mac)" : (isSimplifiedChinese ? "希腊语 (Mac)" : "Greek (Mac)");
                case 10007: return isTraditionalChinese ? "西里爾文 (Mac)" : (isSimplifiedChinese ? "西里尔文 (Mac)" : "Cyrillic (Mac)");
                case 10008: return isTraditionalChinese ? "中文簡體 (Mac)" : (isSimplifiedChinese ? "中文简体 (Mac)" : "Chinese Simplified (Mac)");
                case 10021: return isTraditionalChinese ? "泰語 (Mac)" : (isSimplifiedChinese ? "泰语 (Mac)" : "Thai (Mac)");
                case 10029: return isTraditionalChinese ? "中歐 (Mac)" : (isSimplifiedChinese ? "中欧 (Mac)" : "Central European (Mac)");
                case 10079: return isTraditionalChinese ? "冰島語 (Mac)" : (isSimplifiedChinese ? "冰岛语 (Mac)" : "Icelandic (Mac)");
                case 10081: return isTraditionalChinese ? "土耳其語 (Mac)" : (isSimplifiedChinese ? "土耳其语 (Mac)" : "Turkish (Mac)");
                case 10082: return isTraditionalChinese ? "克羅地亞語 (Mac)" : (isSimplifiedChinese ? "克罗地亚语 (Mac)" : "Croatian (Mac)");
                case 12000: return isTraditionalChinese ? "UTF-32 (小端序)" : (isSimplifiedChinese ? "UTF-32 (小端序)" : "UTF-32 (Little endian)");
                case 12001: return isTraditionalChinese ? "UTF-32 (大端序)" : (isSimplifiedChinese ? "UTF-32 (大端序)" : "UTF-32 (Big endian)");
                case 20936: return isTraditionalChinese ? "中文簡體 (GB2312-80)" : (isSimplifiedChinese ? "中文简体 (GB2312-80)" : "Chinese Simplified (GB2312-80)");
                case 28591: return isTraditionalChinese ? "ISO 8859-1 拉丁語 1" : (isSimplifiedChinese ? "ISO 8859-1 拉丁语 1" : "ISO 8859-1 Latin 1");
                case 28592: return isTraditionalChinese ? "ISO 8859-2 中歐" : (isSimplifiedChinese ? "ISO 8859-2 中欧" : "ISO 8859-2 Central European");
                case 28593: return isTraditionalChinese ? "ISO 8859-3 拉丁語 3" : (isSimplifiedChinese ? "ISO 8859-3 拉丁语 3" : "ISO 8859-3 Latin 3");
                case 28594: return isTraditionalChinese ? "ISO 8859-4 波羅的海語" : (isSimplifiedChinese ? "ISO 8859-4 波罗的海语" : "ISO 8859-4 Baltic");
                case 28595: return isTraditionalChinese ? "ISO 8859-5 西里爾文" : (isSimplifiedChinese ? "ISO 8859-5 西里尔文" : "ISO 8859-5 Cyrillic");
                case 28596: return isTraditionalChinese ? "ISO 8859-6 阿拉伯語" : (isSimplifiedChinese ? "ISO 8859-6 阿拉伯语" : "ISO 8859-6 Arabic");
                case 28597: return isTraditionalChinese ? "ISO 8859-7 希臘語" : (isSimplifiedChinese ? "ISO 8859-7 希腊语" : "ISO 8859-7 Greek");
                case 28598: return isTraditionalChinese ? "ISO 8859-8 希伯來語" : (isSimplifiedChinese ? "ISO 8859-8 希伯来语" : "ISO 8859-8 Hebrew");
                case 28599: return isTraditionalChinese ? "ISO 8859-9 土耳其語" : (isSimplifiedChinese ? "ISO 8859-9 土耳其语" : "ISO 8859-9 Turkish");
                case 28603: return isTraditionalChinese ? "ISO 8859-13 愛沙尼亞語" : (isSimplifiedChinese ? "ISO 8859-13 爱沙尼亚语" : "ISO 8859-13 Estonian");
                case 28605: return isTraditionalChinese ? "ISO 8859-15 拉丁語 9" : (isSimplifiedChinese ? "ISO 8859-15 拉丁语 9" : "ISO 8859-15 Latin 9");
                case 51936: return isTraditionalChinese ? "EUC 中文簡體" : (isSimplifiedChinese ? "EUC 中文简体" : "EUC Simplified Chinese");
                case 51949: return isTraditionalChinese ? "EUC 韓語" : (isSimplifiedChinese ? "EUC 韩语" : "EUC Korean");
                case 52936: return isTraditionalChinese ? "HZ-GB2312 中文簡體" : (isSimplifiedChinese ? "HZ-GB2312 中文简体" : "HZ-GB2312 Simplified Chinese");
                case 54936: return isTraditionalChinese ? "GB18030 中文簡體 (4 字節)" : (isSimplifiedChinese ? "GB18030 中文简体 (4 字节)" : "GB18030 Simplified Chinese (4 byte)");
                case 65000: return isTraditionalChinese ? "UTF-7" : (isSimplifiedChinese ? "UTF-7" : "UTF-7");
                case 65001: return isTraditionalChinese ? "UTF-8" : (isSimplifiedChinese ? "UTF-8" : "UTF-8");
                default: return isTraditionalChinese ? $"未知代碼頁 ({codePage})" : (isSimplifiedChinese ? $"未知代码页 ({codePage})" : $"Unknown Code Page ({codePage})");
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