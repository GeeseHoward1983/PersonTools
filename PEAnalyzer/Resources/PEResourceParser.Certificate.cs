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
        /// <summary>
        /// 解析证书信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
internal static void ParseCertificateInfo(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            try
            {
                // 证书信息在数据目录的第5项 (IMAGE_DIRECTORY_ENTRY_SECURITY)

                if (peInfo.OptionalHeader.DataDirectory.Length > PEConstants.DirectorySecurity &&
                    peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].VirtualAddress != 0)
                {
                    // 注意：安全目录的VirtualAddress实际上是文件偏移量，不是RVA
                    uint certificateOffset = peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].VirtualAddress;
                    uint certificateSize = peInfo.OptionalHeader.DataDirectory[PEConstants.DirectorySecurity].Size;

                    if (certificateOffset != 0 && certificateSize != 0 &&
                        certificateOffset < fs.Length &&
                        certificateOffset + certificateSize <= fs.Length)
                    {
                        long originalPosition = fs.Position;
                        long certEnd = (long)certificateOffset + certificateSize;
                        long pos = certificateOffset;
                        List<string> certs = [];

                        // 安全目录是 8 字节对齐的 WIN_CERTIFICATE 数组，逐个解析（支持多重签名）
                        while (pos + 8 <= certEnd && pos + 8 <= fs.Length)
                        {
                            fs.Position = pos;
                            WINCERTIFICATE certHeader = new()
                            {
                                dwLength = reader.ReadUInt32(),
                                wRevision = reader.ReadUInt16(),
                                wCertificateType = reader.ReadUInt16()
                            };

                            if (certHeader.dwLength < 8 || pos + certHeader.dwLength > certEnd)
                            {
                                break;
                            }

                            string certType = certHeader.wCertificateType switch
                            {
                                0x0001 => "X509",
                                0x0002 => "PKCS#7",
                                0x0003 => "PKCS#1",
                                _ => "未知"
                            };
                            certs.Add($"类型: {certType}, 长度: {certHeader.dwLength} 字节, 修订版: 0x{certHeader.wRevision:X4}");

                            // 下一个证书按 8 字节对齐
                            pos += (long)((certHeader.dwLength + 7u) & ~7u);
                        }

                        fs.Position = originalPosition;

                        if (certs.Count > 0)
                        {
                            peInfo.AdditionalInfo.IsSigned = true;
                            peInfo.AdditionalInfo.CertificateInfo = certs.Count == 1
                                ? certs[0]
                                : $"{certs.Count} 个证书 -> " + string.Join("; ", certs);
                        }
                    }
                }
                else
                {
                    // 如果没有证书，则设置默认值
                    peInfo.AdditionalInfo.IsSigned = false;
                    peInfo.AdditionalInfo.CertificateInfo = "文件未签名";
                }
            }
            catch (IOException ex)
            {
                peInfo.AdditionalInfo.CertificateInfo = $"解析错误: {ex.Message}";
            }
            catch (UnauthorizedAccessException ex)
            {
                peInfo.AdditionalInfo.CertificateInfo = $"解析错误: {ex.Message}";
            }
            catch (ArgumentOutOfRangeException ex)
            {
                peInfo.AdditionalInfo.CertificateInfo = $"解析错误: {ex.Message}";
            }
        }
    }
}