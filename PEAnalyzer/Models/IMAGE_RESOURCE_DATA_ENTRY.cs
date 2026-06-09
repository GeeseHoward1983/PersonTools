using System.Runtime.InteropServices;

namespace PersonalTools.PEAnalyzer.Models
{
    // 资源数据条目
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_RESOURCE_DATA_ENTRY
    {
        public uint OffsetToData;
        public uint Size;
        public uint CodePage;
        public uint Reserved;
    }
}