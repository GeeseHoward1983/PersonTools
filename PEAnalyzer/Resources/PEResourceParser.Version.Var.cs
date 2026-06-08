using PersonalTools.PEAnalyzer.Parsers;
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
                reader.ReadUInt16(); // wValueLength
                reader.ReadUInt16(); // wType

                if (wLength < 6 || fs.Position + wLength - 6 > fs.Length)
                {
                    return;
                }

                // 读取szKey (UNICODE字符串 "VarFileInfo")
                int maxKeyBytes = (int)Math.Min(wLength, (uint)(fs.Length - fs.Position));
                string key = Utilities.ReadUnicodeStringWithMaxBytes(reader, maxKeyBytes);

                if (key.Equals("VarFileInfo", StringComparison.OrdinalIgnoreCase))
                {
                    long varPosition = Utilities.AlignTo4(fs.Position);
                    long varFileInfoEndPosition = Math.Min(startPosition + wLength, endPosition);

                    if (varPosition < fs.Length && varPosition < varFileInfoEndPosition)
                    {
                        fs.Position = varPosition;
                        ParseVar(fs, reader, peInfo, varFileInfoEndPosition);
                    }
                }

                // 确保位置正确前进到下一个兄弟节点
                long nextPosition = Utilities.AlignTo4(startPosition + wLength);
                if (nextPosition < endPosition && nextPosition > fs.Position)
                {
                    fs.Position = nextPosition;
                }
            }
            catch (IOException ex)
            {
                peInfo.AdditionalInfo.FileVersion += $"; VarFileInfo解析错误: {ex.Message}";
            }
            catch (UnauthorizedAccessException ex)
            {
                peInfo.AdditionalInfo.FileVersion += $"; VarFileInfo解析错误: {ex.Message}";
            }
            catch (ArgumentOutOfRangeException ex)
            {
                peInfo.AdditionalInfo.FileVersion += $"; VarFileInfo解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析Var部分（VarFileInfo的子项），支持循环解析多个Var项。
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="endPosition">Var块的结束位置</param>
        internal static void ParseVar(FileStream fs, BinaryReader reader, PEInfo peInfo, long endPosition)
        {
            try
            {
                while (fs.Position < endPosition)
                {
                    long startPosition = fs.Position;
                    if (fs.Position + 6 > fs.Length)
                    {
                        break;
                    }

                    ushort wLength = reader.ReadUInt16();
                    ushort wValueLength = reader.ReadUInt16();
                    reader.ReadUInt16(); // wType

                    if (wLength == 0)
                    {
                        // 长度为0：跳转到下一个4字节边界后结束
                        long alignedPos = Utilities.AlignTo4(startPosition);
                        if (alignedPos < fs.Length)
                        {
                            fs.Position = alignedPos;
                        }

                        break;
                    }

                    if (!ParseSingleVar(fs, reader, peInfo, startPosition, wLength, wValueLength, endPosition))
                    {
                        break;
                    }
                }
            }
            catch (IOException ex)
            {
                peInfo.AdditionalInfo.FileVersion += $"; Var解析错误: {ex.Message}";
            }
            catch (UnauthorizedAccessException ex)
            {
                peInfo.AdditionalInfo.FileVersion += $"; Var解析错误: {ex.Message}";
            }
            catch (ArgumentOutOfRangeException ex)
            {
                peInfo.AdditionalInfo.FileVersion += $"; Var解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析单个 Var 项；返回 false 表示越界，应停止循环。
        /// </summary>
        private static bool ParseSingleVar(FileStream fs, BinaryReader reader, PEInfo peInfo, long startPosition, ushort wLength, ushort wValueLength, long endPosition)
        {
            // 读取变量名（通常是"Translation"）
            int maxVarNameBytes = (int)Math.Min(wLength, (uint)(fs.Length - fs.Position));
            string varName = Utilities.ReadUnicodeStringWithMaxBytes(reader, maxVarNameBytes);

            // 值对齐到4字节边界：用读取键名后的实际流位置，而非按字符串长度推算
            // （避免读取被截断或键名内嵌 NUL 时错位）
            long valuePosition = Utilities.AlignTo4(fs.Position);
            if (valuePosition >= fs.Length || valuePosition >= endPosition)
            {
                return false;
            }

            fs.Position = valuePosition;
            ApplyTranslationIfPresent(fs, reader, peInfo, varName, wValueLength, valuePosition, endPosition);

            // 前进到下一个兄弟节点（对齐到4字节边界）
            long nextPosition = Utilities.AlignTo4(startPosition + wLength);
            if (nextPosition < endPosition && nextPosition > fs.Position && nextPosition < fs.Length)
            {
                fs.Position = nextPosition;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 若当前 Var 为 Translation 且有足够数据，则解码语言/代码页并写入翻译信息。
        /// </summary>
        private static void ApplyTranslationIfPresent(FileStream fs, BinaryReader reader, PEInfo peInfo, string varName, ushort wValueLength, long valuePosition, long endPosition)
        {
            if (!varName.Equals("Translation", StringComparison.OrdinalIgnoreCase) || wValueLength < 4)
            {
                return;
            }

            if (valuePosition + wValueLength > fs.Length || valuePosition + wValueLength > endPosition)
            {
                return;
            }

            byte[] translationBytes = reader.ReadBytes(wValueLength);
            if (translationBytes.Length < 4)
            {
                return;
            }

            uint dword = BitConverter.ToUInt32(translationBytes, 0);
            uint languageId = dword & 0xFFFF;
            uint codePage = (dword >> 16) & 0xFFFF;
            peInfo.AdditionalInfo.TranslationInfo = PEResourceParserVersionLanguage.GetReadableTranslationInfo(languageId, codePage);
        }
    }
}
