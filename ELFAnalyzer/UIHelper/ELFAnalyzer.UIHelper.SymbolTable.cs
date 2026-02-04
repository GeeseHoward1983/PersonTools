using PersonalTools.ELFAnalyzer.Core;
using ELFModels=PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    public class SymbolTableHelper
    {
        public static List<ELFModels.ELFSymbolTableInfo> GetSymbolTableInfoList(ELFParser _parser, SectionType sectionType)
        {
            var result = new List<ELFModels.ELFSymbolTableInfo>();

            if (_parser.Symbols != null && _parser.Symbols.Count > 0)
            {
                List<ELFModels.ELFSymbol>? symbols = _parser.Symbols.GetValueOrDefault(sectionType);
                for (int i = 0; i < symbols?.Count; i++)
                {
                    var sym = symbols[i];
                    if(sym.st_value == 0x4064)
                    {
                        ;
                    }
                    result.Add(new ELFModels.ELFSymbolTableInfo
                    {
                        Number = i,
                        Value = $"0x{sym.st_value:x12}",
                        Size = $"{sym.st_size}",
                        Type = ELFSymbolInfo.GetSymbolType(sym.st_info),
                        Bind = ELFSymbolInfo.GetSymbolBinding(sym.st_info),
                        Vis = ELFSymbolInfo.GetSymbolVisibility(sym.st_other),
                        Ndx = sym.st_shndx switch
                        { 
                            0 => "UND",
                            0xFFF1 => "ABS",
                            0xFFF2 => "COM",
                            _ => $"{sym.st_shndx}"
                        },
                        Name = SymbleName.GetSymbolName(_parser, sym, sectionType)
                    });
                }
            }

            return result;
        }
    }
}