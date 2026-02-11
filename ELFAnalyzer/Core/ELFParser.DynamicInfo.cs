using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    public static class ELFDynamicInfo
    {
        public static string GetDynamicTagDescription(ulong dTag)
        {
            return ELFParserUtils.GetTypeName(typeof(DynamicTag), dTag, "");
        }

        public static string GetDynamicFlagDescription(uint flags)
        {
            List<string> descriptions = [];

            if ((flags & (uint)DynamicOptions.DF_ORIGIN) != 0)
            {
                descriptions.Add("ORIGIN");
            }

            if ((flags & (uint)DynamicOptions.DF_SYMBOLIC) != 0)
            {
                descriptions.Add("SYMBOLIC");
            }

            if ((flags & (uint)DynamicOptions.DF_TEXTREL) != 0)
            {
                descriptions.Add("TEXTREL");
            }

            if ((flags & (uint)DynamicOptions.DF_BIND_NOW) != 0)
            {
                descriptions.Add("BIND_NOW");
            }

            if ((flags & (uint)DynamicOptions.DF_STATIC_TLS) != 0)
            {
                descriptions.Add("STATIC_TLS");
            }

            return Utils.EnumerableToString(", ", descriptions);
        }
    }
}