namespace PersonalTools.PEAnalyzer.Models
{
    // PE信息类
    internal sealed class PEInfo
    {
        public string FilePath { get; set; } = string.Empty;
        internal IMAGE_DOS_HEADER DosHeader { get; set; }
        internal IMAGE_NT_HEADERS NtHeaders { get; set; }
        internal IMAGE_OPTIONAL_HEADER OptionalHeader { get; set; }
        internal List<IMAGE_SECTION_HEADER> SectionHeaders { get; set; } = [];
        public List<ImportFunctionInfo> ImportFunctions { get; set; } = [];
        public List<ExportFunctionInfo> ExportFunctions { get; set; } = [];
        public List<DependencyInfo> Dependencies { get; set; } = [];
        public List<IconInfo> Icons { get; set; } = [];
        public CLRInfo? CLRInfo { get; set; }
        public PEAdditionalInfo AdditionalInfo { get; set; } = new PEAdditionalInfo();
    }
}