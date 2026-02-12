using System.Runtime.InteropServices;

namespace PersonalTools.ELFAnalyzer.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct ELFDynamic
    {
        public long d_tag;     // Dynamic entry type
        public ulong d_val;    // Integer value
    }
}