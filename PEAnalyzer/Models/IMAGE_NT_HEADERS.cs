using System.Runtime.InteropServices;

namespace PersonalTools.PEAnalyzer.Models
{
    // NT头签名
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGENTHEADERS
    {
        public uint Signature;
        public IMAGEFILEHEADER FileHeader;
        public IMAGEOPTIONALHEADER OptionalHeader;
    }
}