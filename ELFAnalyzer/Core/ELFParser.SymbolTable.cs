using PersonalTools.ELFAnalyzer.Models;
using System.IO;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        // 添加字段记录符号表关联的字符串表索引
        private readonly Dictionary<SectionType, uint> _linkedStrTabIdx32 = [];
        private readonly Dictionary<SectionType, uint> _linkedStrTabIdx64 = [];
        private ushort[]? _versionSymbols32;
        private ushort[]? _versionSymbols64;
        private Dictionary<ushort, string>? _versionDefinitions;
        private Dictionary<ushort, string>? _versionDependencies;
    }
}