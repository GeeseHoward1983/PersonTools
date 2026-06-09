using System.Runtime.InteropServices;

namespace PersonalTools.PEAnalyzer.Models
{
    // 导入描述符
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGEIMPORTDESCRIPTOR
    {
        public uint OriginalFirstThunk;
        public uint TimeDateStamp;
        public uint ForwarderChain;
        public uint Name;
        public uint FirstThunk;
    }
}