using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.ELFAnalyzer.Presentation;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    // 兼容界面调用入口；解析由 Core 承担，显示转换由 Presentation 承担。
    internal static class SectionHeaderHelper
    {
        internal static List<ELFSectionHeaderInfo> GetSectionHeaderInfoList(ELFParser parser) => ELFSectionPresentation.GetSectionHeaderInfoList(parser);
    }
}
