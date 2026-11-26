using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Presentation;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    // 兼容界面调用入口；解析由 Core 承担，显示转换由 Presentation 承担。
    internal static class ELFHeaderHelper
    {
        internal static string GetFormattedELFHeaderInfo(ELFParser parser) => ELFHeaderPresentation.GetFormattedELFHeaderInfo(parser);
    }
}
