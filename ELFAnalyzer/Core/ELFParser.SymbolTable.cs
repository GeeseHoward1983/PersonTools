using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MyTool.ELFAnalyzer.Models;

namespace MyTool.ELFAnalyzer.Core
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
                    }
                }
            }
        }

        public string? GetSymbolName(ELFSymbol32 symbol)
        {
            if (_sectionHeaders32 == null || _sectionHeaders32.Count <= _header.e_shstrndx) return string.Empty;
            
            var strSection = _sectionHeaders32[_header.e_shstrndx];
            var strData = new byte[strSection.sh_size];
            Array.Copy(_fileData, (int)strSection.sh_offset, strData, 0, (int)strData.Length);
            
            int offset = (int)symbol.st_name;
            if (offset >= strData.Length) return string.Empty;
            
            return ExtractStringFromBytes(strData, offset);
        }

        public string? GetSymbolName(ELFSymbol64 symbol)
        {
            if (_sectionHeaders64 == null || _sectionHeaders64.Count <= _header.e_shstrndx) return string.Empty;
            
            var strSection = _sectionHeaders64[_header.e_shstrndx];
            var strData = new byte[strSection.sh_size];
            Array.Copy(_fileData, (int)strSection.sh_offset, strData, 0, (int)strData.Length);
            
            int offset = (int)symbol.st_name;
            if (offset >= strData.Length) return string.Empty;
            
            return ExtractStringFromBytes(strData, offset);
        }

        public string? GetSectionName(int index)
        {
            if (_is64Bit)
            {
                if (_sectionHeaders64 == null || index >= _sectionHeaders64.Count) return string.Empty;
                
                var strSection = _sectionHeaders64[_header.e_shstrndx];
                var strData = new byte[strSection.sh_size];
                Array.Copy(_fileData, (int)strSection.sh_offset, strData, 0, (int)strData.Length);
                
                var section = _sectionHeaders64[index];
                int offset = (int)section.sh_name;
                if (offset >= strData.Length) return string.Empty;
                
                return ExtractStringFromBytes(strData, offset);
            }
            else
            {
                if (_sectionHeaders32 == null || index >= _sectionHeaders32.Count) return string.Empty;
                
                var strSection = _sectionHeaders32[_header.e_shstrndx];
                var strData = new byte[strSection.sh_size];
                Array.Copy(_fileData, (int)strSection.sh_offset, strData, 0, (int)strData.Length);
                
                var section = _sectionHeaders32[index];
                int offset = (int)section.sh_name;
                if (offset >= strData.Length) return string.Empty;
                
                return ExtractStringFromBytes(strData, offset);
            }
        }

        private static string? ExtractStringFromBytes(byte[] data, int startOffset)
        {
            int endOffset = startOffset;
            while (endOffset < data.Length && data[endOffset] != 0)
            {
                endOffset++;
            }

            if (endOffset > startOffset)
            {
                return Encoding.UTF8.GetString(data, startOffset, endOffset - startOffset);
            }
            return string.Empty;
        }

        public static string? GetSymbolType(byte stInfo)
        {
            byte type = (byte)(stInfo & 0x0F);
            if (Enum.IsDefined(typeof(SymbolType), type))
            {
                return Enum.GetName(typeof(SymbolType), type)?.Replace("STT_", "");
            }
            return "UNKNOWN";
        }

        public static string? GetSymbolBinding(byte stInfo)
        {
            byte binding = (byte)(stInfo >> 4);
            if (Enum.IsDefined(typeof(SymbolBinding), binding))
            {
                return Enum.GetName(typeof(SymbolBinding), binding)?.Replace("STB_", "");
            }
            return "UNKNOWN";
        }

        public static string? GetSymbolVisibility(byte stOther)
        {
            byte visibility = (byte)(stOther & 0x03);
            if (Enum.IsDefined(typeof(SymbolVisibility), visibility))
            {
                return Enum.GetName(typeof(SymbolVisibility), visibility)?.Replace("STV_", "");
            }
            return "UNKNOWN";
        }
    }
}