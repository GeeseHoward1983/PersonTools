using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.ELFAnalyzer.Presentation;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    // 兼容界面调用入口；解析由 Core 承担，显示转换由 Presentation 承担。
    internal static class ProgramHeaderHelper
    {
        internal static List<ProgramHeaderInfo> GetProgramHeaderInfoList(ELFParser parser) => ELFProgramPresentation.GetProgramHeaderInfoList(parser);

        internal static string GetSectionToSegmentMappingInfo(ELFParser parser) => ELFProgramPresentation.GetSectionToSegmentMappingInfo(parser);

        internal static string GetInterpreterInfo(ELFParser parser) => ELFInterpreterReader.GetInterpreterInfo(parser);
    }
}
