using PersonalTools.ELFAnalyzer.Core;

namespace PersonalTools.ELFAnalyzer
{
    public partial class ELFAnalyzer
    {
        public string GetAttributeInfo()
        {
            return ELFAttributeInfo.GetFormattedAttributeInfo(_parser);
        }
    }
}