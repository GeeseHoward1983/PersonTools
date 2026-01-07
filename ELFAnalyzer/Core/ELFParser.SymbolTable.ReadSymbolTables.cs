using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        private void ReadSymbolTables(BinaryReader reader)
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
                            st_name = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader)
                        };
                        if (_is64Bit)
                        {
                            symbol.st_info = reader.ReadByte();
                            symbol.st_other = reader.ReadByte();
                            symbol.st_shndx = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt16LE(reader) : ReadUInt16BE(reader);
                            symbol.st_value = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader);
                            symbol.st_size = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader);
                        }
                        else
                        {

                            symbol.st_value = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader);
                            symbol.st_size = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader);
                            symbol.st_info = reader.ReadByte();
                            symbol.st_other = reader.ReadByte();
                            symbol.st_shndx = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt16LE(reader) : ReadUInt16BE(reader);
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