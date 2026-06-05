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

        // 缓存按索引读取的节数据，避免符号名/重定位解析时反复整表拷贝
        private readonly Dictionary<int, byte[]> _sectionDataCache = [];

        /// <summary>
        /// 读取指定索引节的原始数据（带缓存），调用方不得就地修改返回的数组。
        /// </summary>
        internal byte[] GetSectionData(int sectionIndex)
        {
            if (_sectionDataCache.TryGetValue(sectionIndex, out byte[]? cached))
            {
                return cached;
            }

            Models.ELFSectionHeader section = SectionHeaders[sectionIndex];
            byte[] data = CopySectionData(section);
            _sectionDataCache[sectionIndex] = data;
            return data;
        }

        /// <summary>
        /// 拷贝指定节的原始数据（不缓存），用于只能拿到节结构而没有索引的场景。
        /// </summary>
        internal byte[] CopySectionData(in Models.ELFSectionHeader section)
        {
            byte[] data = new byte[section.sh_size];
            Array.Copy(FileData, (long)section.sh_offset, data, 0, (int)section.sh_size);
            return data;
        }

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