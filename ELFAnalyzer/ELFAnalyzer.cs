using PersonalTools.ELFAnalyzer.Core;
using System.Text;

namespace PersonalTools.ELFAnalyzer
{
    public partial class ELFAnalyzer
    {
        private readonly ELFParser _parser;

        public ELFAnalyzer(string filePath)
        {
            _parser = new ELFParser(filePath);
        }

        public ELFAnalyzer(byte[] fileData)
        {
            _parser = new ELFParser(fileData);
        }

        private string GetMagicString()
        {
            var magic = new StringBuilder();
            magic.Append($"{_parser.Header.EI_MAG0:X2} ");
            magic.Append($"{_parser.Header.EI_MAG1:X2} ");
            magic.Append($"{_parser.Header.EI_MAG2:X2} ");
            magic.Append($"{_parser.Header.EI_MAG3:X2} ");
            for (int i = 0; i < 7; i++)
            {
                magic.Append($"{_parser.Header.EI_PAD[i]:X2} ");
            }
            return magic.ToString().Trim();
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
    }
}