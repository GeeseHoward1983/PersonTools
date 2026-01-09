using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        private void ReadDynamicEntries(BinaryReader reader, bool isLittleEndian)
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
                        d_tag = _is64Bit ? ELFParserUtils.ReadInt64(reader, isLittleEndian) : ELFParserUtils.ReadInt32(reader, isLittleEndian),
                        d_val = _is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian)
                    };
                    _dynamicEntries.Add(entry);
                }
            }
        }
    }
}