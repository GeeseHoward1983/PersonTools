using PersonalTools.ELFAnalyzer.Core;
using System.Text;

namespace PersonalTools.ELFAnalyzer
{
    public partial class ELFAnalyzer
    {
        public readonly ELFParser _parser;

        public ELFAnalyzer(string filePath)
        {
            _parser = new ELFParser(filePath);
        }

        public ELFAnalyzer(byte[] fileData)
        {
            _parser = new ELFParser(fileData);
        }
        
        public string GetFormattedVersionSymbolInfo()
        {
            return VersionSymbleTable.GetFormattedVersionSymbolInfo(_parser);
        }
        
        public string GetFormattedVersionDependencyInfo()
        {
            return VersionSymbleTable.GetFormattedVersionDependencyInfo(_parser);
        }

        public string GetFormattedVersionDefinitionInfo()
        {
            return VersionSymbleTable.GetFormattedVersionDefinitionInfo(_parser);
        }
        
        public string GetFormattedNotesInfo()
        {
            return ELFNoteInfo.GetFormattedNotesInfo(_parser);
        }
    }
}