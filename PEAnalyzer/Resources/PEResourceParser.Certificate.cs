using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器证书信息解析模块
    /// 专门负责解析PE文件中的数字证书信息
    /// </summary>
    internal static class PEResourceParserCertificate
    {
        private static bool CheckDataDirectory(PEInfo peInfo)
        {
            return peInfo.OptionalHeader.DataDirectory.Length > PEConstants.DirectorySecurity &&
                   peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].VirtualAddress != 0;
        }

        private static bool CheckDataOffset(FileStream fs, PEInfo peInfo)
        {
            return peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].VirtualAddress switch
            {
                0 => peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].VirtualAddress < fs.Length && (long)peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].VirtualAddress + peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].Size <= fs.Length,
                _ => peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].Size switch
                {
                    0 => false,
                    _ => peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].VirtualAddress < fs.Length && (long)peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].VirtualAddress + peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].Size <= fs.Length
                }
            };
        }

        /// <summary>
        /// 解析证书信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        internal static void ParseCertificateInfo(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {

                (peInfo.AdditionalInfo.IsSigned, peInfo.AdditionalInfo.CertificateInfo) = CheckDataDirectory(peInfo) switch
                {
                    true => ParseCertificateInfo2(fs, reader, peInfo),
                    false => (false, "证书信息不完整")
                };
        }

        private static (bool, string) ParseCertificateInfo2(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            return CheckDataOffset(fs, peInfo) switch
            {
                true => ParseCertificateCore(ReadCertificates(fs, reader, peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].VirtualAddress + peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].Size, peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].Size), peInfo),
                false => (false, "证书信息不完整")
            };
        }

        private static (bool, string) ParseCertificateCore(List<string> certs, PEInfo peInfo)
        {
            return certs.Count switch
            {
                0 => (false, ""),
                1 => (true, certs[0]),
                _ => (true, $"{certs.Count} 个证书 -> {string.Join("; ", certs)}")
            }; 
        }

        private static WIN_CERTIFICATE CreatCertHeader(BinaryReader reader)
        {
            return new()
            {
                dwLength = reader.ReadUInt32(),
                wRevision = reader.ReadUInt16(),
                wCertificateType = reader.ReadUInt16()
            };
        }

        private static string GetCertificateTypeDescription(ushort wCertificateType)
        {
            return wCertificateType switch
            {
                0x0001 => "X509",
                0x0002 => "PKCS#7",
                0x0003 => "Reserved(0x0003)",
                _ => "未知"
            };
        }

        // 解析安全目录中的 WIN_CERTIFICATE 数组（8字节对齐，支持多重签名），返回每个证书的描述
        private static List<string> ReadCertificates(FileStream fs, BinaryReader reader, uint certEnd, long pos)
        {
            (long originalPosition, List<string> certs) = (fs.Position, []);

            //证书条目数硬上限：真实 PE 多重签名至多个位数，畸形安全目录可枚举大量伪证书撑内存
            while (pos + 8 <= Math.Min(certEnd, fs.Length) && certs.Count < 64)
            {
                fs.Position = pos;
                WIN_CERTIFICATE certHeader = CreatCertHeader(reader);

                if (certHeader.dwLength < 8 || certHeader.dwLength > certEnd - pos)
                {
                    break;
                }

                certs.Add($"类型: {GetCertificateTypeDescription(certHeader.wCertificateType)}, 长度: {certHeader.dwLength} 字节, 修订版: 0x{certHeader.wRevision:X4}");

                // 下一个证书按 8 字节对齐；在 long 上做对齐运算，避免 dwLength 逼近 uint.MaxValue 时 +7 回绕导致 pos 不前进
                pos += (certHeader.dwLength + 7) & ~7L;
            }

            fs.Position = originalPosition;
            return certs;
        }
    }
}