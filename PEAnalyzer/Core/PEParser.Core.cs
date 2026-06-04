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
        /// 解析PE文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>PE文件信息</returns>
        public static PEInfo ParsePEFile(string filePath)
        {
            PEInfo peInfo = new() { FilePath = filePath };

            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using BinaryReader reader = new(fs);

            // 解析DOS头
            peInfo.DosHeader = ParseDosHeader(reader);
            if (peInfo.DosHeader.e_magic != 0x5A4D)
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
            if (peInfo.NtHeaders.Signature != 0x00004550)
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

            long sectionHeadersLength = peInfo.NtHeaders.FileHeader.NumberOfSections * 40L;
            if (sectionHeadersLength > 0 && fs.Position + sectionHeadersLength > fs.Length)
            {
                throw new InvalidDataException("文件不是有效的PE文件: 节头不完整。");
            }

            // 解析节头
            peInfo.SectionHeaders = ParseSectionHeaders(reader, peInfo.NtHeaders.FileHeader.NumberOfSections);

            // 解析导入表
            ParseImportTable(fs, reader, peInfo);

            // 解析导出表
            ParseExportTable(fs, reader, peInfo);

            // 解析资源信息（包括版本信息）
            PEResourceParserVersion.ParseVersionInfo(fs, reader, peInfo);

            // 解析证书信息
            PEResourceParserCertificate.ParseCertificateInfo(fs, reader, peInfo);

            // 解析CLR运行时头信息
            PEParserCLR.ParseCLRHeaderInfo(fs, reader, peInfo);

            // 解析图标信息
            PEResourceParserIcon.ParseIconInfo(fs, reader, peInfo);

            return peInfo;
        }
    }
}