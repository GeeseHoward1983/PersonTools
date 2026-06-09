using System.Runtime.InteropServices;

namespace PersonalTools.PEAnalyzer.Models
{
    // 导出目录
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGEEXPORTDIRECTORY
    {
        public uint Characteristics;
        public uint TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public uint Name;
        public uint Base;
        public uint NumberOfFunctions;
        public uint NumberOfNames;
        public uint AddressOfFunctions;
        public uint AddressOfNames;
        public uint AddressOfNameOrdinals;
    }
}