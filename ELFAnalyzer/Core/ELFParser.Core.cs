using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        private List<ELFProgramHeader>? _programHeaders;
        private List<Models.ELFSectionHeader>? _sectionHeaders;
        private List<ELFSymbol>? _symbols;
        private List<ELFDynamic>? _dynamicEntries;
        private readonly byte[] _fileData;
        public bool _is64Bit;

        public ELFHeader Header => _header;
        public List<ELFProgramHeader>? ProgramHeaders => _programHeaders;
        public List<Models.ELFSectionHeader>? SectionHeaders => _sectionHeaders;
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
                header.e_type = ELFParserUtils.ReadUInt16LE(reader);
                header.e_machine = ELFParserUtils.ReadUInt16LE(reader);
                header.e_version = ELFParserUtils.ReadUInt32LE(reader);
                header.e_entry = header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? ELFParserUtils.ReadUInt64LE(reader) : ELFParserUtils.ReadUInt32LE(reader);
                header.e_phoff = header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? ELFParserUtils.ReadUInt64LE(reader) : ELFParserUtils.ReadUInt32LE(reader);
                header.e_shoff = header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? ELFParserUtils.ReadUInt64LE(reader) : ELFParserUtils.ReadUInt32LE(reader);
                header.e_flags = ELFParserUtils.ReadUInt32LE(reader);
                header.e_ehsize = ELFParserUtils.ReadUInt16LE(reader);
                header.e_phentsize = ELFParserUtils.ReadUInt16LE(reader);
                header.e_phnum = ELFParserUtils.ReadUInt16LE(reader);
                header.e_shentsize = ELFParserUtils.ReadUInt16LE(reader);
                header.e_shnum = ELFParserUtils.ReadUInt16LE(reader);
                header.e_shstrndx = ELFParserUtils.ReadUInt16LE(reader);
            }
            else // Big endian
            {
                header.e_type = ELFParserUtils.ReadUInt16BE(reader);
                header.e_machine = ELFParserUtils.ReadUInt16BE(reader);
                header.e_version = ELFParserUtils.ReadUInt32BE(reader);
                header.e_entry = header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? ELFParserUtils.ReadUInt64BE(reader) : ELFParserUtils.ReadUInt32BE(reader);
                header.e_phoff = header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? ELFParserUtils.ReadUInt64BE(reader) : ELFParserUtils.ReadUInt32BE(reader);
                header.e_shoff = header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? ELFParserUtils.ReadUInt64BE(reader) : ELFParserUtils.ReadUInt32BE(reader);
                header.e_flags = ELFParserUtils.ReadUInt32BE(reader);
                header.e_ehsize = ELFParserUtils.ReadUInt16BE(reader);
                header.e_phentsize = ELFParserUtils.ReadUInt16BE(reader);
                header.e_phnum = ELFParserUtils.ReadUInt16BE(reader);
                header.e_shentsize = ELFParserUtils.ReadUInt16BE(reader);
                header.e_shnum = ELFParserUtils.ReadUInt16BE(reader);
                header.e_shstrndx = ELFParserUtils.ReadUInt16BE(reader);
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
                var ph = new ELFProgramHeader
                {
                    p_type = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader)
                };
                if (_is64Bit)
                {
                    ph.p_flags = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader);
                    ph.p_offset = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt64LE(reader) : ELFParserUtils.ReadUInt64BE(reader);
                    ph.p_vaddr = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt64LE(reader) : ELFParserUtils.ReadUInt64BE(reader);
                    ph.p_paddr = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt64LE(reader) : ELFParserUtils.ReadUInt64BE(reader);
                    ph.p_filesz = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt64LE(reader) : ELFParserUtils.ReadUInt64BE(reader);
                    ph.p_memsz = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt64LE(reader) : ELFParserUtils.ReadUInt64BE(reader);
                    ph.p_align = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt64LE(reader) : ELFParserUtils.ReadUInt64BE(reader);
                }
                else
                {
                    ph.p_offset = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader);
                    ph.p_vaddr = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader);
                    ph.p_paddr = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader);
                    ph.p_filesz = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader);
                    ph.p_memsz = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader);
                    ph.p_flags = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader);
                    ph.p_align = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader);
                }
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
                var sh = new Models.ELFSectionHeader
                {
                    sh_name = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader),
                    sh_type = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader),
                    sh_flags = _is64Bit ? _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt64LE(reader) : ELFParserUtils.ReadUInt64BE(reader) : _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader),
                    sh_addr = _is64Bit ? _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt64LE(reader) : ELFParserUtils.ReadUInt64BE(reader) : _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader),
                    sh_offset = _is64Bit ? _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt64LE(reader) : ELFParserUtils.ReadUInt64BE(reader) : _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader),
                    sh_size = _is64Bit ? _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt64LE(reader) : ELFParserUtils.ReadUInt64BE(reader) : _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader),
                    sh_link = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader),
                    sh_info = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader),
                    sh_addralign = _is64Bit ? _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt64LE(reader) : ELFParserUtils.ReadUInt64BE(reader) : _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.    ReadUInt32BE(reader),
                    sh_entsize = _is64Bit ? _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt64LE(reader) : ELFParserUtils.ReadUInt64BE(reader) : _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ELFParserUtils.ReadUInt32LE(reader) : ELFParserUtils.ReadUInt32BE(reader)
                };
                _sectionHeaders.Add(sh);
            }
        }
    }
}