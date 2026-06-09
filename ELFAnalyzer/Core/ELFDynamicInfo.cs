using PersonalTools.Enums;
using PersonalTools.Utils;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFDynamicInfo
    {
        public static string GetDynamicTagDescription(ulong dTag)
        {
            return ELFParserUtils.GetTypeName(typeof(DynamicTag), dTag, "");
        }

        public static string GetDynamicFlagDescription(uint flags)
        {
            List<string> descriptions = [];

            if ((flags & (uint)DynamicFlags.DF_ORIGIN) != 0)
            {
                descriptions.Add("ORIGIN");
            }

            if ((flags & (uint)DynamicFlags.DF_SYMBOLIC) != 0)
            {
                descriptions.Add("SYMBOLIC");
            }

            if ((flags & (uint)DynamicFlags.DF_TEXTREL) != 0)
            {
                descriptions.Add("TEXTREL");
            }

            if ((flags & (uint)DynamicFlags.DF_BIND_NOW) != 0)
            {
                descriptions.Add("BIND_NOW");
            }

            if ((flags & (uint)DynamicFlags.DF_STATIC_TLS) != 0)
            {
                descriptions.Add("STATIC_TLS");
            }

            return ConvertUtils.EnumerableToString(", ", descriptions);
        }
    }
}