using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Resources;
using System.IO;
using System.Windows;

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
        public static PEInfo? ParsePEFile(string filePath)
        {
            PEInfo peInfo = new() { FilePath = filePath };

            using FileStream fs = new(filePath, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(fs);
            // 解析DOS头
            peInfo.DosHeader = ParseDosHeader(reader);
            if (peInfo.DosHeader.e_magic != 0x5A4D)
            {
                MessageBox.Show("文件不是有效的PE文件！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            // 移动到NT头位置
            fs.Position = peInfo.DosHeader.e_lfanew;

            // 解析NT头
            peInfo.NtHeaders = ParseNtHeaders(reader);
            if (peInfo.NtHeaders.Signature != 0x00004550)
            {
                MessageBox.Show("文件不是有效的PE文件！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            // 解析可选头
            peInfo.OptionalHeader = ParseOptionalHeader(reader, peInfo.NtHeaders.FileHeader.SizeOfOptionalHeader);

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