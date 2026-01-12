using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    public static partial class ELFHeaderInfo
    {
        public static ELFHeader ReadELFHeader(BinaryReader reader, ref bool is64Bit, ref bool isLittleEndian)
        {
            var header = new ELFHeader
            {
                EI_MAG0 = reader.ReadByte(),
                EI_MAG1 = reader.ReadByte(),
                EI_MAG2 = reader.ReadByte(),
                EI_MAG3 = reader.ReadByte(),
                EI_CLASS = reader.ReadByte(),
                EI_DATA = reader.ReadByte(),
                EI_VERSION = reader.ReadByte(),
                EI_OSABI = reader.ReadByte(),
                EI_ABIVERSION = reader.ReadByte(),
                EI_PAD = reader.ReadBytes(7)
            };

            if (header.EI_MAG0 != 0x7F || header.EI_MAG1 != 0x45 || // 'E'
                header.EI_MAG2 != 0x4C || header.EI_MAG3 != 0x46)   // 'L' 'F'
            {
                throw new InvalidDataException("File is not a valid ELF file");
            }

            isLittleEndian = header.EI_DATA == (byte)ELFData.ELFDATA2LSB;
            is64Bit = header.EI_CLASS == (byte)ELFClass.ELFCLASS64;
            header.e_type = ELFParserUtils.ReadUInt16(reader, isLittleEndian);
            header.e_machine = ELFParserUtils.ReadUInt16(reader, isLittleEndian);
            header.e_version = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
            header.e_entry = is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian);
            header.e_phoff = is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian);
            header.e_shoff = is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian);
            header.e_flags = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
            header.e_ehsize = ELFParserUtils.ReadUInt16(reader, isLittleEndian);
            header.e_phentsize = ELFParserUtils.ReadUInt16(reader, isLittleEndian);
            header.e_phnum = ELFParserUtils.ReadUInt16(reader, isLittleEndian);
            header.e_shentsize = ELFParserUtils.ReadUInt16(reader, isLittleEndian);
            header.e_shnum = ELFParserUtils.ReadUInt16(reader, isLittleEndian);
            header.e_shstrndx = ELFParserUtils.ReadUInt16(reader, isLittleEndian);

            return header;
        }
    }
}
