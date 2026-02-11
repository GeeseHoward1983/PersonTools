using PersonalTools.ELFAnalyzer.Core;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    public class ExidxInfoHelper
    {
        public static string GetExidxInfo(ELFParser Parser)
        {
            return ELFExidxInfo.GetFormattedExidxInfo(Parser);
        }
    }
}