using System.Runtime.InteropServices;

namespace PersonalTools.PEAnalyzer.Models
{
    // 资源目录项
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_RESOURCE_DIRECTORY_ENTRY
    {
        public uint NameOrId;
        public uint OffsetToData;
    }
}