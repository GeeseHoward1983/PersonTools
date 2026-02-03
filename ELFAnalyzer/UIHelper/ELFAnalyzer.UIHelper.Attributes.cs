using PersonalTools.ELFAnalyzer.Core;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    public class AttributesHelper
    {
        public static string GetAttributeInfo(ELFParser _parser)
        {
            return ELFAttributeInfo.GetFormattedAttributeInfo(_parser);
        }
    }
}