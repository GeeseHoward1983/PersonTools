using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器版本信息字符串解析辅助模块
    /// 包含版本信息中字符串相关解析的辅助函数
    /// </summary>
    internal static class PEResourceParserVersionString
    {
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

                if (!VersionNodeReader.TryReadNodeHeader(fs, reader, out ushort wLength, out _, out _))
                {
                    return;
                }

                if (wLength < 6 || fs.Position + wLength - 6 > fs.Length)
                {
                    return;
                }

                // 读取szKey (UNICODE字符串 "StringFileInfo")
                string key = VersionNodeReader.ReadKey(fs, reader, wLength);

                if (key.Equals("StringFileInfo", StringComparison.OrdinalIgnoreCase))
                {
                    VersionNodeReader.ParseChildBlock(fs, startPosition, wLength, endPosition,
                        stringFileInfoEndPosition => PEResourceParserVersionTable.ParseStringTable(fs, reader, peInfo, stringFileInfoEndPosition));
                }

                // 确保位置正确前进到下一个兄弟节点
                VersionNodeReader.AdvanceToSibling(fs, startPosition, wLength, endPosition);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                // 忽略StringFileInfo解析错误
                peInfo.AdditionalInfo.FileVersion += $"; StringFileInfo解析错误: {ex.Message}";
            }
        }
    }
}