using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFHeaderInfo
    {
        public static ELFHeader ReadELFHeader(BinaryReader reader, ref bool is64Bit, ref bool isLittleEndian)
        {
            // e_ident 占 16 字节，文件过短直接读取会抛 EndOfStreamException，信息不清晰，提前校验
            if (reader.BaseStream.Length < 16)
            {
                throw new InvalidDataException("文件过短，非有效 ELF 文件");
            }

            ELFHeader header = new()
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

            isLittleEndian = header.EI_DATA == (byte)ELFData.LSB;
            is64Bit = header.EI_CLASS == (byte)ELFClass.BIT64;

            // 魔数合法后按位宽校验完整头长度（ELF32=52、ELF64=64）：文件被截断到头长度之内时，
            // 提前抛清晰的 InvalidDataException，而非在逐字段读取途中抛不清晰的 EndOfStreamException。
            long requiredHeaderSize = is64Bit ? 64 : 52;
            if (reader.BaseStream.Length < requiredHeaderSize)
            {
                throw new InvalidDataException("文件过短，ELF 头不完整");
            }

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

        /// <summary>
        /// Determines if the ELF file uses little-endian byte order.
        /// </summary>
        /// <param name="header">The ELF header to check</param>
        /// <returns>True if the ELF file is little-endian, false otherwise</returns>
        public static bool IsLittleEndian(this ELFHeader header)
        {
            return header.EI_DATA == (byte)ELFData.LSB;
        }
    }
}
