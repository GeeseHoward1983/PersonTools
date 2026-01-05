using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using System.Text;

namespace PersonalTools.ELFAnalyzer
{
    public partial class ELFAnalyzer
    {
        public List<ELFSymbolTableInfo> GetSymbolTableInfoList(SectionType sectionType)
        {
            var result = new List<ELFSymbolTableInfo>();

            if (_parser.Symbols32 != null && _parser.Symbols32.Count > 0)
            {
                List<ELFSymbol32>? symbols = _parser.Symbols32?.GetValueOrDefault(sectionType);
                for (int i = 0; i < symbols?.Count; i++)
                {
                    var sym = symbols[i];
                    result.Add(new ELFSymbolTableInfo
                    {
                        Number = i,
                        Value = $"0x{sym.st_value:x8}",
                        Size = $"{sym.st_size}",
                        Type = ELFParser.GetSymbolType(sym.st_info) ?? string.Empty,
                        Bind = ELFParser.GetSymbolBinding(sym.st_info) ?? string.Empty,
                        Vis = ELFParser.GetSymbolVisibility(sym.st_other) ?? string.Empty,
                        Ndx = sym.st_shndx == 0 ? "UND" : sym.st_shndx == 0xFFF1 ? "ABS" : sym.st_shndx == 0xFFF2 ? "COM" : $"{sym.st_shndx}",
                        Name = _parser.GetSymbolName(sym, sectionType) ?? string.Empty
                    });
                }
            }
            else if (_parser.Symbols64 != null && _parser.Symbols64.Count > 0)
            {
                List<ELFSymbol64>? symbols = _parser.Symbols64?.GetValueOrDefault(sectionType);
                for (int i = 0; i < symbols?.Count; i++)
                {
                    var sym = symbols[i];
                    result.Add(new ELFSymbolTableInfo
                    {
                        Number = i,
                        Value = $"0x{sym.st_value:x12}",
                        Size = $"{sym.st_size}",
                        Type = ELFParser.GetSymbolType(sym.st_info) ?? string.Empty,
                        Bind = ELFParser.GetSymbolBinding(sym.st_info) ?? string.Empty,
                        Vis = ELFParser.GetSymbolVisibility(sym.st_other) ?? string.Empty,
                        Ndx = sym.st_shndx == 0 ? "UND" : sym.st_shndx == 0xFFF1 ? "ABS" : sym.st_shndx == 0xFFF2 ? "COM" : $"{sym.st_shndx}",
                        Name = _parser.GetSymbolName(sym, sectionType) ?? string.Empty
                    });
                }
            }

            return result;
        }
    }
}