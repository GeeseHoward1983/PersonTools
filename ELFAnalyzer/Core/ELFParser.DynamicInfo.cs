using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    public static class ELF_DYNAMIC_INFO
    {
        public static string GetDynamicTagDescription(ulong dTag)
        {
            return ELFParserUtils.GetTypeName(typeof(DynamicTag), dTag, "");
        }

        public static string GetDynamicFlagDescription(uint flags)
        {
            var descriptions = new List<string>();

            if ((flags & (uint)DynamicFlags.DF_ORIGIN) != 0) descriptions.Add("ORIGIN");
            if ((flags & (uint)DynamicFlags.DF_SYMBOLIC) != 0) descriptions.Add("SYMBOLIC");
            if ((flags & (uint)DynamicFlags.DF_TEXTREL) != 0) descriptions.Add("TEXTREL");
            if ((flags & (uint)DynamicFlags.DF_BIND_NOW) != 0) descriptions.Add("BIND_NOW");
            if ((flags & (uint)DynamicFlags.DF_STATIC_TLS) != 0) descriptions.Add("STATIC_TLS");

            return string.Join(", ", descriptions);
        }

        public static string GetDynamicFlag1Description(uint flags)
        {
            var descriptions = new List<string>();

            // DT_FLAGS_1 flags values
            if ((flags & 0x00000001) != 0) descriptions.Add("NOW");           // Set RTLD_NOW for this object
            if ((flags & 0x00000002) != 0) descriptions.Add("GLOBAL");        // Set RTLD_GLOBAL for this object
            if ((flags & 0x00000004) != 0) descriptions.Add("GROUP");         // Set RTLD_GROUP for this object
            if ((flags & 0x00000008) != 0) descriptions.Add("NODELETE");      // Set RTLD_NODELETE for this object
            if ((flags & 0x00000010) != 0) descriptions.Add("LOADFLTR");      // Immediate loading of filters
            if ((flags & 0x00000020) != 0) descriptions.Add("INITFIRST");     // Set RTLD_INITFIRST for this object
            if ((flags & 0x00000040) != 0) descriptions.Add("NOOPEN");        // Set RTLD_NOOPEN for this object
            if ((flags & 0x00000080) != 0) descriptions.Add("ORIGIN");        // $ORIGIN must be resolved
            if ((flags & 0x00000100) != 0) descriptions.Add("DIRECT");        // Direct binding enabled
            if ((flags & 0x00000200) != 0) descriptions.Add("TRANS");         // Object is a transition
            if ((flags & 0x00000400) != 0) descriptions.Add("INTERPOSE");     // Object is an interposer
            if ((flags & 0x00000800) != 0) descriptions.Add("NODEFLIB");      // Ignore default lib search path
            if ((flags & 0x00001000) != 0) descriptions.Add("NOKSYMS");       // Do not allow RTLD_NOLOAD
            if ((flags & 0x00002000) != 0) descriptions.Add("NOHDR");         // Object has no headers
            if ((flags & 0x00004000) != 0) descriptions.Add("EDITED");        // Object has been modified
            if ((flags & 0x00008000) != 0) descriptions.Add("NORELOC");       // Object has no relocations
            if ((flags & 0x00010000) != 0) descriptions.Add("SYMINTPOSE");    // Object has individual interposers
            if ((flags & 0x00020000) != 0) descriptions.Add("GLOBAUDIT");     // Global auditing required
            if ((flags & 0x00040000) != 0) descriptions.Add(" SINGLETON");    // Singleton symbols are used

            return string.Join(", ", descriptions);
        }
    }
}