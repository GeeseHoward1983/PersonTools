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

                if (!VersionNodeReader.TryReadNodeHeader(fs, reader, out ushort wLength, out _, out _))
                {
                    return;
                }

                if (wLength < 6 || fs.Position + wLength - 6 > fs.Length)
                {
                    return;
                }

                // 读取语言和代码页标识符（通常是8位十六进制字符串），随后对齐到 Strings 起始
                _ = VersionNodeReader.ReadKey(fs, reader, wLength);

                long stringsPosition = PEParserUtils.AlignTo4(fs.Position);
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
                    if (!ParseStringPair(fs, reader, peInfo, tableEndPosition))
                    {
                        break;
                    }

                    // 末项时 AdvanceToSibling 不会前进（下一兄弟落在表尾外），若 fs.Position 停在
                    // 非对齐处会让下一轮从错位读取；统一对齐前进，无实质前进则结束，避免静默错位解析。
                    long aligned = PEParserUtils.AlignTo4(fs.Position);
                    fs.Position = aligned <= fs.Length ? aligned : fs.Length;
                    if (fs.Position <= before)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                // 仅记录日志，保留已成功解析的字符串字段，不把异常细节追加到展示字段
                PersonalTools.Utils.AppLogger.Log($"StringTable解析错误: {ex.Message}");
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
                if (!VersionNodeReader.TryReadNodeHeader(fs, reader, out ushort wLength, out _, out _))
                {
                    return false;
                }

                if (wLength == 0 || startPosition + wLength > endPosition)
                {
                    return false;
                }

                // 块内上界：键与值都不超过 startPosition + wLength（同时不超过 endPosition）
                long blockEnd = Math.Min(startPosition + wLength, endPosition);

                // 读取键名（Unicode，至 null 或块尾）
                string keyValue = ReadUnicodeWithin(fs, reader, blockEnd);

                // 值对齐到 4 字节边界
                long valuePosition = PEParserUtils.AlignTo4(fs.Position);
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
                VersionNodeReader.AdvanceToSibling(fs, startPosition, wLength, endPosition);

                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                PersonalTools.Utils.AppLogger.Log($"StringPair解析错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从当前位置读取 Unicode 字符串，直到 null 终止符或到达上界 <paramref name="limit"/>。
        /// </summary>
        private static string ReadUnicodeWithin(FileStream fs, BinaryReader reader, long limit)
        {
            // 用 long 计算可读字节数后再夹到 int 范围：>2GB 文件直接 (int) 强转会回绕成负值/巨值
            long available = Math.Max(0, Math.Min(limit, fs.Length) - fs.Position);
            int maxBytes = (int)Math.Min(available, int.MaxValue);
            return PEParserUtils.ReadUnicodeStringWithMaxBytes(reader, maxBytes);
        }
    }
}
