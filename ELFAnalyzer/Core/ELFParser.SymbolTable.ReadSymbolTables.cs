using PersonalTools.ELFAnalyzer.Models;
using System.IO;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        private void ReadSymbolTables(BinaryReader reader)
        {
            if (_is64Bit)
            {
                for (int i = 0; i < _sectionHeaders64?.Count; i++)
                {
                    var section = _sectionHeaders64[i];
                    if (section.sh_type == (uint)SectionType.SHT_SYMTAB || section.sh_type == (uint)SectionType.SHT_DYNSYM)
                    {
                        reader.BaseStream.Seek((long)section.sh_offset, SeekOrigin.Begin);
                        
                        int symbolCount = (int)(section.sh_size / section.sh_entsize);
                        _symbols64 = new List<ELFSymbol64>(symbolCount);
                        
                        for (int j = 0; j < symbolCount; j++)
                        {
                            var symbol = new ELFSymbol64
                            {
                                st_name = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                                st_info = reader.ReadByte(),
                                st_other = reader.ReadByte(),
                                st_shndx = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt16LE(reader) : ReadUInt16BE(reader),
                                st_value = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader),
                                st_size = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader)
                            };
                            _symbols64.Add(symbol);
                        }
                        Symbols64.Add((SectionType)section.sh_type, _symbols64);
                        // 记录符号表关联的字符串表索引
                        _linkedStrTabIdx64.Add((SectionType)section.sh_type, section.sh_link);
                    }
                }
            }
            else
            {
                for (int i = 0; i < _sectionHeaders32?.Count; i++)
                {
                    var section = _sectionHeaders32[i];
                    if (section.sh_type == (uint)SectionType.SHT_SYMTAB || section.sh_type == (uint)SectionType.SHT_DYNSYM)
                    {
                        reader.BaseStream.Seek(section.sh_offset, SeekOrigin.Begin);
                        
                        int symbolCount = (int)(section.sh_size / section.sh_entsize);
                        _symbols32 = new List<ELFSymbol32>(symbolCount);
                        
                        for (int j = 0; j < symbolCount; j++)
                        {
                            var symbol = new ELFSymbol32
                            {
                                st_name = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                                st_value = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                                st_size = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                                st_info = reader.ReadByte(),
                                st_other = reader.ReadByte(),
                                st_shndx = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt16LE(reader) : ReadUInt16BE(reader)
                            };
                            _symbols32.Add(symbol);
                        }
                        Symbols32.Add((SectionType)section.sh_type, _symbols32);

                        // 记录符号表关联的字符串表索引
                        _linkedStrTabIdx32.Add((SectionType)section.sh_type, section.sh_link);
                    }
                }
            }
        }
    }
}