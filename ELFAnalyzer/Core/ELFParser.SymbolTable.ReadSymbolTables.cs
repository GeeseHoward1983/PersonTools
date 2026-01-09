using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        private void ReadSymbolTables(BinaryReader reader, bool isLittleEndian)
        {
            for (int i = 0; i < _sectionHeaders?.Count; i++)
            {
                var section = _sectionHeaders[i];
                if (section.sh_type == (uint)SectionType.SHT_SYMTAB || section.sh_type == (uint)SectionType.SHT_DYNSYM)
                {
                    reader.BaseStream.Seek((long)section.sh_offset, SeekOrigin.Begin);

                    int symbolCount = (int)(section.sh_size / section.sh_entsize);
                    _symbols = new List<ELFSymbol>(symbolCount);

                    for (int j = 0; j < symbolCount; j++)
                    {
                        var symbol = new ELFSymbol
                        {
                            st_name = ELFParserUtils.ReadUInt32(reader, isLittleEndian)
                        };
                        if (_is64Bit)
                        {
                            symbol.st_info = reader.ReadByte();
                            symbol.st_other = reader.ReadByte();
                            symbol.st_shndx = ELFParserUtils.ReadUInt16(reader, isLittleEndian);
                            symbol.st_value = ELFParserUtils.ReadUInt64(reader, isLittleEndian);
                            symbol.st_size = ELFParserUtils.ReadUInt64(reader, isLittleEndian);
                        }
                        else
                        {

                            symbol.st_value = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
                            symbol.st_size = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
                            symbol.st_info = reader.ReadByte();
                            symbol.st_other = reader.ReadByte();
                            symbol.st_shndx = ELFParserUtils.ReadUInt16(reader, isLittleEndian);
                        }
                        
                        _symbols.Add(symbol);
                    }
                    Symbols.Add((SectionType)section.sh_type, _symbols);
                    // 记录符号表关联的字符串表索引
                    _linkedStrTabIdx.Add((SectionType)section.sh_type, section.sh_link);
                }
            }
        }
    }
}