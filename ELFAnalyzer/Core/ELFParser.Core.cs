using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal sealed class ELFParser
    {
        private bool _is64Bit;

        public ELFHeader Header => _header;
        public List<ELFProgramHeader>? ProgramHeaders { get; set; } = [];
        public List<Models.ELFSectionHeader> SectionHeaders { get; set; } = [];
        public Dictionary<SectionType, List<ELFSymbol>> Symbols { get; set; } = [];
        public List<ELFDynamic> DynamicEntries { get; set; } = [];
        public byte[] FileData { get; }

        public bool Is64Bit => _is64Bit;

        public Dictionary<SectionType, uint> LinkedStrTabIdx { get; } = [];
        public ushort[] VersionSymbols { get; set; } = [];
        public Dictionary<ushort, string> VersionDefinitions { get; set; } = [];
        public Dictionary<ushort, string> VersionDependencies { get; set; } = [];

        private ELFHeader _header;

        public ELFParser(string filePath)
        {
            FileData = File.ReadAllBytes(filePath);
            ParseELFFile();
        }

        public ELFParser(byte[] fileData)
        {
            FileData = fileData;
            ParseELFFile();
        }

        private void ParseELFFile()
        {
            using MemoryStream ms = new(FileData);
            using BinaryReader reader = new(ms);
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