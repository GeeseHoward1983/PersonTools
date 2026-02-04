using PersonalTools.ELFAnalyzer.Core;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    public class ExidxInfoHelper
    {
        public static string GetExidxInfo(ELFParser _parser)
        {
            return ELFExidxInfo.GetFormattedExidxInfo(_parser);
        }
    }
}