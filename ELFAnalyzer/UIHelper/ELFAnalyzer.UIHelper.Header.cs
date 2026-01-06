using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using System.Text;

namespace PersonalTools.ELFAnalyzer
{
    public partial class ELFAnalyzer
    {
        public string GetFormattedELFHeaderInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("ELF 头:");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"  Magic:            {GetMagicString()}");
            sb.AppendLine($"  类别:             {_parser.GetELFClassName()} ({(_parser.Header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? "64-bit" : "32-bit")})");
            sb.AppendLine($"  数据:             {_parser.GetELFDataName()} ({(_parser.Header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? "2's complement, 小端" : "2's complement, 大端")})");
            sb.AppendLine($"  版本:             {_parser.GetReadableVersion()} ({_parser.Header.EI_VERSION})");
            sb.AppendLine($"  OS/ABI:           {_parser.GetOSABIName()} ({_parser.Header.EI_OSABI})");
            sb.AppendLine($"  ABI 版本:         {_parser.Header.EI_ABIVERSION}");
            sb.AppendLine($"  类型:             {_parser.GetELFTypeName()} ({_parser.GetFileTypeDescription()})");
            sb.AppendLine($"  系统架构:         {_parser.GetArchitectureName()} ({_parser.GetMachineDescription()})");
            sb.AppendLine($"  版本:             0x{_parser.Header.e_version:X}");
            sb.AppendLine($"  入口点地址:       {_parser.GetEntryPointAddress()}");
            sb.AppendLine($"  程序头起点:       {_parser.Header.e_phoff} (bytes into file)");
            sb.AppendLine($"  节头的起点:       {_parser.Header.e_shoff} (bytes into file)");
            sb.AppendLine($"  标志:             0x{_parser.Header.e_flags:X}  {_parser.GetFormattedELFFlags()}");
            sb.AppendLine($"  本头的大小:       {_parser.GetHeaderSize()}");
            sb.AppendLine($"  程序头的大小:     {_parser.Header.e_phentsize} (bytes)");
            sb.AppendLine($"  程序头数量:       {_parser.Header.e_phnum}");
            sb.AppendLine($"  节头大小:         {_parser.Header.e_shentsize} (bytes)");
            sb.AppendLine($"  节头数量:         {_parser.Header.e_shnum}");
            sb.AppendLine($"  字符串表索引节头: {_parser.Header.e_shstrndx}");
            return sb.ToString();
        }
    }
}