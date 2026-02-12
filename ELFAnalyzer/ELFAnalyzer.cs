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
            return VersionSymbleTable.GetFormattedVersionSymbolInfo(Parser);
        }

        public string GetFormattedVersionDependencyInfo()
        {
            return VersionSymbleTable.GetFormattedVersionDependencyInfo(Parser);
        }

        public string GetFormattedVersionDefinitionInfo()
        {
            return VersionSymbleTable.GetFormattedVersionDefinitionInfo(Parser);
        }

        public string GetFormattedNotesInfo()
        {
            return ELFNoteInfo.GetFormattedNotesInfo(Parser);
        }
    }
}