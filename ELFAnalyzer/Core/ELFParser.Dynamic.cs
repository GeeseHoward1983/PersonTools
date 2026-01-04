using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MyTool.ELFAnalyzer.Models;
using PersonalTools.ELFAnalyzer.Models;

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
    }
}