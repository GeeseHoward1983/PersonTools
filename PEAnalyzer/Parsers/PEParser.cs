using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Resources;
using System.IO;

namespace PersonalTools.PEAnalyzer.Parsers
{
    /// <summary>
    /// PE文件解析器核心类（编排头部与各内容目录解析器）。
    /// </summary>
    internal static class PEParser
    {
        /// <summary>
        /// 内容目录解析步骤（在头部解析完成后按顺序执行；每一步自行处理异常，互不影响）。
        /// 以数据形式集中表达对各解析模块的依赖，便于增删与降低核心方法的耦合。
        /// </summary>
        private static readonly Action<FileStream, BinaryReader, PEInfo>[] ContentParsers =
        [
            PEImportParser.ParseImportTable,
            PEExportParser.ParseExportTable,
            PEResourceParserVersion.ParseVersionInfo,
            PEResourceParserCertificate.ParseCertificateInfo,
            PEParserCLR.ParseCLRHeaderInfo,
            PEResourceParserIcon.ParseIconInfo,
        ];

        /// <summary>
        /// 解析PE文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>PE文件信息</returns>
        public static PEInfo ParsePEFile(string filePath)
        {
            // TODO(性能): 本方法为同步全量解析(磁盘 I/O + 六大目录)，当前由 UI 线程直接调用
            // (PEAnalyzerControl.LoadPEFile / DependencyNode.EnsureLoaded)，大文件或频繁展开依赖树会卡顿。
            // 后续可提供 ParsePEFileAsync 或调用侧 Task.Run 卸载到后台线程。
            PEInfo peInfo = new() { FilePath = filePath };

            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using BinaryReader reader = new(fs);

            ParseHeaders(fs, reader, peInfo);
            ParseContentDirectories(fs, reader, peInfo);

            return peInfo;
        }

        /// <summary>
        /// 解析并校验 DOS 头、NT 头、可选头与节头。
        /// </summary>
        private static void ParseHeaders(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            // 解析DOS头
            peInfo.DosHeader = PEHeaderParser.ParseDosHeader(reader);
            if (peInfo.DosHeader.e_magic != PEConstants.DosSignature)
            {
                throw new InvalidDataException("文件不是有效的PE文件: DOS头签名错误。");
            }

            // e_lfanew 须指向 DOS 头(64 字节)之后且在文件内；落在 DOS 头内或文件外均为畸形
            if (peInfo.DosHeader.e_lfanew < 64 || peInfo.DosHeader.e_lfanew >= fs.Length)
            {
                throw new InvalidDataException("文件不是有效的PE文件: e_lfanew 指向文件外部或 DOS 头内。");
            }

            fs.Position = peInfo.DosHeader.e_lfanew;

            if (fs.Position + 24 > fs.Length)
            {
                throw new InvalidDataException("文件不是有效的PE文件: NT头不完整。");
            }

            // 解析NT头
            peInfo.NtHeaders = PEHeaderParser.ParseNtHeaders(reader);
            if (peInfo.NtHeaders.Signature != PEConstants.NtSignature)
            {
                throw new InvalidDataException("文件不是有效的PE文件: NT头签名错误。");
            }

            if (peInfo.NtHeaders.FileHeader.SizeOfOptionalHeader == 0 ||
                fs.Position + peInfo.NtHeaders.FileHeader.SizeOfOptionalHeader > fs.Length)
            {
                throw new InvalidDataException("文件不是有效的PE文件: 可选头大小不正确。");
            }

            // 解析可选头
            peInfo.OptionalHeader = PEHeaderParser.ParseOptionalHeader(reader, peInfo.NtHeaders.FileHeader.SizeOfOptionalHeader);

            long sectionHeadersLength = peInfo.NtHeaders.FileHeader.NumberOfSections * (long)PEConstants.SectionHeaderSize;
            if (sectionHeadersLength > 0 && fs.Position + sectionHeadersLength > fs.Length)
            {
                throw new InvalidDataException("文件不是有效的PE文件: 节头不完整。");
            }

            // 解析节头
            peInfo.SectionHeaders = PEHeaderParser.ParseSectionHeaders(reader, peInfo.NtHeaders.FileHeader.NumberOfSections);
        }

        /// <summary>
        /// 依次执行各内容目录（导入/导出/版本/证书/CLR/图标）的解析。
        /// </summary>
        private static void ParseContentDirectories(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            foreach (Action<FileStream, BinaryReader, PEInfo> parse in ContentParsers)
            {
                // 隔离每个内容目录解析：单步对畸形数据抛出的异常（含 ArgumentOutOfRange/EndOfStream 等）
                // 不应中断后续目录解析（如 CLR 失败不该让图标解析不再执行）
                try
                {
                    parse(fs, reader, peInfo);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
                    or ArgumentOutOfRangeException or EndOfStreamException or InvalidDataException or OverflowException)
                {
                    PersonalTools.Utils.AppLogger.Log($"内容目录解析错误（已跳过该步）: {ex.Message}");
                }
            }
        }
    }
}
