using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    public class Dynamic
    {
        public static void ReadDynamicEntries(ELFParser parser, BinaryReader reader, bool isLittleEndian)
        {
            // Find the dynamic section
            int dynamicSectionIndex = -1;
            for (int i = 0; i < parser.SectionHeaders?.Count; i++)
            {
                if (parser.SectionHeaders[i].sh_type == (uint)SectionType.SHT_DYNAMIC)
                {
                    dynamicSectionIndex = i;
                    break;
                }
            }

            if (dynamicSectionIndex != -1 && parser.SectionHeaders != null)
            {
                Models.ELFSectionHeader dynSection = parser.SectionHeaders[dynamicSectionIndex];
                reader.BaseStream.Seek((long)dynSection.sh_offset, SeekOrigin.Begin);

                int entryCount = (int)(dynSection.sh_size / dynSection.sh_entsize);
                parser.DynamicEntries = new List<ELFDynamic>(entryCount);

                for (int i = 0; i < entryCount; i++)
                {
                    ELFDynamic entry = new()
                    {
                        d_tag = parser.Is64Bit ? ELFParserUtils.ReadInt64(reader, isLittleEndian) : ELFParserUtils.ReadInt32(reader, isLittleEndian),
                        d_val = parser.Is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian)
                    };
                    parser.DynamicEntries.Add(entry);
                }
            }
        }
    }
}