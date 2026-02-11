using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    public class ELFProgramHeaderInfo
    {
        public static string GetProgramHeaderType(uint pType)
        {
            return ELFParserUtils.GetTypeName(typeof(ProgramHeaderType), pType, "");
        }

        public static string GetProgramHeaderFlags(uint pFlags)
        {
            List<string> descriptions = [];

            if ((pFlags & (uint)ProgramHeaderPermissions.PF_R) != 0)
            {
                descriptions.Add("R");
            }

            if ((pFlags & (uint)ProgramHeaderPermissions.PF_W) != 0)
            {
                descriptions.Add("W");
            }

            if ((pFlags & (uint)ProgramHeaderPermissions.PF_X) != 0)
            {
                descriptions.Add("E");
            }

            return Utils.EnumerableToString("", descriptions);
        }

        public static void ReadProgramHeaders(ELFParser parser, BinaryReader reader, bool isLittleEndian)
        {
            if (parser.Header.e_phnum == 0)
            {
                return;
            }

            reader.BaseStream.Seek((long)parser.Header.e_phoff, SeekOrigin.Begin);

            parser.ProgramHeaders = [];
            for (ushort i = 0; i < parser.Header.e_phnum; i++)
            {
                ELFProgramHeader ph = new()
                {
                    p_type = ELFParserUtils.ReadUInt32(reader, isLittleEndian)
                };
                if (parser.Is64Bit)
                {
                    ph.p_flags = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
                    ph.p_offset = ELFParserUtils.ReadUInt64(reader, isLittleEndian);
                    ph.p_vaddr = ELFParserUtils.ReadUInt64(reader, isLittleEndian);
                    ph.p_paddr = ELFParserUtils.ReadUInt64(reader, isLittleEndian);
                    ph.p_filesz = ELFParserUtils.ReadUInt64(reader, isLittleEndian);
                    ph.p_memsz = ELFParserUtils.ReadUInt64(reader, isLittleEndian);
                    ph.p_align = ELFParserUtils.ReadUInt64(reader, isLittleEndian);
                }
                else
                {
                    ph.p_offset = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
                    ph.p_vaddr = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
                    ph.p_paddr = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
                    ph.p_filesz = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
                    ph.p_memsz = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
                    ph.p_flags = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
                    ph.p_align = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
                }
                parser.ProgramHeaders.Add(ph);
            }
        }

    }
}
