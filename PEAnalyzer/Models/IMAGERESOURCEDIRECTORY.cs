using System.Runtime.InteropServices;

namespace PersonalTools.PEAnalyzer.Models
{
    // 资源目录
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGERESOURCEDIRECTORY
    {
        public uint Characteristics;
        public uint TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public ushort NumberOfNamedEntries;
        public ushort NumberOfIdEntries;
    }
}