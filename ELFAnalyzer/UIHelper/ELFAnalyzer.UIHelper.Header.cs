using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System;
using System.Text;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    public class ELFHeaderHelper
    {
        public static string GetFormattedELFHeaderInfo(ELFParser parser)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ELF 头:");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"  Magic:            {GetMagicString(parser.Header)}");
            sb.AppendLine($"  类别:             {HeaderInfo.GetELFClassName(parser.Header)} ({(parser.Is64Bit switch
            {
                true => "64-bit",
                _ => "32-bit"
            })})");
            sb.AppendLine($"  数据:             {HeaderInfo.GetELFDataName(parser.Header)} ({(parser.Header.EI_DATA switch
            {
                (byte)ELFData.ELFDATA2LSB => "2's complement, little endian",
                _ => "2's complement, big endian"
            } )})");
            sb.AppendLine($"  版本:             {HeaderInfo.GetReadableVersion(parser.Header)} ({parser.Header.EI_VERSION})");
            sb.AppendLine($"  OS/ABI:           {HeaderInfo.GetOSABIName(parser.Header)} ({parser.Header.EI_OSABI})");
            sb.AppendLine($"  ABI 版本:         {parser.Header.EI_ABIVERSION}");
            sb.AppendLine($"  类型:             {HeaderInfo.GetELFTypeName(parser.Header)} ({HeaderInfo.GetFileTypeDescription(parser.Header)})");
            sb.AppendLine($"  系统架构:         {HeaderInfo.GetArchitectureName(parser.Header)} ({HeaderInfo.GetMachineDescription(parser.Header)})");
            sb.AppendLine($"  版本:             0x{parser.Header.e_version:X}");
            sb.AppendLine($"  入口点地址:       {HeaderInfo.GetEntryPointAddress(parser.Header)}");
            sb.AppendLine($"  程序头起点:       {parser.Header.e_phoff} (bytes into file)");
            sb.AppendLine($"  节头的起点:       {parser.Header.e_shoff} (bytes into file)");
            sb.AppendLine($"  标志:             0x{parser.Header.e_flags:X}  {HeaderInfo.GetFormattedELFFlags(parser.Header)}");
            sb.AppendLine($"  本头的大小:       {HeaderInfo.GetHeaderSize(parser.Header)}");
            sb.AppendLine($"  程序头的大小:     {parser.Header.e_phentsize} (bytes)");
            sb.AppendLine($"  程序头数量:       {parser.Header.e_phnum}");
            sb.AppendLine($"  节头大小:         {parser.Header.e_shentsize} (bytes)");
            sb.AppendLine($"  节头数量:         {parser.Header.e_shnum}");
            sb.AppendLine($"  字符串表索引节头: {parser.Header.e_shstrndx}");
            return sb.ToString();
        }

        private static string GetMagicString(ELFHeader header)
        {
            var magic = new StringBuilder();
            magic.Append($"{header.EI_MAG0:X2} ");
            magic.Append($"{header.EI_MAG1:X2} ");
            magic.Append($"{header.EI_MAG2:X2} ");
            magic.Append($"{header.EI_MAG3:X2} ");

            magic.Append($"{Utils.ToHexString(header.EI_PAD)} ");
            return magic.ToString().Trim();
        }

    }
}