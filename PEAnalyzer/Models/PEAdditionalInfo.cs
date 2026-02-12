namespace PersonalTools.PEAnalyzer.Models
{
    // PE文件附加信息类
    internal sealed class PEAdditionalInfo
    {
        public string Copyright { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string FileDescription { get; set; } = string.Empty;
        public string FileVersion { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductVersion { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string InternalName { get; set; } = string.Empty;
        public string LegalCopyright { get; set; } = string.Empty;
        public string LegalTrademarks { get; set; } = string.Empty;
        public bool IsSigned { get; set; }
        public string CertificateInfo { get; set; } = string.Empty;

        // 翻译信息（来自VarFileInfo）
        public string TranslationInfo { get; set; } = string.Empty;

        // 是否已解析StringTable
        public bool StringTableParsed { get; set; }

        // StringTable结束位置
        public long StringTableEndPosition { get; set; }
    }
}