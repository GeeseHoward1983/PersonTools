using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器证书信息解析模块
    /// 专门负责解析PE文件中的数字证书信息
    /// </summary>
    public static class PEResourceParserCertificate
    {
        /// <summary>
        /// 解析证书信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        public static void ParseCertificateInfo(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            try
            {
                // 证书信息在数据目录的第5项 (IMAGE_DIRECTORY_ENTRY_SECURITY)
                const int SECURITY_DIRECTORY_INDEX = 4;

                if (peInfo.OptionalHeader.DataDirectory.Length > SECURITY_DIRECTORY_INDEX &&
                    peInfo.OptionalHeader.DataDirectory[SECURITY_DIRECTORY_INDEX].VirtualAddress != 0)
                {
                    // 注意：安全目录的VirtualAddress实际上是文件偏移量，不是RVA
                    uint certificateOffset = peInfo.OptionalHeader.DataDirectory[SECURITY_DIRECTORY_INDEX].VirtualAddress;
                    uint certificateSize = peInfo.OptionalHeader.DataDirectory[SECURITY_DIRECTORY_INDEX].Size;

                    if (certificateOffset != 0 && certificateSize != 0 &&
                        certificateOffset < fs.Length &&
                        certificateOffset + certificateSize <= fs.Length)
                    {
                        long originalPosition = fs.Position;

                        fs.Position = certificateOffset;

                        // 读取证书头
                        if (fs.Position + 8 <= fs.Length)
                        {
                            var certHeader = new WIN_CERTIFICATE
                            {
                                dwLength = reader.ReadUInt32(),
                                wRevision = reader.ReadUInt16(),
                                wCertificateType = reader.ReadUInt16()
                            };

                            peInfo.AdditionalInfo.IsSigned = true;

                            // 根据证书类型生成信息
                            string certType = "未知";
                            switch (certHeader.wCertificateType)
                            {
                                case 0x0001:
                                    certType = "X509";
                                    break;
                                case 0x0002:
                                    certType = "PKCS#7";
                                    break;
                                case 0x0003:
                                    certType = "PKCS#1";
                                    break;
                            }

                            peInfo.AdditionalInfo.CertificateInfo =
                                $"类型: {certType}, 长度: {certHeader.dwLength} 字节, 修订版: {certHeader.wRevision}";
                        }

                        fs.Position = originalPosition;
                    }
                }
                else
                {
                    // 如果没有证书，则设置默认值
                    peInfo.AdditionalInfo.IsSigned = false;
                    peInfo.AdditionalInfo.CertificateInfo = "文件未签名";
                }
            }
            catch (Exception ex)
            {
                peInfo.AdditionalInfo.CertificateInfo = $"解析错误: {ex.Message}";
            }
        }
    }
}