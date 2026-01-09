using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    public static class ELFSectionHeader
    {
        public static string GetProgramHeaderType(uint pType)
        {
            return ELFParserUtils.GetTypeName(typeof(ProgramHeaderType), pType, "");
        }

        public static string GetProgramHeaderFlags(uint pFlags)
        {
            var descriptions = new List<string>();

            if ((pFlags & (uint)ProgramHeaderFlags.PF_R) != 0) descriptions.Add("R");
            if ((pFlags & (uint)ProgramHeaderFlags.PF_W) != 0) descriptions.Add("W");
            if ((pFlags & (uint)ProgramHeaderFlags.PF_X) != 0) descriptions.Add("E");

            return string.Join("", descriptions);
        }

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
    }
}