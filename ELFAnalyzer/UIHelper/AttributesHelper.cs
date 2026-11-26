using PersonalTools.ELFAnalyzer.Core;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class AttributesHelper
    {
        internal static string GetAttributeInfo(ELFParser Parser) => ELFAttributeInfo.GetFormattedAttributeInfo(Parser);
    }
}