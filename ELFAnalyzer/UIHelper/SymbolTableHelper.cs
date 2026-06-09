using PersonalTools.ELFAnalyzer.Core;
using ELFModels = PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using PersonalTools.ELFAnalyzer.Models;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class SymbolTableHelper
    {
        internal static List<ELFModels.ELFSymbolTableInfo> GetSymbolTableInfoList(ELFParser Parser, SectionType sectionType)
        {
            List<ELFSymbolTableInfo> result = [];

            if (Parser.Symbols != null && Parser.Symbols.Count > 0)
            {
                List<ELFModels.ELFSymbol>? symbols = Parser.Symbols.GetValueOrDefault(sectionType);
                for (int i = 0; i < symbols?.Count; i++)
                {
                    ELFSymbol sym = symbols[i];
                    byte stType = (byte)(sym.StInfo & 0x0F);

                    string name = ELFSymbolNameResolver.GetSymbolName(Parser, sym, sectionType, i);
                    // SECTION 符号 st_name 通常为 0，按 readelf 显示其所属节名
                    if (string.IsNullOrEmpty(name) && stType == (byte)SymbolType.STT_SECTION
                        && sym.StShndx > 0 && sym.StShndx < 0xFF00)
                    {
                        name = ELFSymbolNameResolver.GetSectionName(Parser, sym.StShndx);
                    }

                    result.Add(new ELFModels.ELFSymbolTableInfo
                    {
                        Number = i,
                        Value = $"0x{sym.StValue:x12}",
                        Size = $"{sym.StSize}",
                        Type = StripSymbolPrefix(ELFSymbolInfo.GetSymbolType(sym.StInfo)),
                        Bind = StripSymbolPrefix(ELFSymbolInfo.GetSymbolBinding(sym.StInfo)),
                        Vis = StripSymbolPrefix(ELFSymbolInfo.GetSymbolVisibility(sym.StOther)),
                        Ndx = sym.StShndx switch
                        {
                            0 => "UND",
                            0xFFF1 => "ABS",
                            0xFFF2 => "COM",
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