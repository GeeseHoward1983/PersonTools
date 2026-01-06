using PersonalTools.ELFAnalyzer.Models;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        private List<ELFProgramHeader>? _programHeaders;
        private List<ELFSectionHeader>? _sectionHeaders;
        private List<ELFSymbol>? _symbols;
        private List<ELFDynamic>? _dynamicEntries;
        private readonly byte[] _fileData;
        private bool _is64Bit;

        public ELFHeader Header => _header;
        public List<ELFProgramHeader>? ProgramHeaders => _programHeaders;
        public List<ELFSectionHeader>? SectionHeaders => _sectionHeaders;
        public Dictionary<SectionType, List<ELFSymbol>?> Symbols = [];
        public List<ELFDynamic>? DynamicEntries => _dynamicEntries;
        public byte[] FileData => _fileData;

        private ELFHeader _header;

        public ELFParser(string filePath)
        {
            _fileData = File.ReadAllBytes(filePath);
            ParseELFFile();
        }

        public ELFParser(byte[] fileData)
        {
            _fileData = fileData;
            ParseELFFile();
        }

        private void ParseELFFile()
        {
            using var ms = new MemoryStream(_fileData);
            using var reader = new BinaryReader(ms);
                // Read ELF header
                _header = ReadELFHeader(reader);

                _is64Bit = _header.EI_CLASS == (byte)ELFClass.ELFCLASS64;

                // Read program headers
                ReadProgramHeaders(reader);

                // Read section headers
                ReadSectionHeaders(reader);

                // Read symbol tables if present
                ReadSymbolTables(reader);

                // Read dynamic entries if present
                ReadDynamicEntries(reader);
                
                // Read version information if present
                ReadVersionInformation();
        }

        private static ELFHeader ReadELFHeader(BinaryReader reader)
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

            // Read the rest based on endianness
            if (header.EI_DATA == (byte)ELFData.ELFDATA2LSB)
            {
                header.e_type = ReadUInt16LE(reader);
                header.e_machine = ReadUInt16LE(reader);
                header.e_version = ReadUInt32LE(reader);
                header.e_entry = header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? ReadUInt64LE(reader) : ReadUInt32LE(reader);
                header.e_phoff = header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? ReadUInt64LE(reader) : ReadUInt32LE(reader);
                header.e_shoff = header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? ReadUInt64LE(reader) : ReadUInt32LE(reader);
                header.e_flags = ReadUInt32LE(reader);
                header.e_ehsize = ReadUInt16LE(reader);
                header.e_phentsize = ReadUInt16LE(reader);
                header.e_phnum = ReadUInt16LE(reader);
                header.e_shentsize = ReadUInt16LE(reader);
                header.e_shnum = ReadUInt16LE(reader);
                header.e_shstrndx = ReadUInt16LE(reader);
            }
            else // Big endian
            {
                header.e_type = ReadUInt16BE(reader);
                header.e_machine = ReadUInt16BE(reader);
                header.e_version = ReadUInt32BE(reader);
                header.e_entry = header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? ReadUInt64BE(reader) : ReadUInt32BE(reader);
                header.e_phoff = header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? ReadUInt64BE(reader) : ReadUInt32BE(reader);
                header.e_shoff = header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? ReadUInt64BE(reader) : ReadUInt32BE(reader);
                header.e_flags = ReadUInt32BE(reader);
                header.e_ehsize = ReadUInt16BE(reader);
                header.e_phentsize = ReadUInt16BE(reader);
                header.e_phnum = ReadUInt16BE(reader);
                header.e_shentsize = ReadUInt16BE(reader);
                header.e_shnum = ReadUInt16BE(reader);
                header.e_shstrndx = ReadUInt16BE(reader);
            }

            return header;
        }

        private void ReadProgramHeaders(BinaryReader reader)
        {
            if (_header.e_phnum == 0) return;

            reader.BaseStream.Seek((long)_header.e_phoff, SeekOrigin.Begin);

            _programHeaders = [];
            for (ushort i = 0; i < _header.e_phnum; i++)
            {
                var ph = new ELFProgramHeader();
                ph.p_type = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader);
                if (_is64Bit)
                {
                    ph.p_flags = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader);
                    ph.p_offset = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader);
                    ph.p_vaddr = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader);
                    ph.p_paddr = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader);
                    ph.p_filesz = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader);
                    ph.p_memsz = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader);

                }
                else
                {
                    ph.p_offset = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader);
                    ph.p_vaddr = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader);
                    ph.p_paddr = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader);
                    ph.p_filesz = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader);
                    ph.p_memsz = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader);
                    ph.p_flags = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader);
                }
                ph.p_align = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader);
                _programHeaders.Add(ph);
            }
        }

        private void ReadSectionHeaders(BinaryReader reader)
        {
            if (_header.e_shnum == 0) return;

            reader.BaseStream.Seek((long)_header.e_shoff, SeekOrigin.Begin);

            _sectionHeaders = [];
            for (int i = 0; i < _header.e_shnum; i++)
            {
                var sh = new ELFSectionHeader
                {
                    sh_name = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                    sh_type = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                    sh_flags = _is64Bit ? _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader) : _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                    sh_addr = _is64Bit ? _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader) : _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                    sh_offset = _is64Bit ? _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader) : _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                    sh_size = _is64Bit ? _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader) : _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                    sh_link = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                    sh_info = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                    sh_addralign = _is64Bit ? _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader) : _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                    sh_entsize = _is64Bit ? _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader) : _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader)
                };
                _sectionHeaders.Add(sh);
            }
        }

        private static ushort ReadUInt16LE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2);
            if (BitConverter.IsLittleEndian) return BitConverter.ToUInt16(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        private static ushort ReadUInt16BE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(2);
            if (!BitConverter.IsLittleEndian) return BitConverter.ToUInt16(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        private static uint ReadUInt32LE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian) return BitConverter.ToUInt32(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        private static uint ReadUInt32BE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            if (!BitConverter.IsLittleEndian) return BitConverter.ToUInt32(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        private static ulong ReadUInt64LE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8);
            if (BitConverter.IsLittleEndian) return BitConverter.ToUInt64(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        private static ulong ReadUInt64BE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8);
            if (!BitConverter.IsLittleEndian) return BitConverter.ToUInt64(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }
    }
}