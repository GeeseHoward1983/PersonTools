using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        // 添加字段记录符号表关联的字符串表索引
        private readonly Dictionary<SectionType, uint> _linkedStrTabIdx = [];
        private ushort[]? _versionSymbols;
        private Dictionary<ushort, string>? _versionDefinitions;
        private Dictionary<ushort, string>? _versionDependencies;
    }
}