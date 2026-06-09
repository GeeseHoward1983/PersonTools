using PersonalTools.ELFAnalyzer.Core;

namespace PersonalTools.ELFAnalyzer
{
    internal sealed class ELFAnalyzer
    {
        internal ELFParser Parser { get; }

        public ELFAnalyzer(string filePath)
        {
            Parser = new ELFParser(filePath);
        }

        public ELFAnalyzer(byte[] fileData)
        {
            Parser = new ELFParser(fileData);
        }

        public string GetFormattedVersionSymbolInfo()
        {
            return VersionSymbolFormatter.GetFormattedVersionSymbolInfo(Parser);
        }

        public string GetFormattedVersionDependencyInfo()
        {
            return VersionSymbolFormatter.GetFormattedVersionDependencyInfo(Parser);
        }

        public string GetFormattedVersionDefinitionInfo()
        {
            return VersionSymbolFormatter.GetFormattedVersionDefinitionInfo(Parser);
        }

        public string GetFormattedNotesInfo()
        {
            return ELFNoteInfo.GetFormattedNotesInfo(Parser);
        }
    }
}