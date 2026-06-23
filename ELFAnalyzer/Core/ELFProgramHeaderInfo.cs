using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Utils;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFProgramHeaderInfo
    {
        public static string GetProgramHeaderType(uint pType)
        {
            return ELFParserUtils.GetTypeName(typeof(ProgramHeaderType), pType, "");
        }

        public static string GetProgramHeaderFlags(uint pFlags)
        {
            List<string> descriptions = [];

            if ((pFlags & (uint)ProgramHeaderFlags.PF_R) != 0)
            {
                descriptions.Add("R");
            }

            if ((pFlags & (uint)ProgramHeaderFlags.PF_W) != 0)
            {
                descriptions.Add("W");
            }

            if ((pFlags & (uint)ProgramHeaderFlags.PF_X) != 0)
            {
                descriptions.Add("E");
            }

            return ConvertUtils.EnumerableToString("", descriptions);
        }

        internal static void ReadProgramHeaders(ELFParser parser, BinaryReader reader, bool isLittleEndian)
        {
            if (parser.Header.e_phnum == 0)
            {
                return;
            }

            // 安全：校验程序头表起始偏移落在文件内；畸形 e_phoff/e_phnum 在越界 Seek 后会令
            // ReadExactly 抛 EndOfStreamException 中止整份分析，这里提前拦截
            if (parser.Header.e_phoff >= (ulong)parser.FileData.Length)
            {
                return;
            }

            // 每个程序头项的字节数（按位数固定），用于循环内逐项校验剩余空间
            int phEntrySize = parser.Is64Bit ? ELFConstants.ProgramHeaderSize64 : ELFConstants.ProgramHeaderSize32;
            reader.BaseStream.Seek((long)parser.Header.e_phoff, SeekOrigin.Begin);

            parser.ProgramHeaders = [];
            for (ushort i = 0; i < parser.Header.e_phnum; i++)
            {
                // 剩余字节不足一个完整程序头项时停止，避免越界读取（畸形 e_phnum 大于实际表项数）
                if (reader.BaseStream.Position + phEntrySize > reader.BaseStream.Length)
                {
                    break;
                }

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
