using PersonalTools.PEAnalyzer.Parsers;
using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器版本信息表解析辅助模块
    /// 包含版本信息中表格相关解析的辅助函数
    /// </summary>
    internal static class PEResourceParserVersionTable
    {
        /// <summary>
        /// 版本字符串键 -> PEAdditionalInfo 赋值器（大小写不敏感），取代原先的大型 switch。
        /// </summary>
        private static readonly Dictionary<string, Action<PEAdditionalInfo, string>> VersionFieldSetters =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["CompanyName"] = (info, v) => info.CompanyName = v,
                ["FileDescription"] = (info, v) => info.FileDescription = v,
                ["FileVersion"] = (info, v) => info.FileVersion = v,
                ["ProductName"] = (info, v) => info.ProductName = v,
                ["ProductVersion"] = (info, v) => info.ProductVersion = v,
                ["LegalCopyright"] = (info, v) => info.LegalCopyright = v,
                ["LegalTrademarks"] = (info, v) => info.LegalTrademarks = v,
                ["OriginalFilename"] = (info, v) => info.OriginalFileName = v,
                ["InternalName"] = (info, v) => info.InternalName = v,
                ["Copyright"] = (info, v) => info.Copyright = v,
            };

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
                reader.ReadUInt16(); // wValueLength
                reader.ReadUInt16(); // wType

                if (wLength < 6 || fs.Position + wLength - 6 > fs.Length)
                {
                    return;
                }

                // 读取语言和代码页标识符（通常是8位十六进制字符串），随后对齐到 Strings 起始
                int maxLangIdBytes = (int)Math.Min(wLength, (uint)(fs.Length - fs.Position));
                _ = Utilities.ReadUnicodeStringWithMaxBytes(reader, maxLangIdBytes);

                long stringsPosition = Utilities.AlignTo4(fs.Position);
                if (stringsPosition >= fs.Length || stringsPosition >= endPosition)
                {
                    return;
                }

                fs.Position = stringsPosition;

                // 逐个解析字符串对，直到表尾或遇到长度为 0 的项
                long tableEndPosition = Math.Min(startPosition + wLength, endPosition);
                while (fs.Position < tableEndPosition && fs.Position + 6 <= fs.Length)
                {
                    long before = fs.Position;
                    if (!ParseStringPair(fs, reader, peInfo, tableEndPosition) || fs.Position <= before)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                peInfo.AdditionalInfo.FileVersion += $"; StringTable解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析单个字符串对。
        /// </summary>
        /// <returns>true 表示成功解析了一项（且已前进到下一项）；false 表示遇到终止项或越界，应停止。</returns>
        internal static bool ParseStringPair(FileStream fs, BinaryReader reader, PEInfo peInfo, long endPosition)
        {
            try
            {
                long startPosition = fs.Position;
                if (fs.Position + 6 > fs.Length)
                {
                    return false;
                }

                ushort wLength = reader.ReadUInt16();
                reader.ReadUInt16(); // wValueLength
                reader.ReadUInt16(); // wType

                if (wLength == 0 || startPosition + wLength > endPosition)
                {
                    return false;
                }

                // 块内上界：键与值都不超过 startPosition + wLength（同时不超过 endPosition）
                long blockEnd = Math.Min(startPosition + wLength, endPosition);

                // 读取键名（Unicode，至 null 或块尾）
                string keyValue = ReadUnicodeWithin(fs, reader, blockEnd);

                // 值对齐到 4 字节边界
                long valuePosition = Utilities.AlignTo4(fs.Position);
                if (valuePosition >= endPosition || valuePosition >= fs.Length)
                {
                    return false;
                }

                fs.Position = valuePosition;
                string value = ReadUnicodeWithin(fs, reader, blockEnd);

                if (VersionFieldSetters.TryGetValue(keyValue, out Action<PEAdditionalInfo, string>? setter))
                {
                    setter(peInfo.AdditionalInfo, value);
                }

                // 前进到下一个兄弟节点
                long nextPosition = Utilities.AlignTo4(startPosition + wLength);
                if (nextPosition < endPosition && nextPosition > fs.Position)
                {
                    fs.Position = nextPosition;
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                peInfo.AdditionalInfo.FileVersion += $"; StringPair解析错误: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// 从当前位置读取 Unicode 字符串，直到 null 终止符或到达上界 <paramref name="limit"/>。
        /// </summary>
        private static string ReadUnicodeWithin(FileStream fs, BinaryReader reader, long limit)
        {
            int maxBytes = (int)Math.Max(0, Math.Min(limit, fs.Length) - fs.Position);
            return Utilities.ReadUnicodeStringWithMaxBytes(reader, maxBytes);
        }
    }
}
