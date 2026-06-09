using System.Runtime.InteropServices;

namespace PersonalTools.PEAnalyzer.Models
{
    // NT头签名
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_NT_HEADERS
    {
        public uint Signature;
        public IMAGE_FILE_HEADER FileHeader;
        public IMAGE_OPTIONAL_HEADER OptionalHeader;
    }
}