using PersonalTools.PEAnalyzer.Models;
using System.Globalization;
using System.IO;
using System.Text;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器版本信息表解析辅助模块
    /// 包含版本信息中表格相关解析的辅助函数
    /// </summary>
    internal static class PEResourceParserVersionTable
    {
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
                {
                    return;
                }

                ushort wLength = reader.ReadUInt16();
                ushort wValueLength = reader.ReadUInt16();
                ushort wType = reader.ReadUInt16();

                // 读取语言和代码页标识符（通常是8位十六进制字符串）
                string langId = PEResourceParserCore.ReadUnicodeStringWithMaxLength(reader, wLength); // 读取直到找到null终止符

                // 计算Strings的位置
                long keyLengthInBytes = (langId.Length + 1) * 2; // Unicode字符串长度 + null终止符
                long afterLangIdPosition = startPosition + 6 + keyLengthInBytes;
                long stringsPosition = afterLangIdPosition + 3 & ~3; // 对齐到4字节边界

                if (stringsPosition >= fs.Length || stringsPosition >= endPosition)
                {
                    return;
                }

                fs.Position = stringsPosition;

                // 解析字符串对
                long tableEndPosition = Math.Min(startPosition + wLength, endPosition);

                while (fs.Position < tableEndPosition && fs.Position + 6 <= fs.Length)
                {
                    long stringStartPos = fs.Position;

                    // 先读取头部信息判断长度
                    if (fs.Position + 6 > fs.Length)
                    {
                        break;
                    }

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
            catch (IOException ex)
            {
                // 忽略StringTable解析错误
                peInfo.AdditionalInfo.FileVersion += $"; StringTable解析错误: {ex.Message}";
            }
            catch (UnauthorizedAccessException ex)
            {
                // 忽略StringTable解析错误
                peInfo.AdditionalInfo.FileVersion += $"; StringTable解析错误: {ex.Message}";
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // 忽略StringTable解析错误
                peInfo.AdditionalInfo.FileVersion += $"; StringTable解析错误: {ex.Message}";
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
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
                {
                    return;
                }

                ushort wLength = reader.ReadUInt16();
                ushort wValueLength = reader.ReadUInt16();
                ushort wType = reader.ReadUInt16();

                if (wLength == 0 || startPosition + wLength > endPosition)
                {
                    return;
                }

                // 读取键名
                StringBuilder keySb = new();
                char ch;
                while (fs.Position < fs.Length && fs.Position < endPosition && fs.Position < startPosition + wLength)
                {
                    if (fs.Position + 2 > fs.Length)
                    {
                        break;
                    }

                    ch = (char)reader.ReadUInt16();
                    if (ch == '\0')
                    {
                        break;
                    }

                    keySb.Append(ch);
                }

                string keyValue = keySb.ToString();

                // 跳过可能的额外null字符并对齐到4字节边界
                long currentPosition = fs.Position;
                long valuePosition = currentPosition + 3 & ~3;

                // 确保valuePosition不超过边界
                if (valuePosition >= endPosition || valuePosition >= fs.Length)
                {
                    return;
                }

                fs.Position = valuePosition;

                // 读取值
                StringBuilder valueSb = new();
                long valueEndPosition = Math.Min(startPosition + wLength, endPosition);
                while (fs.Position < fs.Length && fs.Position < valueEndPosition)
                {
                    if (fs.Position + 2 > fs.Length)
                    {
                        break;
                    }

                    ch = (char)reader.ReadUInt16();
                    if (ch == '\0')
                    {
                        break;
                    }

                    valueSb.Append(ch);
                }

                string value = valueSb.ToString();

                // 根据键名设置相应的属性
                switch (keyValue.ToLower(CultureInfo.CurrentCulture))
                {
                    case "companyname":
                        peInfo.AdditionalInfo.CompanyName = value;
                        break;
                    case "filedescription":
                        peInfo.AdditionalInfo.FileDescription = value;
                        break;
                    case "fileversion":
                        peInfo.AdditionalInfo.FileVersion = value;
                        break;
                    case "productname":
                        peInfo.AdditionalInfo.ProductName = value;
                        break;
                    case "productversion":
                        peInfo.AdditionalInfo.ProductVersion = value;
                        break;
                    case "legalcopyright":
                        peInfo.AdditionalInfo.LegalCopyright = value;
                        break;
                    case "legaltrademarks":
                        peInfo.AdditionalInfo.LegalTrademarks = value;
                        break;
                    case "originalfilename":
                        peInfo.AdditionalInfo.OriginalFileName = value;
                        break;
                    case "internalname":
                        peInfo.AdditionalInfo.InternalName = value;
                        break;
                    case "copyright":
                        peInfo.AdditionalInfo.Copyright = value;
                        break;
                }

                // 确保位置正确前进到下一个兄弟节点
                long nextPosition = startPosition + wLength + 3 & ~3;
                if (nextPosition < endPosition && nextPosition > fs.Position)
                {
                    fs.Position = nextPosition;
                }
            }
            catch (IOException ex)
            {
                // 忽略StringPair解析错误
                peInfo.AdditionalInfo.FileVersion += $"; StringPair解析错误: {ex.Message}";
            }
            catch (UnauthorizedAccessException ex)
            {
                // 忽略StringPair解析错误
                peInfo.AdditionalInfo.FileVersion += $"; StringPair解析错误: {ex.Message}";
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // 忽略StringPair解析错误
                peInfo.AdditionalInfo.FileVersion += $"; StringPair解析错误: {ex.Message}";
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
            }
        }
    }
}