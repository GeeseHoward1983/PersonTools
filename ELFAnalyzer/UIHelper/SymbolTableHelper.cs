using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.Enums;
using PersonalTools.ELFAnalyzer.Models;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class SymbolTableHelper
    {
        internal static List<ELFSymbolTableInfo> GetSymbolTableInfoList(ELFParser Parser, SectionType sectionType)
        {
            List<ELFSymbolTableInfo> result = [];

            if (Parser.Symbols != null && Parser.Symbols.Count > 0)
            {
                List<ELFSymbol>? symbols = Parser.Symbols.GetValueOrDefault(sectionType);
                for (int i = 0; i < symbols?.Count; i++)
                {
                    ELFSymbol sym = symbols[i];
                    byte stType = (byte)(sym.StInfo & ELFConstants.ST_TYPE_MASK);

                    string name = ELFSymbolNameResolver.GetSymbolName(Parser, sym, sectionType, i);
                    // SECTION 符号 st_name 通常为 0，按 readelf 显示其所属节名
                    if (string.IsNullOrEmpty(name) && stType == (byte)SymbolType.STT_SECTION
                        && sym.StShndx > 0 && sym.StShndx < ELFConstants.SHN_LORESERVE)
                    {
                        name = ELFSymbolNameResolver.GetSectionName(Parser, sym.StShndx);
                    }

                    result.Add(new ELFSymbolTableInfo
                    {
                        Number = i,
                        Value = Parser.Is64Bit ? $"0x{sym.StValue:x16}" : $"0x{sym.StValue:x8}",
                        Size = $"{sym.StSize}",
                        Type = StripSymbolPrefix(ELFSymbolInfo.GetSymbolType(sym.StInfo)),
                        Bind = StripSymbolPrefix(ELFSymbolInfo.GetSymbolBinding(sym.StInfo)),
                        Vis = StripSymbolPrefix(ELFSymbolInfo.GetSymbolVisibility(sym.StOther)),
                        Ndx = sym.StShndx switch
                        {
                            ELFConstants.SHN_UNDEF => "UND",
                            ELFConstants.SHN_ABS => "ABS",
                            ELFConstants.SHN_COMMON => "COM",
                            _ => $"{sym.StShndx}"
                        },
                        Name = name
                    });
                }
            }

            return result;
        }

        // 去掉 STT_/STB_/STV_ 前缀，与 readelf 输出一致
        private static string StripSymbolPrefix(string value)
        {
            if (value.Length > 4 &&
                (value.StartsWith("STT_", StringComparison.Ordinal)
                || value.StartsWith("STB_", StringComparison.Ordinal)
                || value.StartsWith("STV_", StringComparison.Ordinal)))
            {
                return value[4..];
            }
            return value;
        }
    }
}