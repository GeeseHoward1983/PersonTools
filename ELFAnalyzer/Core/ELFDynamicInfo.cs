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

        // DT_FLAGS_1 用 DF_1_* 位命名空间，与 DT_FLAGS 的 DF_* 完全不同，必须单独解码
        // 逐位检测 DynamicFlags1，输出去掉 "DF_1_" 前缀的名（如 NOW/PIE/NODELETE）
        public static string GetDynamicFlag1Description(uint flags)
        {
            List<string> descriptions = [];

            foreach (DynamicFlags1 flag in Enum.GetValues<DynamicFlags1>())
            {
                if ((flags & (uint)flag) != 0)
                {
                    // 去掉枚举名的 "DF_1_" 前缀，对齐 readelf 输出风格
                    descriptions.Add(flag.ToString()["DF_1_".Length..]);
                }
            }

            return ConvertUtils.EnumerableToString(", ", descriptions);
        }
    }
}