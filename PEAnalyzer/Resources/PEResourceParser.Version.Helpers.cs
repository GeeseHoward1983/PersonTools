using PersonalTools.PEAnalyzer.Parsers;
using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
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
                if (!VersionNodeReader.TryReadNodeHeader(fs, reader, out ushort wLength, out ushort wValueLength, out _))
                {
                    return;
                }

                if (wLength < 6 || startPosition + wLength > fs.Length)
                {
                    return;
                }

                // 读取szKey (UNICODE字符串 "VS_VERSION_INFO") 并校验
                string vsVersionInfoKey = VersionNodeReader.ReadKey(fs, reader, wLength);
                if (!vsVersionInfoKey.Equals("VS_VERSION_INFO", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // VS_FIXEDFILEINFO 紧随其后（对齐到4字节边界），大小 52 字节
                long alignedPosition = PEParserUtils.AlignTo4(fs.Position);

                // wValueLength==0 表示没有 VS_FIXEDFILEINFO（合法），直接解析子项
                if (wValueLength == 0)
                {
                    ParseChildrenIfPresent(fs, reader, peInfo, alignedPosition, startPosition + wLength);
                    return;
                }

                if (wValueLength < 52 || alignedPosition + 52 > fs.Length)
                {
                    peInfo.AdditionalInfo.FileVersion = $"版本信息数据不完整: wValueLength={wValueLength}, 需要>=52";
                    return;
                }

                fs.Position = alignedPosition;
                VS_FIXEDFILEINFO fixedFileInfo = ReadFixedFileInfo(reader);
                ApplyFixedFileVersions(peInfo, fixedFileInfo);

                // 子项（StringFileInfo / VarFileInfo）紧跟在 VS_FIXEDFILEINFO 之后
                ParseChildrenIfPresent(fs, reader, peInfo, PEParserUtils.AlignTo4(alignedPosition + 52), startPosition + wLength);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                PersonalTools.Utils.AppLogger.Log($"版本信息结构解析错误: {ex.Message}");
                peInfo.AdditionalInfo.FileVersion = "版本信息不完整";
            }
        }

        // 若子项起始位置有效（在文件内且未越过本结构末尾），定位并解析 StringFileInfo/VarFileInfo
        private static void ParseChildrenIfPresent(FileStream fs, BinaryReader reader, PEInfo peInfo, long childrenStart, long endPosition)
        {
            if (childrenStart < fs.Length && childrenStart < endPosition)
            {
                fs.Position = childrenStart;
                ParseVersionChildren(fs, reader, peInfo, endPosition);
            }
        }

        /// <summary>
        /// 读取 VS_FIXEDFILEINFO 结构（52 字节，13 个 DWORD）。
        /// </summary>
        private static VS_FIXEDFILEINFO ReadFixedFileInfo(BinaryReader reader)
        {
            return new VS_FIXEDFILEINFO
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
        }

        /// <summary>
        /// 校验签名并将 VS_FIXEDFILEINFO 中的文件/产品版本写入 PEInfo。
        /// </summary>
        private static void ApplyFixedFileVersions(PEInfo peInfo, VS_FIXEDFILEINFO info)
        {
            if (info.dwSignature != 0xFEEF04BD) // VS_VERSIONINFO签名
            {
                peInfo.AdditionalInfo.FileVersion = $"无效的版本信息签名: 0x{info.dwSignature:X8}";
                return;
            }

            peInfo.AdditionalInfo.FileVersion = FormatVersion(info.dwFileVersionMS, info.dwFileVersionLS);
            peInfo.AdditionalInfo.ProductVersion = FormatVersion(info.dwProductVersionMS, info.dwProductVersionLS);
        }

        /// <summary>
        /// 将 MS/LS 两个 DWORD 格式化为 "主.次.编译.修订" 版本字符串。
        /// </summary>
        private static string FormatVersion(uint ms, uint ls)
        {
            return $"{(ms >> 16) & 0xFFFF}.{ms & 0xFFFF}.{(ls >> 16) & 0xFFFF}.{ls & 0xFFFF}";
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
                    if (!VersionNodeReader.TryReadNodeHeader(fs, reader, out ushort wLength, out _, out _))
                    {
                        break;
                    }

                    if (wLength == 0)
                    {
                        break;
                    }

                    // 读取键名
                    string key = VersionNodeReader.ReadKey(fs, reader, wLength);

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

                    // 移动到下一个子项：基于 childStartPos+wLength 计算并直接定位。
                    // 退步判断须对比 childStartPos（而非已被子解析器 ParseStringFileInfo/ParseVarFileInfo 改动的 fs.Position），
                    // 否则会因 fs.Position 已前移而误判退步提前 break，漏掉后续兄弟节点（如 StringFileInfo 之后的 VarFileInfo）
                    long nextChildPos = (childStartPos + wLength + 3) & ~3;
                    if (nextChildPos >= endPosition || nextChildPos <= childStartPos)
                    {
                        break;
                    }

                    fs.Position = nextChildPos;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                // 仅记录日志，保留已成功解析的版本字段，不把异常细节追加到展示字段
                PersonalTools.Utils.AppLogger.Log($"版本子项解析错误: {ex.Message}");
            }
        }
    }
}
