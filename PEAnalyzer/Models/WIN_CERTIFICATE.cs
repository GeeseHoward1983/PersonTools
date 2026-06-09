using System.Runtime.InteropServices;

namespace PersonalTools.PEAnalyzer.Models
{
    // 证书结构
    [StructLayout(LayoutKind.Sequential)]
    internal struct WINCERTIFICATE
    {
        public uint dwLength;
        public ushort wRevision;
        public ushort wCertificateType;
        // BYTE bCertificate[ANYSIZE_ARRAY];  // 实际证书数据
    }
}