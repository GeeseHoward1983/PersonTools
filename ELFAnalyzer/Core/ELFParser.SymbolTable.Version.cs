using PersonalTools.ELFAnalyzer.Models;
using System.IO;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        // 解析版本信息
        private void ReadVersionInformation()
        {
            // 初始化版本定义和依赖字典
            _versionDefinitions = [];
            _versionDependencies = [];
            
            // 解析版本符号表
            ParseVersionSymbolTable();
            
            // 解析版本定义
            ParseVersionDefinitions();
            
            // 解析版本需求
            ParseVersionDependencies();
        }
    }
}