using System.Runtime.InteropServices;

namespace PersonalTools.PEAnalyzer.Models
{
    // 数据目录项
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGEDATADIRECTORY
    {
        public uint VirtualAddress;
        public uint Size;
    }
}