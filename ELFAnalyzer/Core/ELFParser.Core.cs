using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    public class ELFParser
    {
        private readonly byte[] _fileData;
        private readonly Dictionary<SectionType, uint> _linkedStrTabIdx = [];

        private bool _is64Bit;

        public ELFHeader Header => _header;
        public List<ELFProgramHeader>? ProgramHeaders { get; set; } = [];
        public List<Models.ELFSectionHeader> SectionHeaders { get; set; } = [];
        public Dictionary<SectionType, List<ELFSymbol>> Symbols = [];
        public List<ELFDynamic> DynamicEntries { get; set; } = [];
        public byte[] FileData => _fileData;

        public bool Is64Bit => _is64Bit;

        public Dictionary<SectionType, uint> LinkedStrTabIdx => _linkedStrTabIdx;
        public ushort[] VersionSymbols { get; set; } = [];
        public Dictionary<ushort, string> VersionDefinitions { get; set; } = [];
        public Dictionary<ushort, string> VersionDependencies { get; set; } = [];

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
            _header = ELFHeaderInfo.ReadELFHeader(reader, ref _is64Bit, ref isLittleEndian);

            // Read program headers
            ELFProgramHeaderInfo.ReadProgramHeaders(this, reader, isLittleEndian);

            // Read section headers
            ELFSectionHeader.ReadSectionHeaders(this, reader, isLittleEndian);

            // Read symbol tables if present
            SymbleTable.ReadSymbolTables(this, reader, isLittleEndian);

            // Read dynamic entries if present
            Dynamic.ReadDynamicEntries(this, reader, isLittleEndian);

            // Read version information if present
            VersionSymbleTable.ReadVersionInformation(this);
        }



    }
}