using PersonalTools.ELFAnalyzer.Models;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        private void ReadDynamicEntries(BinaryReader reader)
        {
            // Find the dynamic section
            int dynamicSectionIndex = -1;
            for (int i = 0; i < _sectionHeaders?.Count; i++)
            {
                if (_sectionHeaders[i].sh_type == (uint)SectionType.SHT_DYNAMIC)
                {
                    dynamicSectionIndex = i;
                    break;
                }
            }

            if (dynamicSectionIndex != -1 && _sectionHeaders != null)
            {
                var dynSection = _sectionHeaders[dynamicSectionIndex];
                reader.BaseStream.Seek((long)dynSection.sh_offset, SeekOrigin.Begin);

                int entryCount = (int)(dynSection.sh_size / dynSection.sh_entsize);
                _dynamicEntries = new List<ELFDynamic>(entryCount);

                for (int i = 0; i < entryCount; i++)
                {
                    var entry = new ELFDynamic
                    {
                        d_tag = _is64Bit ? _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadInt64LE(reader) : ReadInt64BE(reader) : _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadInt32LE(reader) : ReadInt32BE(reader),
                        d_val = _is64Bit ? _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader) : _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader)
                    };
                    _dynamicEntries.Add(entry);
                }
            }
        }
    }
}