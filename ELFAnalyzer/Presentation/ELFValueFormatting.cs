using System.Globalization;

namespace PersonalTools.ELFAnalyzer.Presentation
{
    // 纯显示格式，不处理文件读取、节定位或符号表关联。
    internal static class ELFValueFormatting
    {
        internal static string Number(ulong value)
        {
            return value.ToString(CultureInfo.CurrentCulture);
        }

        internal static string Hex(ulong value, bool is64Bit)
        {
            return value.ToString(is64Bit ? "x16" : "x8", CultureInfo.InvariantCulture);
        }

        internal static string Address(ulong value, bool is64Bit)
        {
            return $"0x{Hex(value, is64Bit)}";
        }

        internal static string DynamicTagValue(long value, bool is64Bit)
        {
            return "0x" + value.ToString(is64Bit ? "x16" : "x8", CultureInfo.InvariantCulture);
        }

    }
}
