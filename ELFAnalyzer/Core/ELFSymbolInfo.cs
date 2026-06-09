using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFSymbolInfo
    {
        public static string GetSymbolType(byte stInfo)
        {
            byte type = (byte)(stInfo & 0x0F);
            return ELFParserUtils.GetTypeName(typeof(SymbolType), type, "");
        }

        public static string GetSymbolBinding(byte stInfo)
        {
            byte binding = (byte)(stInfo >> 4);
            return ELFParserUtils.GetTypeName(typeof(SymbolBinding), binding, "");
        }

        public static string GetSymbolVisibility(byte stOther)
        {
            byte visibility = (byte)(stOther & 0x03);
            return ELFParserUtils.GetTypeName(typeof(SymbolVisibility), visibility, "");
        }
    }
}