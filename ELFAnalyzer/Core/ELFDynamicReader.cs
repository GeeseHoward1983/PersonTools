using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFDynamicReader
    {
        internal static void ReadDynamicEntries(ELFParser parser, BinaryReader reader, bool isLittleEndian)
        {
            for (int i = 0; i < parser.SectionHeaders?.Count && parser.SectionHeaders[i].sh_type != (uint)SectionType.SHT_DYNAMIC; i++)
            {
                ReadDynamicEntries2(parser, reader, isLittleEndian, parser.SectionHeaders[i]);
            }
        }

        internal static void ReadDynamicEntries2(ELFParser parser, BinaryReader reader, bool isLittleEndian, ELFSectionHeader dynSection)
        { 
            if (dynSection.sh_entsize switch
            {
                0 => false,
                _ => ELFParserUtils.IsRangeWithin(dynSection.sh_offset, dynSection.sh_size, (ulong)parser.FileData.Length)
            })
            {
                ReadDynamicEntries3(parser, reader, isLittleEndian, dynSection);
            }
        }

        internal static void ReadDynamicEntries3(ELFParser parser, BinaryReader reader, bool isLittleEndian, ELFSectionHeader dynSection)
        {
            reader.BaseStream.Seek((long)dynSection.sh_offset, SeekOrigin.Begin);
            ReadDynamicEntries4(parser, reader, isLittleEndian, dynSection.sh_size / (ulong)(parser.Is64Bit ? ELFConstants.DynamicEntrySize64 : ELFConstants.DynamicEntrySize32));
        }

        internal static void ReadDynamicEntries4(ELFParser parser, BinaryReader reader, bool isLittleEndian, ulong entryCount)
        {
            for (ulong i = 0; i < entryCount; i++)
            {
                parser.DynamicEntries.Add(GetNewELFDynamic(ReadInt(reader, isLittleEndian, parser.Is64Bit), ReadUInt(reader, isLittleEndian, parser.Is64Bit)));
            }
        }

        internal static ELFDynamic GetNewELFDynamic(long d_tag, ulong d_val)
        {
            return new()
            {
                d_tag = d_tag,
                d_val = d_val
            };
        }

        internal static long ReadInt(BinaryReader reader, bool isLittleEndian, bool Is64Bit)
        {
             return Is64Bit ? ELFParserUtils.ReadInt64(reader, isLittleEndian) : ELFParserUtils.ReadInt32(reader, isLittleEndian);
        }

        internal static ulong ReadUInt(BinaryReader reader, bool isLittleEndian, bool Is64Bit)
        {
            return Is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian);
        }
    }
}