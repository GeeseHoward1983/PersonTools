using System.Runtime.InteropServices;

namespace PersonalTools.PEAnalyzer.Models
{
    // 资源目录项
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGERESOURCEDIRECTORYENTRY
    {
        public uint NameOrId;
        public uint OffsetToData;
    }
}