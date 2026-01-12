using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    public static class ELFSectionHeader
    {
        public static string GetSectionType(uint shType)
        {
            return ELFParserUtils.GetTypeName(typeof(SectionType), shType, "");
        }

        public static string GetSectionFlags(ulong shFlags)
        {
            string sectionFlags = "";

            if ((shFlags & (ulong)SectionFlags.SHF_WRITE) != 0) sectionFlags += "W";
            if ((shFlags & (ulong)SectionFlags.SHF_ALLOC) != 0) sectionFlags += "A";
            if ((shFlags & (ulong)SectionFlags.SHF_EXECINSTR) != 0) sectionFlags += "X";
            if ((shFlags & (ulong)SectionFlags.SHF_MERGE) != 0) sectionFlags += "M";
            if ((shFlags & (ulong)SectionFlags.SHF_STRINGS) != 0) sectionFlags += "S";
            if ((shFlags & (ulong)SectionFlags.SHF_INFO_LINK) != 0) sectionFlags += "I";
            if ((shFlags & (ulong)SectionFlags.SHF_LINK_ORDER) != 0) sectionFlags += "L";
            if ((shFlags & (ulong)SectionFlags.SHF_OS_NONCONFORMING) != 0) sectionFlags += "O";
            if ((shFlags & (ulong)SectionFlags.SHF_GROUP) != 0) sectionFlags += "G";
            if ((shFlags & (ulong)SectionFlags.SHF_TLS) != 0) sectionFlags += "T";
            if ((shFlags & (ulong)SectionFlags.SHF_COMPRESSED) != 0) sectionFlags += "C";

            return sectionFlags;
        }

        public static void ReadSectionHeaders(ELFParser parser, BinaryReader reader, bool isLittleEndian)
        {
            if (parser.Header.e_shnum == 0) return;

            reader.BaseStream.Seek((long)parser.Header.e_shoff, SeekOrigin.Begin);

            parser.SectionHeaders = [];
            for (int i = 0; i < parser.Header.e_shnum; i++)
            {
                var sh = new Models.ELFSectionHeader
                {
                    sh_name = ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_type = ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_flags = parser.Is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_addr = parser.Is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_offset = parser.Is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_size = parser.Is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_link = ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_info = ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_addralign = parser.Is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_entsize = parser.Is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian)
                };
                parser.SectionHeaders.Add(sh);
            }
        }

    }
}