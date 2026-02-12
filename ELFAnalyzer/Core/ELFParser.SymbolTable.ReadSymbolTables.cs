using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class SymbleTable
    {
        internal static void ReadSymbolTables(ELFParser parser, BinaryReader reader, bool isLittleEndian)
        {
            for (int i = 0; i < parser.SectionHeaders?.Count; i++)
            {
                Models.ELFSectionHeader section = parser.SectionHeaders[i];
                if (section.sh_type is ((uint)SectionType.SHT_SYMTAB) or ((uint)SectionType.SHT_DYNSYM))
                {
                    reader.BaseStream.Seek((long)section.sh_offset, SeekOrigin.Begin);

                    int symbolCount = (int)(section.sh_size / section.sh_entsize);
                    List<ELFSymbol> _symbols = new(symbolCount);

                    for (int j = 0; j < symbolCount; j++)
                    {
                        ELFSymbol symbol = new()
                        {
                            StName = ELFParserUtils.ReadUInt32(reader, isLittleEndian)
                        };
                        if (parser.Is64Bit)
                        {
                            symbol.StInfo = reader.ReadByte();
                            symbol.StOther = reader.ReadByte();
                            symbol.StShndx = ELFParserUtils.ReadUInt16(reader, isLittleEndian);
                            symbol.StValue = ELFParserUtils.ReadUInt64(reader, isLittleEndian);
                            symbol.StSize = ELFParserUtils.ReadUInt64(reader, isLittleEndian);
                        }
                        else
                        {

                            symbol.StValue = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
                            symbol.StSize = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
                            symbol.StInfo = reader.ReadByte();
                            symbol.StOther = reader.ReadByte();
                            symbol.StShndx = ELFParserUtils.ReadUInt16(reader, isLittleEndian);
                        }

                        _symbols.Add(symbol);
                    }
                    parser.Symbols.Add((SectionType)section.sh_type, _symbols);
                    // 记录符号表关联的字符串表索引
                    parser.LinkedStrTabIdx.Add((SectionType)section.sh_type, section.sh_link);
                }
            }
        }
    }
}