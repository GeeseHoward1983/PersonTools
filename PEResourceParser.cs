using System;
using System.IO;
using System.Text;

namespace MyTool
{
    /// <summary>
    /// PE资源解析器
    /// 专门负责解析PE文件中的各种资源信息
    /// </summary>
    public static class PEResourceParser
    {
        /// <summary>
        /// 解析版本信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        public static void ParseVersionInfo(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            try
            {
                // 版本信息通常在资源节中，数据目录索引为#2 (IMAGE_DIRECTORY_ENTRY_RESOURCE)
                const int RESOURCE_DIRECTORY_INDEX = 2; // IMAGE_DIRECTORY_ENTRY_RESOURCE

                if (peInfo.OptionalHeader.DataDirectory.Length > RESOURCE_DIRECTORY_INDEX &&
                    peInfo.OptionalHeader.DataDirectory[RESOURCE_DIRECTORY_INDEX].VirtualAddress != 0)
                {
                    uint resourceRVA = peInfo.OptionalHeader.DataDirectory[RESOURCE_DIRECTORY_INDEX].VirtualAddress;
                    long resourceOffset = PEResourceParser.RvaToOffset(resourceRVA, peInfo.SectionHeaders);

                    if (resourceOffset != -1 && resourceOffset < fs.Length)
                    {
                        // 解析资源目录以找到版本信息
                        ParseResourceDirectoryForVersionInfo(fs, reader, peInfo, resourceOffset);
                    }
                }
            }
            catch (Exception ex)
            {
                // 解析版本信息时出现异常，记录日志但不中断程序执行
                peInfo.AdditionalInfo.FileVersion = $"解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析资源目录以查找版本信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="resourceOffset">资源节偏移</param>
        private static void ParseResourceDirectoryForVersionInfo(FileStream fs, BinaryReader reader, PEInfo peInfo, long resourceOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = resourceOffset;

                // 读取根资源目录
                var rootDirectory = new IMAGE_RESOURCE_DIRECTORY
                {
                    Characteristics = reader.ReadUInt32(),
                    TimeDateStamp = reader.ReadUInt32(),
                    MajorVersion = reader.ReadUInt16(),
                    MinorVersion = reader.ReadUInt16(),
                    NumberOfNamedEntries = reader.ReadUInt16(),
                    NumberOfIdEntries = reader.ReadUInt16()
                };

                int totalEntries = rootDirectory.NumberOfNamedEntries + rootDirectory.NumberOfIdEntries;

                // 遍历资源目录项
                for (int i = 0; i < totalEntries; i++)
                {
                    fs.Position = resourceOffset + 16 + i * 8; // 16是IMAGE_RESOURCE_DIRECTORY大小，每项8字节

                    var entry = new IMAGE_RESOURCE_DIRECTORY_ENTRY
                    {
                        NameOrId = reader.ReadUInt32(),
                        OffsetToData = reader.ReadUInt32()
                    };

                    // 检查是否是RT_VERSION资源类型 (ID = 16)
                    if ((entry.NameOrId & 0xFFFF) == 16) // RT_VERSION = 16
                    {
                        long nextLevelOffset = resourceOffset + (entry.OffsetToData & 0x7FFFFFFF);
                        ParseVersionResource(fs, reader, peInfo, nextLevelOffset, resourceOffset);
                        break;
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                peInfo.AdditionalInfo.FileVersion = $"资源目录解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析版本资源
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="directoryOffset">目录偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        private static void ParseVersionResource(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = directoryOffset;

                // 读取资源目录
                var directory = new IMAGE_RESOURCE_DIRECTORY
                {
                    Characteristics = reader.ReadUInt32(),
                    TimeDateStamp = reader.ReadUInt32(),
                    MajorVersion = reader.ReadUInt16(),
                    MinorVersion = reader.ReadUInt16(),
                    NumberOfNamedEntries = reader.ReadUInt16(),
                    NumberOfIdEntries = reader.ReadUInt16()
                };

                // 遍历子项查找语言节点
                int totalEntries = directory.NumberOfNamedEntries + directory.NumberOfIdEntries;
                for (int i = 0; i < totalEntries; i++)
                {
                    fs.Position = directoryOffset + 16 + i * 8; // 跳过目录头(16字节)，每项8字节

                    var entry = new IMAGE_RESOURCE_DIRECTORY_ENTRY
                    {
                        NameOrId = reader.ReadUInt32(),
                        OffsetToData = reader.ReadUInt32()
                    };

                    // 检查是否是叶子节点
                    // 最高位为1表示指向下一级目录，为0表示指向数据条目
                    // 我们需要处理两种情况
                    if ((entry.OffsetToData & 0x80000000) != 0)
                    {
                        // 最高位为1，表示指向下一级目录
                        // 清除最高位得到实际偏移
                        long nextLevelOffset = resourceBaseOffset + (entry.OffsetToData & 0x7FFFFFFF);
                        // 递归处理下一级目录
                        ParseVersionResource(fs, reader, peInfo, nextLevelOffset, resourceBaseOffset);
                        break;
                    }
                    else
                    {
                        // 最高位为0，表示指向数据条目
                        long dataEntryOffset = resourceBaseOffset + entry.OffsetToData;
                        ParseVersionDataEntry(fs, reader, peInfo, dataEntryOffset, resourceBaseOffset);
                        break;
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                peInfo.AdditionalInfo.FileVersion = $"版本资源解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析版本数据项
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="dataEntryOffset">数据项偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        private static void ParseVersionDataEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset, long resourceBaseOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = dataEntryOffset;

                // 读取资源数据项
                var dataEntry = new IMAGE_RESOURCE_DATA_ENTRY
                {
                    OffsetToData = reader.ReadUInt32(),
                    Size = reader.ReadUInt32(),
                    CodePage = reader.ReadUInt32(),
                    Reserved = reader.ReadUInt32()
                };

                // 计算实际数据偏移（注意：资源数据的OffsetToData是RVA）
                long dataOffset = RvaToOffset(dataEntry.OffsetToData, peInfo.SectionHeaders);
                if (dataOffset != -1 && dataOffset < fs.Length)
                {
                    fs.Position = dataOffset;

                    // 读取版本信息结构
                    ParseVersionInfoStructure(fs, reader, peInfo, dataEntry.Size);
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                peInfo.AdditionalInfo.FileVersion = $"数据项解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析版本信息结构
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="size">数据大小</param>
        private static void ParseVersionInfoStructure(FileStream fs, BinaryReader reader, PEInfo peInfo, uint size)
        {
            try
            {
                long startPosition = fs.Position;

                // 读取VS_VERSIONINFO头
                if (fs.Position + 6 <= fs.Length)
                {
                    ushort wLength = reader.ReadUInt16();
                    ushort wValueLength = reader.ReadUInt16();
                    ushort wType = reader.ReadUInt16();

                    // 检查基本的有效性
                    if (wLength == 0)
                        return;

                    // 读取szKey (UNICODE字符串 "VS_VERSION_INFO")
                    string vsVersionInfoKey = ReadUnicodeStringWithMaxLength(reader, wLength);
                    
                    // 验证键名
                    if (!vsVersionInfoKey.Equals("VS_VERSION_INFO", StringComparison.OrdinalIgnoreCase))
                        return;

                    // 对齐到4字节边界
                    long alignedPosition = (fs.Position + 3) & ~3;

                    // 检查是否有足够的空间读取VS_FIXEDFILEINFO
                    // VS_FIXEDFILEINFO大小为52字节，但我们需要检查wValueLength是否有效
                    if (wValueLength >= 52 && alignedPosition + 52 <= fs.Length)
                    {
                        fs.Position = alignedPosition;

                        // 读取VS_FIXEDFILEINFO结构
                        var fixedFileInfo = new VS_FIXEDFILEINFO
                        {
                            dwSignature = reader.ReadUInt32(),
                            dwStrucVersion = reader.ReadUInt32(),
                            dwFileVersionMS = reader.ReadUInt32(),
                            dwFileVersionLS = reader.ReadUInt32(),
                            dwProductVersionMS = reader.ReadUInt32(),
                            dwProductVersionLS = reader.ReadUInt32(),
                            dwFileFlagsMask = reader.ReadUInt32(),
                            dwFileFlags = reader.ReadUInt32(),
                            dwFileOS = reader.ReadUInt32(),
                            dwFileType = reader.ReadUInt32(),
                            dwFileSubtype = reader.ReadUInt32(),
                            dwFileDateMS = reader.ReadUInt32(),
                            dwFileDateLS = reader.ReadUInt32()
                        };

                        // 验证签名
                        if (fixedFileInfo.dwSignature == 0xFEEF04BD) // VS_VERSIONINFO签名
                        {
                            uint fileVersionMajor = (fixedFileInfo.dwFileVersionMS >> 16) & 0xFFFF;
                            uint fileVersionMinor = fixedFileInfo.dwFileVersionMS & 0xFFFF;
                            uint fileVersionBuild = (fixedFileInfo.dwFileVersionLS >> 16) & 0xFFFF;
                            uint fileVersionRev = fixedFileInfo.dwFileVersionLS & 0xFFFF;
                            peInfo.AdditionalInfo.FileVersion = $"{fileVersionMajor}.{fileVersionMinor}.{fileVersionBuild}.{fileVersionRev}";

                            uint productVersionMajor = (fixedFileInfo.dwProductVersionMS >> 16) & 0xFFFF;
                            uint productVersionMinor = fixedFileInfo.dwProductVersionMS & 0xFFFF;
                            uint productVersionBuild = (fixedFileInfo.dwProductVersionLS >> 16) & 0xFFFF;
                            uint productVersionRev = fixedFileInfo.dwProductVersionLS & 0xFFFF;
                            peInfo.AdditionalInfo.ProductVersion = $"{productVersionMajor}.{productVersionMinor}.{productVersionBuild}.{productVersionRev}";
                        }
                        else
                        {
                            // 签名无效，记录日志
                            peInfo.AdditionalInfo.FileVersion = $"无效的版本信息签名: 0x{fixedFileInfo.dwSignature:X8}";
                        }

                        // 继续解析StringFileInfo部分
                        // 计算StringFileInfo的开始位置
                        long stringFileInfoPosition = alignedPosition + 52; // 跳过FIXEDFILEINFO
                        stringFileInfoPosition = (stringFileInfoPosition + 3) & ~3; // 对齐到4字节边界

                        if (stringFileInfoPosition < fs.Length && stringFileInfoPosition < startPosition + wLength)
                        {
                            fs.Position = stringFileInfoPosition;
                            ParseStringFileInfo(fs, reader, peInfo, startPosition + wLength);
                        }
                    }
                    else
                    {
                        // wValueLength太小或没有足够的数据
                        peInfo.AdditionalInfo.FileVersion = $"版本信息数据不完整: wValueLength={wValueLength}, 需要>=52";
                    }
                }
            }
            catch (Exception ex)
            {
                peInfo.AdditionalInfo.FileVersion = $"版本信息结构解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析StringFileInfo部分
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="endPosition">版本信息结构的结束位置</param>
        private static void ParseStringFileInfo(FileStream fs, BinaryReader reader, PEInfo peInfo, long endPosition)
        {
            try
            {
                long startPosition = fs.Position;

                // 检查是否还有足够的数据
                if (fs.Position + 6 > fs.Length)
                    return;

                ushort wLength = reader.ReadUInt16();
                ushort wValueLength = reader.ReadUInt16();
                ushort wType = reader.ReadUInt16();

                // 读取szKey (UNICODE字符串 "StringFileInfo")
                string key = ReadUnicodeStringWithMaxLength(reader, wLength); // 使用wLength而不是固定值

                if (key.Equals("StringFileInfo", StringComparison.OrdinalIgnoreCase))
                {
                    // 计算StringTable的位置
                    long keyLengthInBytes = (key.Length + 1) * 2; // Unicode字符串长度 + null终止符
                    long afterKeyPosition = startPosition + 6 + keyLengthInBytes; // 6是头部大小
                    long stringTablePosition = (afterKeyPosition + 3) & ~3; // 对齐到4字节边界

                    long stringFileInfoEndPosition = startPosition + wLength;
                    long actualEndPosition = Math.Min(stringFileInfoEndPosition, endPosition);

                    if (stringTablePosition < fs.Length && stringTablePosition < actualEndPosition)
                    {
                        fs.Position = stringTablePosition;
                        ParseStringTable(fs, reader, peInfo, actualEndPosition);
                    }
                }
            }
            catch (Exception ex)
            {
                // 忽略StringFileInfo解析错误，但可以记录日志用于调试
                peInfo.AdditionalInfo.FileVersion += $"; StringFileInfo解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析StringTable部分
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="endPosition">StringFileInfo块的结束位置</param>
        private static void ParseStringTable(FileStream fs, BinaryReader reader, PEInfo peInfo, long endPosition)
        {
            try
            {
                long startPosition = fs.Position;

                // 检查是否还有足够的数据
                if (fs.Position + 6 > fs.Length)
                    return;

                ushort wLength = reader.ReadUInt16();
                ushort wValueLength = reader.ReadUInt16();
                ushort wType = reader.ReadUInt16();

                // 读取语言和代码页标识符（通常是8位十六进制字符串）
                string langId = ReadUnicodeStringWithMaxLength(reader, wLength); // 读取直到找到null终止符

                // 计算Strings的位置
                long keyLengthInBytes = (langId.Length + 1) * 2; // Unicode字符串长度 + null终止符
                long afterLangIdPosition = startPosition + 6 + keyLengthInBytes;
                long stringsPosition = (afterLangIdPosition + 3) & ~3; // 对齐到4字节边界

                if (stringsPosition >= fs.Length || stringsPosition >= endPosition)
                    return;

                fs.Position = stringsPosition;

                // 解析字符串对
                long tableEndPosition = Math.Min(startPosition + wLength, endPosition);

                while (fs.Position < tableEndPosition && fs.Position + 6 <= fs.Length)
                {
                    long stringStartPos = fs.Position;

                    // 先读取头部信息判断长度
                    if (fs.Position + 6 > fs.Length) break;
                    
                    ushort strLength = reader.ReadUInt16();
                    ushort strValueLength = reader.ReadUInt16();
                    ushort strType = reader.ReadUInt16();

                    if (strLength == 0)
                    {
                        fs.Position = stringStartPos;
                        break;
                    }

                    // 重新定位到开始位置继续解析
                    fs.Position = stringStartPos;
                    ParseStringPair(fs, reader, peInfo, tableEndPosition);
                }
            }
            catch (Exception ex)
            {
                // 忽略StringTable解析错误
                peInfo.AdditionalInfo.FileVersion += $"; StringTable解析错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 解析单个字符串对
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="endPosition">StringTable块的结束位置</param>
        private static void ParseStringPair(FileStream fs, BinaryReader reader, PEInfo peInfo, long endPosition)
        {
            try
            {
                long startPosition = fs.Position;

                // 检查是否还有足够的数据
                if (fs.Position + 6 > fs.Length)
                    return;

                ushort wLength = reader.ReadUInt16();
                ushort wValueLength = reader.ReadUInt16();
                ushort wType = reader.ReadUInt16();

                if (wLength == 0 || startPosition + wLength > endPosition)
                    return;

                // 读取键名
                var keySb = new StringBuilder();
                char ch;
                while (fs.Position < fs.Length && fs.Position < endPosition && fs.Position < startPosition + wLength)
                {
                    if (fs.Position + 2 > fs.Length) break;
                    ch = (char)reader.ReadUInt16();
                    if (ch == '\0')
                        break;
                    keySb.Append(ch);
                }

                string keyValue = keySb.ToString();

                // 跳过可能的额外null字符并对齐到4字节边界
                long currentPosition = fs.Position;
                long valuePosition = (currentPosition + 3) & ~3;

                // 确保valuePosition不超过边界
                if (valuePosition >= endPosition || valuePosition >= fs.Length)
                    return;

                fs.Position = valuePosition;

                // 读取值
                var valueSb = new StringBuilder();
                long valueEndPosition = Math.Min(startPosition + wLength, endPosition);
                while (fs.Position < fs.Length && fs.Position < valueEndPosition)
                {
                    if (fs.Position + 2 > fs.Length) break;
                    ch = (char)reader.ReadUInt16();
                    if (ch == '\0')
                        break;
                    valueSb.Append(ch);
                }

                string value = valueSb.ToString();

                // 根据键名设置相应的属性
                switch (keyValue.ToLower())
                {
                    case "companyname":
                        peInfo.AdditionalInfo.CompanyName = value;
                        break;
                    case "filedescription":
                        peInfo.AdditionalInfo.FileDescription = value;
                        break;
                    case "fileversion":
                        if (string.IsNullOrEmpty(peInfo.AdditionalInfo.FileVersion) || !peInfo.AdditionalInfo.FileVersion.Contains("."))
                            peInfo.AdditionalInfo.FileVersion = value;
                        break;
                    case "productname":
                        peInfo.AdditionalInfo.ProductName = value;
                        break;
                    case "productversion":
                        if (string.IsNullOrEmpty(peInfo.AdditionalInfo.ProductVersion) || !peInfo.AdditionalInfo.ProductVersion.Contains("."))
                            peInfo.AdditionalInfo.ProductVersion = value;
                        break;
                    case "originalfilename":
                        peInfo.AdditionalInfo.OriginalFileName = value;
                        break;
                    case "internalname":
                        peInfo.AdditionalInfo.InternalName = value;
                        break;
                    case "legalcopyright":
                        peInfo.AdditionalInfo.LegalCopyright = value;
                        break;
                    case "legaltrademarks":
                        peInfo.AdditionalInfo.LegalTrademarks = value;
                        break;
                }

                // 移动到下一个字符串对
                fs.Position = (startPosition + wLength + 3) & ~3;

                // 确保不超出边界
                if (fs.Position > endPosition)
                    fs.Position = endPosition;
            }
            catch (Exception ex)
            {
                // 忽略单个字符串对解析错误
                peInfo.AdditionalInfo.FileVersion += $"; StringPair解析错误: {ex.Message}";
            }
        }

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

        /// <summary>
        /// 将RVA转换为文件偏移量
        /// </summary>
        /// <param name="rva">相对虚拟地址</param>
        /// <param name="sections">节头列表</param>
        /// <returns>文件偏移量</returns>
        public static long RvaToOffset(uint rva, System.Collections.Generic.List<IMAGE_SECTION_HEADER> sections)
        {
            // 添加对RVA的基本验证
            if (rva == 0)
                return -1;

            foreach (var section in sections)
            {
                // 确保VirtualSize不为0，避免除零错误
                if (section.VirtualSize == 0)
                    continue;

                if (rva >= section.VirtualAddress && rva < section.VirtualAddress + section.VirtualSize)
                {
                    // 确保计算结果不会溢出
                    long offset = (long)(section.PointerToRawData + (rva - section.VirtualAddress));
                    // 确保offset不为负数且在合理范围内
                    if (offset >= 0)
                        return offset;
                }
            }
            return -1;
        }

        /// <summary>
        /// 读取UNICODE字符串（带最大长度限制）
        /// </summary>
        /// <param name="reader">二进制读取器</param>
        /// <param name="maxLength">最大字符数</param>
        /// <returns>读取的字符串</returns>
        private static string ReadUnicodeStringWithMaxLength(BinaryReader reader, int maxLength)
        {
            try
            {
                var sb = new StringBuilder();
                int count = 0;

                while (count < maxLength)
                {
                    if (reader.BaseStream.Position + 2 > reader.BaseStream.Length)
                        break;

                    ushort ch = reader.ReadUInt16();
                    if (ch == 0) // NULL终止符
                        break;

                    sb.Append((char)ch);
                    count++;
                }

                return sb.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}