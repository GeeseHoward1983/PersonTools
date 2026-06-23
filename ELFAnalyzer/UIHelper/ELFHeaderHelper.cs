using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.Utils;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.Globalization;
using System.Text;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class ELFHeaderHelper
    {
        internal static string GetFormattedELFHeaderInfo(ELFParser parser)
        {
            StringBuilder sb = new();
            sb.AppendLine("ELF 头:");
            sb.AppendLine("================================================================================");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Magic:            {GetMagicString(parser.Header)}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  类别:             {ELFHeaderDescriptions.GetELFClassName(parser.Header)} ({(parser.Is64Bit ? "64-bit" : "32-bit")})");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  数据:             {ELFHeaderDescriptions.GetELFDataName(parser.Header)} ({parser.Header.EI_DATA switch
            {
                (byte)ELFData.LSB => "2's complement, little endian",
                _ => "2's complement, big endian"
            }})");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  版本:             {ELFHeaderDescriptions.GetReadableVersion(parser.Header)} ({parser.Header.EI_VERSION})");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  OS/ABI:           {ELFHeaderDescriptions.GetOSABIName(parser.Header)} ({parser.Header.EI_OSABI})");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  ABI 版本:         {parser.Header.EI_ABIVERSION}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  类型:             {ELFHeaderDescriptions.GetELFTypeName(parser.Header)} ({ELFHeaderDescriptions.GetFileTypeDescription(parser.Header)})");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  系统架构:         {ELFHeaderDescriptions.GetArchitectureName(parser.Header)} ({ELFHeaderDescriptions.GetMachineDescription(parser.Header)})");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  版本:             0x{parser.Header.e_version:X}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  入口点地址:       {ELFHeaderDescriptions.GetEntryPointAddress(parser.Header)}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  程序头起点:       {parser.Header.e_phoff} (bytes into file)");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  节头的起点:       {parser.Header.e_shoff} (bytes into file)");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  标志:             0x{parser.Header.e_flags:X}  {ELFHeaderDescriptions.GetFormattedELFFlags(parser.Header)}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  本头的大小:       {ELFHeaderDescriptions.GetHeaderSize(parser.Header)}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  程序头的大小:     {parser.Header.e_phentsize} (bytes)");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  程序头数量:       {parser.Header.e_phnum}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  节头大小:         {parser.Header.e_shentsize} (bytes)");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  节头数量:         {parser.Header.e_shnum}");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  字符串表索引节头: {parser.Header.e_shstrndx}");
            return sb.ToString();
        }

        private static string GetMagicString(ELFHeader header)
        {
            StringBuilder magic = new();
            magic.Append(CultureInfo.InvariantCulture, $"{header.EI_MAG0:X2} ");
            magic.Append(CultureInfo.InvariantCulture, $"{header.EI_MAG1:X2} ");
            magic.Append(CultureInfo.InvariantCulture, $"{header.EI_MAG2:X2} ");
            magic.Append(CultureInfo.InvariantCulture, $"{header.EI_MAG3:X2} ");
            magic.Append(CultureInfo.InvariantCulture, $"{header.EI_CLASS:X2} ");
            magic.Append(CultureInfo.InvariantCulture, $"{header.EI_DATA:X2} ");
            magic.Append(CultureInfo.InvariantCulture, $"{header.EI_VERSION:X2} ");
            magic.Append(CultureInfo.InvariantCulture, $"{header.EI_OSABI:X2} ");
            magic.Append(CultureInfo.InvariantCulture, $"{header.EI_ABIVERSION:X2} ");

            // EI_PAD 逐字节按 "XX " 格式追加，与前 9 字节保持一致的空格分隔（避免拼成无分隔的连串，与 readelf 的 16 字节格式一致）
            foreach (byte pad in header.EI_PAD)
            {
                magic.Append(CultureInfo.InvariantCulture, $"{pad:X2} ");
            }
            return magic.ToString().Trim();
        }

    }
}