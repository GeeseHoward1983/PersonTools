using System;
using System.IO;
using System.Text;

namespace MyTool
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
                        PEResourceParserVersionTable.ParseStringTable(fs, reader, peInfo, stringFileInfoEndPosition);
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
    }
}