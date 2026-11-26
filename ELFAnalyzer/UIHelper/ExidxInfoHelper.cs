using PersonalTools.ELFAnalyzer.Core;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class ExidxInfoHelper
    {
        internal static string GetExidxInfo(ELFParser Parser) => ELFExidxInfo.GetFormattedExidxInfo(Parser);
    }
}