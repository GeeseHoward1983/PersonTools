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
            bool isLittleEndian = true;
            // Read ELF header
            _header = ReadELFHeader(reader, ref _is64Bit, ref isLittleEndian);

            // Read program headers
            ReadProgramHeaders(reader, isLittleEndian);

            // Read section headers
            ReadSectionHeaders(reader, isLittleEndian);

            // Read symbol tables if present
            ReadSymbolTables(reader, isLittleEndian);

            // Read dynamic entries if present
            ReadDynamicEntries(reader, isLittleEndian);

            // Read version information if present
            ReadVersionInformation();
        }

        private static ELFHeader ReadELFHeader(BinaryReader reader, ref bool is64Bit, ref bool isLittleEndian)
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

        private void ReadProgramHeaders(BinaryReader reader, bool isLittleEndian)
        {
            if (_header.e_phnum == 0) return;

            reader.BaseStream.Seek((long)_header.e_phoff, SeekOrigin.Begin);

            _programHeaders = [];
            for (ushort i = 0; i < _header.e_phnum; i++)
            {
                var ph = new ELFProgramHeader
                {
                    p_type = ELFParserUtils.ReadUInt32(reader, isLittleEndian)
                };
                if (_is64Bit)
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
                _programHeaders.Add(ph);
            }
        }

        private void ReadSectionHeaders(BinaryReader reader, bool isLittleEndian)
        {
            if (_header.e_shnum == 0) return;

            reader.BaseStream.Seek((long)_header.e_shoff, SeekOrigin.Begin);

            _sectionHeaders = [];
            for (int i = 0; i < _header.e_shnum; i++)
            {
                var sh = new Models.ELFSectionHeader
                {
                    sh_name = ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_type = ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_flags = _is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_addr = _is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_offset = _is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_size = _is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_link = ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_info = ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_addralign = _is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian),
                    sh_entsize = _is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian)
                };
                _sectionHeaders.Add(sh);
            }
        }
    }
}