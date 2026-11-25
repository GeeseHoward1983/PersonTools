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
        private static bool CheckDataDirectory(IMAGE_DATA_DIRECTORY[] dataDirectory)
        {
            return dataDirectory.Length > PEConstants.DirectorySecurity &&
                   dataDirectory[PEConstants.DirectorySecurity].VirtualAddress != 0;
        }

        private static bool CheckDataOffset(FileStream fs, IMAGE_DATA_DIRECTORY directory)
        {
            return directory.VirtualAddress > 0 && directory.Size >= 8
                && (long)directory.VirtualAddress + directory.Size <= fs.Length;
        }

        /// <summary>
        /// 解析证书信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        internal static void ParseCertificateInfo(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {

                (peInfo.AdditionalInfo.IsSigned, peInfo.AdditionalInfo.CertificateInfo) = CheckDataDirectory(peInfo.OptionalHeader.DataDirectory) switch
                {
                    true => ParseCertificateInfo2(fs, reader, peInfo),
                    false => (false, "未包含数字签名")
                };
        }

        private static (bool, string) ParseCertificateInfo2(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            return CheckDataOffset(fs, peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity]) switch
            {
                true => ParseCertificateCore(ReadCertificates(fs, reader, (long)peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].VirtualAddress + peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].Size, peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].VirtualAddress), peInfo),
                false => (false, "证书信息不完整")
            };
        }

        private static (bool, string) ParseCertificateCore(List<string> certs, PEInfo peInfo)
        {
            return certs.Count switch
            {
                0 => (false, "证书信息不完整"),
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
        private static List<string> ReadCertificates(FileStream fs, BinaryReader reader, long certEnd, long pos)
        {
            List<string> certs = [];        

            return ReadCertificates2(certs, fs, reader, certEnd, pos);
        }

        private static List<string> ReadCertificates2(List<string> certs, FileStream fs, BinaryReader reader, long certEnd, long pos)
        {
            // 边界或条目校验失败时停止，保留此前已经解析的证书。
            while (TryReadNextCertificate(certs, fs, reader, certEnd, ref pos))
            {
            }

            return certs;
        }

        private static bool TryReadNextCertificate(List<string> certs, FileStream fs, BinaryReader reader, long certEnd, ref long pos)
        {
            // 先检查完整头部的读取边界；越界时短路，不读取头部或推进位置。
            return pos + 8 <= Math.Min(certEnd, fs.Length) &&
                   ReadCertificates3(certs, fs, reader, certEnd, ref pos);
        }

        private static bool ReadCertificates3(List<string> certs, FileStream fs, BinaryReader reader, long certEnd, ref long pos)
        {
            fs.Position = pos;
            return ReadCertificates4(certs, CreatCertHeader(reader), certEnd, ref pos);
        }

        private static bool ReadCertificates4(List<string> certs, WIN_CERTIFICATE certHeader, long certEnd, ref long pos)
        {
            return IsCertificateLengthValid(certHeader.dwLength, certEnd - pos) &&
                   AppendCertificate(certs, certHeader, ref pos);
        }

        // 仅在长度校验通过后调用；添加条目并推进位置，返回 true 让遍历继续。
        private static bool AppendCertificate(List<string> certs, WIN_CERTIFICATE certHeader, ref long pos)
        {
            certs.Add(FormatCertificateDescription(certHeader));
            return AppendCertificate2(ref pos, certHeader.dwLength);
        }

        private static bool AppendCertificate2(ref long pos, long length)
        {
            GetAlignedCertificateLength(ref pos, (uint)length);
            return true;
        }

        private static bool IsCertificateLengthValid(uint length, long remainingLength)
        {
            return length >= 8 && length <= remainingLength;
        }

        private static string FormatCertificateDescription(WIN_CERTIFICATE certHeader)
        {
            return $"类型: {GetCertificateTypeDescription(certHeader.wCertificateType)}, 长度: {certHeader.dwLength} 字节, 修订版: 0x{certHeader.wRevision:X4}";
        }

        private static void GetAlignedCertificateLength(ref long pos, uint length)
        {
            // 下一个证书按 8 字节对齐；加法前转为 long，避免 uint 溢出导致位置不前进。
            pos += ((long)length + 7) & ~7L;
        }

    }
}