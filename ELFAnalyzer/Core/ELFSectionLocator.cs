
namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFSectionLocator
    {
        internal static int FindIndex(ELFParser parser, Func<int, bool> matches)
        {
            return Enumerable.Range(0, parser.SectionHeaders?.Count ?? 0).FirstOrDefault(matches, -1);
        }

        internal static int FindByName(ELFParser parser, string name)
        {
            return FindIndex(parser, index => ELFSymbolNameResolver.GetSectionName(parser, index) == name);
        }
    }
}
