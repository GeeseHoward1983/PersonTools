using System.Windows.Media.Imaging;

namespace PersonalTools.PEAnalyzer.Models
{
    // PE信息类
    internal sealed class PEInfo
    {
        public string FilePath { get; set; } = string.Empty;
        internal IMAGEDOSHEADER DosHeader { get; set; }
        internal IMAGENTHEADERS NtHeaders { get; set; }
        internal IMAGEOPTIONALHEADER OptionalHeader { get; set; }
        internal List<IMAGESECTIONHEADER> SectionHeaders { get; set; } = [];
        public List<ImportFunctionInfo> ImportFunctions { get; set; } = [];
        public List<ExportFunctionInfo> ExportFunctions { get; set; } = [];
        public List<DependencyInfo> Dependencies { get; set; } = [];
        public List<IconInfo> Icons { get; set; } = [];
        public CLRInfo? CLRInfo { get; set; }
        public PEAdditionalInfo AdditionalInfo { get; set; } = new PEAdditionalInfo();
    }
}