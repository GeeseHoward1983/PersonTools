using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Resources;
using System.IO;

namespace PersonalTools
{
    /// <summary>
    /// PE文件解析器核心类
    /// </summary>
    internal static partial class PEParser
    {
        /// <summary>
        /// 内容目录解析步骤（在头部解析完成后按顺序执行；每一步自行处理异常，互不影响）。
        /// 以数据形式集中表达对各解析模块的依赖，便于增删与降低核心方法的耦合。
        /// </summary>
        private static readonly Action<FileStream, BinaryReader, PEInfo>[] ContentParsers =
        [
            ParseImportTable,
            ParseExportTable,
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
            peInfo.DosHeader = ParseDosHeader(reader);
            if (peInfo.DosHeader.e_magic != PEConstants.DosSignature)
            {
                throw new InvalidDataException("文件不是有效的PE文件: DOS头签名错误。");
            }

            if (peInfo.DosHeader.e_lfanew >= fs.Length)
            {
                throw new InvalidDataException("文件不是有效的PE文件: e_lfanew 指向文件外部。");
            }

            fs.Position = peInfo.DosHeader.e_lfanew;

            if (fs.Position + 24 > fs.Length)
            {
                throw new InvalidDataException("文件不是有效的PE文件: NT头不完整。");
            }

            // 解析NT头
            peInfo.NtHeaders = ParseNtHeaders(reader);
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
            peInfo.OptionalHeader = ParseOptionalHeader(reader, peInfo.NtHeaders.FileHeader.SizeOfOptionalHeader);

            long sectionHeadersLength = peInfo.NtHeaders.FileHeader.NumberOfSections * (long)PEConstants.SectionHeaderSize;
            if (sectionHeadersLength > 0 && fs.Position + sectionHeadersLength > fs.Length)
            {
                throw new InvalidDataException("文件不是有效的PE文件: 节头不完整。");
            }

            // 解析节头
            peInfo.SectionHeaders = ParseSectionHeaders(reader, peInfo.NtHeaders.FileHeader.NumberOfSections);
        }

        /// <summary>
        /// 依次执行各内容目录（导入/导出/版本/证书/CLR/图标）的解析。
        /// </summary>
        private static void ParseContentDirectories(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            foreach (Action<FileStream, BinaryReader, PEInfo> parse in ContentParsers)
            {
                parse(fs, reader, peInfo);
            }
        }
    }
}
