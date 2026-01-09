using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    public static class ELFSymbolInfo
    {
        public static string GetSymbolType(byte stInfo)
        {
            byte type = (byte)(stInfo & 0x0F);
            if (Enum.IsDefined(typeof(SymbolType), type))
            {
                return Enum.GetName(typeof(SymbolType), type)?.Replace("STT_", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        public static string GetSymbolBinding(byte stInfo)
        {
            byte binding = (byte)(stInfo >> 4);
            if (Enum.IsDefined(typeof(SymbolBinding), binding))
            {
                return Enum.GetName(typeof(SymbolBinding), binding)?.Replace("STB_", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        public static string GetSymbolVisibility(byte stOther)
        {
            byte visibility = (byte)(stOther & 0x03);
            if (Enum.IsDefined(typeof(SymbolVisibility), visibility))
            {
                return Enum.GetName(typeof(SymbolVisibility), visibility)?.Replace("STV_", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }
    }
}