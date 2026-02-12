namespace PersonalTools.ELFAnalyzer.Core
{
    internal static partial class VersionSymbleTable
    {
        // 解析版本信息
        internal static void ReadVersionInformation(ELFParser parser)
        {
            // 初始化版本定义和依赖字典
            parser.VersionDefinitions = [];
            parser.VersionDependencies = [];

            // 解析版本符号表
            ParseVersionSymbolTable(parser);

            // 解析版本定义
            ParseVersionDefinitions(parser);

            // 解析版本需求
            ParseVersionDependencies(parser);
        }
    }
}