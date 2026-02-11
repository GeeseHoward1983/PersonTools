using PersonalTools.ELFAnalyzer.Core;
using ELFModels = PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using PersonalTools.ELFAnalyzer.Models;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    public class SymbolTableHelper
    {
        public static List<ELFModels.ELFSymbolTableInfo> GetSymbolTableInfoList(ELFParser Parser, SectionType sectionType)
        {
            List<ELFSymbolTableInfo> result = [];

            if (Parser.Symbols != null && Parser.Symbols.Count > 0)
            {
                List<ELFModels.ELFSymbol>? symbols = Parser.Symbols.GetValueOrDefault(sectionType);
                for (int i = 0; i < symbols?.Count; i++)
                {
                    ELFSymbol sym = symbols[i];
                    if (sym.StValue == 0x4064)
                    {
                        ;
                    }
                    result.Add(new ELFModels.ELFSymbolTableInfo
                    {
                        Number = i,
                        Value = $"0x{sym.StValue:x12}",
                        Size = $"{sym.StSize}",
                        Type = ELFSymbolInfo.GetSymbolType(sym.StInfo),
                        Bind = ELFSymbolInfo.GetSymbolBinding(sym.StInfo),
                        Vis = ELFSymbolInfo.GetSymbolVisibility(sym.StOther),
                        Ndx = sym.StShndx switch
                        {
                            0 => "UND",
                            0xFFF1 => "ABS",
                            0xFFF2 => "COM",
                            _ => $"{sym.StShndx}"
                        },
                        Name = SymbleName.GetSymbolName(Parser, sym, sectionType)
                    });
                }
            }

            return result;
        }
    }
}