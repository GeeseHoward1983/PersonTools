
namespace PersonalTools.ELFAnalyzer.Models
{
    // 找不到表和表内空字符串必须区分：前者显示空值，后者由展示层回退显示偏移。
    internal readonly record struct ELFDynamicStringData(bool HasTable, string Value);
}
