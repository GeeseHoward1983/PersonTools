using MyTool.PEAnalyzer.Models;
using System.IO;

namespace MyTool.PEAnalyzer.Resources
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
                    long alignedPosition = fs.Position + 3 & ~3;

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
                            uint fileVersionMajor = fixedFileInfo.dwFileVersionMS >> 16 & 0xFFFF;
                            uint fileVersionMinor = fixedFileInfo.dwFileVersionMS & 0xFFFF;
                            uint fileVersionBuild = fixedFileInfo.dwFileVersionLS >> 16 & 0xFFFF;
                            uint fileVersionRev = fixedFileInfo.dwFileVersionLS & 0xFFFF;
                            peInfo.AdditionalInfo.FileVersion = $"{fileVersionMajor}.{fileVersionMinor}.{fileVersionBuild}.{fileVersionRev}";

                            uint productVersionMajor = fixedFileInfo.dwProductVersionMS >> 16 & 0xFFFF;
                            uint productVersionMinor = fixedFileInfo.dwProductVersionMS & 0xFFFF;
                            uint productVersionBuild = fixedFileInfo.dwProductVersionLS >> 16 & 0xFFFF;
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
                        childrenStartPos = childrenStartPos + 3 & ~3; // 对齐到4字节边界

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
                        PEResourceParserVersionString.ParseStringFileInfo(fs, reader, peInfo, endPosition);
                    }
                    else if (key.Equals("VarFileInfo", StringComparison.OrdinalIgnoreCase))
                    {
                        PEResourceParserVersionVar.ParseVarFileInfo(fs, reader, peInfo, endPosition);
                    }

                    // 移动到下一个子项
                    long nextChildPos = childStartPos + wLength + 3 & ~3;
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
    }
}
