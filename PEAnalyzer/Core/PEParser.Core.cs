using MyTool.PEAnalyzer.Models;
using MyTool.PEAnalyzer.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MyTool
{
    /// <summary>
    /// PE文件解析器核心类
    /// </summary>
    public static partial class PEParser
    {
        /// <summary>
        /// 解析PE文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>PE文件信息</returns>
        public static PEInfo ParsePEFile(string filePath)
        {
            var peInfo = new PEInfo { FilePath = filePath };

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);
            // 解析DOS头
            peInfo.DosHeader = ParseDosHeader(reader);

            // 移动到NT头位置
            fs.Position = peInfo.DosHeader.e_lfanew;

            // 解析NT头
            peInfo.NtHeaders = ParseNtHeaders(reader);

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