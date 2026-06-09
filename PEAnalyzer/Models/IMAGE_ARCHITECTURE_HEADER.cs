using System.Runtime.InteropServices;

namespace PersonalTools.PEAnalyzer.Models
{
    // 版权信息头
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGEARCHITECTUREHEADER
    {
        public uint AmaskValue;
        public uint Reserved1;  // 以前称为 Adummy1
        public uint Reserved2;  // 以前称为 Adummy2
        public uint Signature;
        public uint Reserved3;  // 以前称为 StrucLen
        public uint AddressOfData;  // RVA to the data
        public uint SizeOfData;    // 大小
    }
}