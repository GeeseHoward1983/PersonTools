using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器版本信息变量解析辅助模块
    /// 包含版本信息中变量相关解析的辅助函数
    /// </summary>
    internal static class PEResourceParserVersionVar
    {
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
                {
                    return;
                }

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
                    long varPosition = afterKeyPosition + 3 & ~3; // 对齐到4字节边界

                    long varFileInfoEndPosition = Math.Min(startPosition + wLength, endPosition);

                    if (varPosition < fs.Length && varPosition < varFileInfoEndPosition)
                    {
                        fs.Position = varPosition;
                        ParseVar(fs, reader, peInfo, varFileInfoEndPosition);
                    }
                }

                // 确保位置正确前进到下一个兄弟节点
                long nextPosition = startPosition + wLength + 3 & ~3;
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
                {
                    return;
                }

                ushort wLength = reader.ReadUInt16();
                ushort wValueLength = reader.ReadUInt16();
                ushort wType = reader.ReadUInt16();

                // 读取变量名（通常是"Translation"）
                string varName = PEResourceParserCore.ReadUnicodeStringWithMaxLength(reader, wLength);

                // 计算值的位置
                long keyLengthInBytes = (varName.Length + 1) * 2; // Unicode字符串长度 + null终止符
                long afterVarNamePosition = startPosition + 6 + keyLengthInBytes;
                long valuePosition = afterVarNamePosition + 3 & ~3; // 对齐到4字节边界

                if (valuePosition >= fs.Length || valuePosition >= endPosition)
                {
                    return;
                }

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
                        uint codePage = translation >> 16 & 0xFFFF;

                        // 存储翻译信息（转换为可读格式）
                        peInfo.AdditionalInfo.TranslationInfo = PEResourceParserVersionLanguage.GetReadableTranslationInfo(languageId, codePage);
                    }
                }

                // 移动到下一个Var（如果有）
                long nextPosition = startPosition + wLength + 3 & ~3;
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
    }
}