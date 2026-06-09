using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFSectionHeader
    {
        public static string GetSectionType(uint shType)
        {
            return ELFParserUtils.GetTypeName(typeof(SectionType), shType, "");
        }

        // 节标志位 → 字符（顺序与 readelf 一致）
        private static readonly (SectionAttributes Flag, char Char)[] s_sectionFlagChars =
        [
            (SectionAttributes.SHF_WRITE, 'W'),
            (SectionAttributes.SHF_ALLOC, 'A'),
            (SectionAttributes.SHF_EXECINSTR, 'X'),
            (SectionAttributes.SHF_MERGE, 'M'),
            (SectionAttributes.SHF_STRINGS, 'S'),
            (SectionAttributes.SHF_INFO_LINK, 'I'),
            (SectionAttributes.SHF_LINK_ORDER, 'L'),
            (SectionAttributes.SHF_OS_NONCONFORMING, 'O'),
            (SectionAttributes.SHF_GROUP, 'G'),
            (SectionAttributes.SHF_TLS, 'T'),
            (SectionAttributes.SHF_COMPRESSED, 'C'),
        ];

        public static string GetSectionFlags(ulong shFlags)
        {
            string sectionFlags = "";
            foreach ((SectionAttributes flag, char ch) in s_sectionFlagChars)
            {
                if ((shFlags & (ulong)flag) != 0)
                {
                    sectionFlags += ch;
                }
            }
            return sectionFlags;
        }

        internal static void ReadSectionHeaders(ELFParser parser, BinaryReader reader, bool isLittleEndian)
        {
            if (parser.Header.e_shnum == 0)
            {
                return;
            }

            reader.BaseStream.Seek((long)parser.Header.e_shoff, SeekOrigin.Begin);

            parser.SectionHeaders = [];
            for (int i = 0; i < parser.Header.e_shnum; i++)
            {
                Models.ELFSectionHeader sh = new()
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