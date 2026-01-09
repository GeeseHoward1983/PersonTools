using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer
{
    public partial class ELFAnalyzer
    {
        public List<ELFSymbolTableInfo> GetSymbolTableInfoList(SectionType sectionType)
        {
            var result = new List<ELFSymbolTableInfo>();

            if (_parser.Symbols != null && _parser.Symbols.Count > 0)
            {
                List<ELFSymbol>? symbols = _parser.Symbols?.GetValueOrDefault(sectionType);
                for (int i = 0; i < symbols?.Count; i++)
                {
                    var sym = symbols[i];
                    result.Add(new ELFSymbolTableInfo
                    {
                        Number = i,
                        Value = $"0x{sym.st_value:x12}",
                        Size = $"{sym.st_size}",
                        Type = ELFSymbolInfo.GetSymbolType(sym.st_info) ?? string.Empty,
                        Bind = ELFSymbolInfo.GetSymbolBinding(sym.st_info) ?? string.Empty,
                        Vis = ELFSymbolInfo.GetSymbolVisibility(sym.st_other) ?? string.Empty,
                        Ndx = sym.st_shndx == 0 ? "UND" : sym.st_shndx == 0xFFF1 ? "ABS" : sym.st_shndx == 0xFFF2 ? "COM" : $"{sym.st_shndx}",
                        Name = _parser.GetSymbolName(sym, sectionType) ?? string.Empty
                    });
                }
            }

            return result;
        }
    }
}