using PersonalTools.ELFAnalyzer.Core;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    public class AttributesHelper
    {
        public static string GetAttributeInfo(ELFParser Parser)
        {
            return ELFAttributeInfo.GetFormattedAttributeInfo(Parser);
        }
    }
}