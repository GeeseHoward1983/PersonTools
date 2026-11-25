using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.Globalization;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class ELFHeaderHelper
    {
        internal static string GetFormattedELFHeaderInfo(ELFParser parser)
        {
            // 最后的空行保留原输出末尾的换行；所有数值统一使用固定区域性。
            return string.Join(Environment.NewLine,
            [
                "ELF 头:",
                "================================================================================",
                .. GetHeaderLines(parser.Header, parser.Is64Bit).Select(FormatLine),
                string.Empty
            ]);
        }

        private static IEnumerable<FormattableString> GetHeaderLines(ELFHeader header, bool is64Bit)
        {
            return GetIdentificationLines(header, is64Bit)
                .Concat(GetFileLines(header))
                .Concat(GetLayoutLines(header))
                .Concat(GetSizeAndCountLines(header));
        }

        private static FormattableString[] GetIdentificationLines(ELFHeader header, bool is64Bit)
        {
            return
            [
                $"  Magic:            {GetMagicString(header)}",
                $"  类别:             {ELFHeaderDescriptions.GetELFClassName(header)} ({(is64Bit ? "64-bit" : "32-bit")})",
                $"  数据:             {ELFHeaderDescriptions.GetELFDataName(header)} ({GetDataDescription(header.EI_DATA)})",
                $"  版本:             {ELFHeaderDescriptions.GetReadableVersion(header)}",
                $"  OS/ABI:           {ELFHeaderDescriptions.GetOSABIName(header)} ({header.EI_OSABI})",
                $"  ABI 版本:         {header.EI_ABIVERSION}"
            ];
        }

        private static FormattableString[] GetFileLines(ELFHeader header)
        {
            return
            [
                $"  类型:             {ELFHeaderDescriptions.GetELFTypeName(header)} ({ELFHeaderDescriptions.GetFileTypeDescription(header)})",
                $"  系统架构:         {ELFHeaderDescriptions.GetArchitectureName(header)} ({ELFHeaderDescriptions.GetMachineDescription(header)})",
                $"  版本:             0x{header.e_version:X}",
                $"  入口点地址:       {ELFHeaderDescriptions.GetEntryPointAddress(header)}"
            ];
        }

        private static FormattableString[] GetLayoutLines(ELFHeader header)
        {
            return
            [
                $"  程序头起点:       {header.e_phoff} (bytes into file)",
                $"  节头的起点:       {header.e_shoff} (bytes into file)",
                $"  标志:             0x{header.e_flags:X}  {ELFHeaderDescriptions.GetFormattedELFFlags(header)}"
            ];
        }

        private static FormattableString[] GetSizeAndCountLines(ELFHeader header)
        {
            return
            [
                $"  本头的大小:       {ELFHeaderDescriptions.GetHeaderSize(header)}",
                $"  程序头的大小:     {header.e_phentsize} (bytes)",
                $"  程序头数量:       {header.e_phnum}",
                $"  节头大小:         {header.e_shentsize} (bytes)",
                $"  节头数量:         {header.e_shnum}",
                $"  字符串表索引节头: {header.e_shstrndx}"
            ];
        }

        private static string FormatLine(FormattableString line)
        {
            return line.ToString(CultureInfo.InvariantCulture);
        }

        private static string GetDataDescription(byte data)
        {
            // EI_DATA=0 或非法值与 readelf 一致显示 none，不能误标为 big endian。
            return data switch
            {
                (byte)ELFData.LSB => "2's complement, little endian",
                (byte)ELFData.MSB => "2's complement, big endian",
                _ => "none"
            };
        }

        private static string GetMagicString(ELFHeader header)
        {
            // 包含全部 e_ident 字节，统一以空格分隔，不添加首尾空格。
            return string.Join(" ", GetIdentificationBytes(header).Select(FormatMagicByte));
        }

        private static IEnumerable<byte> GetIdentificationBytes(ELFHeader header)
        {
            return new byte[]
            {
                header.EI_MAG0, header.EI_MAG1, header.EI_MAG2, header.EI_MAG3,
                header.EI_CLASS, header.EI_DATA, header.EI_VERSION,
                header.EI_OSABI, header.EI_ABIVERSION
            }.Concat(header.EI_PAD);
        }

        private static string FormatMagicByte(byte value)
        {
            return value.ToString("X2", CultureInfo.InvariantCulture);
        }
    }
}
