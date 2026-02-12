namespace PersonalTools.PEAnalyzer.Models
{
    // 依赖信息
    internal sealed class DependencyInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ForwardedTo { get; set; } = string.Empty;
        public bool IsForwarded { get; set; }
        public List<DependencyInfo> Dependencies { get; set; } = [];
    }
}