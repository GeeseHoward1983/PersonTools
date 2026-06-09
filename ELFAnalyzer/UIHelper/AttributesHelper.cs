using PersonalTools.ELFAnalyzer.Core;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class AttributesHelper
    {
        internal static string GetAttributeInfo(ELFParser Parser)
        {
            return ELFAttributeInfo.GetFormattedAttributeInfo(Parser);
        }
    }
}