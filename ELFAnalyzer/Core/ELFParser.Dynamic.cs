using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MyTool.ELFAnalyzer.Models;

namespace MyTool.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        private void ReadDynamicEntries(BinaryReader reader)
        {
            // Find the dynamic section
            int dynamicSectionIndex = -1;
            if (_is64Bit)
            {
                for (int i = 0; i < _sectionHeaders64?.Count; i++)
                {
                    if (_sectionHeaders64[i].sh_type == (uint)SectionType.SHT_DYNAMIC)
                    {
                        dynamicSectionIndex = i;
                        break;
                    }
                }
                
                if (dynamicSectionIndex != -1 && _sectionHeaders64 != null)
                {
                    var dynSection = _sectionHeaders64[dynamicSectionIndex];
                    reader.BaseStream.Seek((long)dynSection.sh_offset, SeekOrigin.Begin);
                    
                    int entryCount = (int)(dynSection.sh_size / dynSection.sh_entsize);
                    _dynamicEntries64 = new List<ELFDynamic64>(entryCount);
                    
                    for (int i = 0; i < entryCount; i++)
                    {
                        var entry = new ELFDynamic64
                        {
                            d_tag = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadInt64LE(reader) : ReadInt64BE(reader),
                            d_val = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader)
                        };
                        _dynamicEntries64.Add(entry);
                    }
                }
            }
            else
            {
                for (int i = 0; i < _sectionHeaders32?.Count; i++)
                {
                    if (_sectionHeaders32[i].sh_type == (uint)SectionType.SHT_DYNAMIC)
                    {
                        dynamicSectionIndex = i;
                        break;
                    }
                }
                
                if (dynamicSectionIndex != -1 && _sectionHeaders32 != null)
                {
                    var dynSection = _sectionHeaders32[dynamicSectionIndex];
                    reader.BaseStream.Seek(dynSection.sh_offset, SeekOrigin.Begin);
                    
                    int entryCount = (int)(dynSection.sh_size / dynSection.sh_entsize);
                    _dynamicEntries32 = new List<ELFDynamic32>(entryCount);
                    
                    for (int i = 0; i < entryCount; i++)
                    {
                        var entry = new ELFDynamic32
                        {
                            d_tag = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadInt32LE(reader) : ReadInt32BE(reader),
                            d_val = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader)
                        };
                        _dynamicEntries32.Add(entry);
                    }
                }
            }
        }

        private static long ReadInt64LE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8);
            if (BitConverter.IsLittleEndian) return BitConverter.ToInt64(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        private static long ReadInt64BE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8);
            if (!BitConverter.IsLittleEndian) return BitConverter.ToInt64(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        private static int ReadInt32LE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian) return BitConverter.ToInt32(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        private static int ReadInt32BE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            if (!BitConverter.IsLittleEndian) return BitConverter.ToInt32(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static string? GetDynamicTagDescription(long dTag)
        {
            if (Enum.IsDefined(typeof(DynamicTag), dTag))
            {
                return Enum.GetName(typeof(DynamicTag), dTag);
            }
            return "DT_UNKNOWN";
        }

        public static string? GetDynamicFlagDescription(uint flags)
        {
            var descriptions = new List<string>();
            
            if ((flags & (uint)DynamicFlags.DF_ORIGIN) != 0) descriptions.Add("ORIGIN");
            if ((flags & (uint)DynamicFlags.DF_SYMBOLIC) != 0) descriptions.Add("SYMBOLIC");
            if ((flags & (uint)DynamicFlags.DF_TEXTREL) != 0) descriptions.Add("TEXTREL");
            if ((flags & (uint)DynamicFlags.DF_BIND_NOW) != 0) descriptions.Add("BIND_NOW");
            if ((flags & (uint)DynamicFlags.DF_STATIC_TLS) != 0) descriptions.Add("STATIC_TLS");
            
            return string.Join(", ", descriptions);
        }

        public static string? GetProgramHeaderType(uint pType)
        {
            if (Enum.IsDefined(typeof(ProgramHeaderType), pType))
            {
                return Enum.GetName(typeof(ProgramHeaderType), pType);
            }
            return "PT_UNKNOWN";
        }

        public static string? GetProgramHeaderFlags(uint pFlags)
        {
            var descriptions = new List<string>();
            
            if ((pFlags & (uint)ProgramHeaderFlags.PF_R) != 0) descriptions.Add("R");
            if ((pFlags & (uint)ProgramHeaderFlags.PF_W) != 0) descriptions.Add("W");
            if ((pFlags & (uint)ProgramHeaderFlags.PF_X) != 0) descriptions.Add("E");
            
            return string.Join("", descriptions);
        }

        public static string? GetSectionType(uint shType)
        {
            if (Enum.IsDefined(typeof(SectionType), shType))
            {
                return Enum.GetName(typeof(SectionType), shType);
            }
            return "SHT_UNKNOWN";
        }

        public static string? GetSectionFlags(ulong shFlags, bool is64Bit)
        {
            var descriptions = new List<string>();
            
            if (is64Bit)
            {
                if ((shFlags & (ulong)SectionFlags.SHF_WRITE) != 0) descriptions.Add("W");
                if ((shFlags & (ulong)SectionFlags.SHF_ALLOC) != 0) descriptions.Add("A");
                if ((shFlags & (ulong)SectionFlags.SHF_EXECINSTR) != 0) descriptions.Add("X");
                if ((shFlags & (ulong)SectionFlags.SHF_MERGE) != 0) descriptions.Add("M");
                if ((shFlags & (ulong)SectionFlags.SHF_STRINGS) != 0) descriptions.Add("S");
                if ((shFlags & (ulong)SectionFlags.SHF_INFO_LINK) != 0) descriptions.Add("I");
                if ((shFlags & (ulong)SectionFlags.SHF_LINK_ORDER) != 0) descriptions.Add("L");
                if ((shFlags & (ulong)SectionFlags.SHF_OS_NONCONFORMING) != 0) descriptions.Add("O");
                if ((shFlags & (ulong)SectionFlags.SHF_GROUP) != 0) descriptions.Add("G");
                if ((shFlags & (ulong)SectionFlags.SHF_TLS) != 0) descriptions.Add("T");
                if ((shFlags & (ulong)SectionFlags.SHF_COMPRESSED) != 0) descriptions.Add("C");
            }
            else
            {
                uint flags32 = (uint)shFlags;
                if ((flags32 & (uint)SectionFlags.SHF_WRITE) != 0) descriptions.Add("W");
                if ((flags32 & (uint)SectionFlags.SHF_ALLOC) != 0) descriptions.Add("A");
                if ((flags32 & (uint)SectionFlags.SHF_EXECINSTR) != 0) descriptions.Add("X");
                if ((flags32 & (uint)SectionFlags.SHF_MERGE) != 0) descriptions.Add("M");
                if ((flags32 & (uint)SectionFlags.SHF_STRINGS) != 0) descriptions.Add("S");
                if ((flags32 & (uint)SectionFlags.SHF_INFO_LINK) != 0) descriptions.Add("I");
                if ((flags32 & (uint)SectionFlags.SHF_LINK_ORDER) != 0) descriptions.Add("L");
                if ((flags32 & (uint)SectionFlags.SHF_OS_NONCONFORMING) != 0) descriptions.Add("O");
                if ((flags32 & (uint)SectionFlags.SHF_GROUP) != 0) descriptions.Add("G");
                if ((flags32 & (uint)SectionFlags.SHF_TLS) != 0) descriptions.Add("T");
                if ((flags32 & (uint)SectionFlags.SHF_COMPRESSED) != 0) descriptions.Add("C");
            }
            
            return string.Join("", descriptions);
        }
    }
}