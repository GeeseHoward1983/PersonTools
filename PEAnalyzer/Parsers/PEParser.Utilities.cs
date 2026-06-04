using PersonalTools.PEAnalyzer.Models;
using System.IO;
using System.Text;

namespace PersonalTools.PEAnalyzer.Parsers
{
    internal static class Utilities
    {
        public static string ReadNullTerminatedString(BinaryReader reader)
        {
            StringBuilder sb = new();
            try
            {
                byte b;

                while (reader.BaseStream.Position < reader.BaseStream.Length && (b = reader.ReadByte()) != 0)
                {
                    // 确保是有效的ASCII字符
                    if (b is >= 32 and <= 126)
                    {
                        sb.Append((char)b);
                    }
                    else if (b is 9 or 10 or 13)
                    {
                        // 允许制表符、换行符和回车符
                        sb.Append((char)b);
                    }
                    else
                    {
                        // 其他字符用'?'替换
                        sb.Append('?');
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // 遇到文件末尾时返回已读取的部分字符串
            }

            return sb.ToString();
        }

        // 获取机器类型描述
        public static string GetMachineTypeDescription(ushort machine)
        {
            return machine switch
            {
                0x014c => "Intel 386 (x86)",
                0x0162 => "MIPS R3000",
                0x0166 => "MIPS R4000",
                0x0168 => "MIPS R10000",
                0x0169 => "MIPS WCI v2",
                0x0184 => "Alpha AXP",
                0x01A2 => "SH3",
                0x01A3 => "SH3 DSP",
                0x01A4 => "SH3E",
                0x01A6 => "SH4",
                0x01A8 => "SH5",
                0x01C0 => "ARM",
                0x01C2 => "ARM Thumb/Thumb-2",
                0x01C4 => "ARM Thumb-2",
                0x01D3 => "AM33",
                0x01F0 => "PowerPC",
                0x01F1 => "PowerPC FP",
                0x0200 => "Intel IA64",
                0x0266 => "MIPS16",
                0x0268 => "Motorola 68000 series",
                0x0284 => "Alpha AXP 64-bit",
                0x0366 => "MIPS with FPU",
                0x0466 => "MIPS16 with FPU",
                0x0520 => "TRICORE",
                0x0CEF => "CEF",
                0x0EBC => "EFI Byte Code",
                0x8664 => "AMD64 (x64)",
                0x9041 => "Mitsubishi M32R",
                0xAA64 => "ARM64",
                0xC0EE => "CEE",
                _ => $"Unknown (0x{machine:X4})",
            };
        }

        // 获取子系统描述
        public static string GetSubsystemDescription(ushort subsystem)
        {
            return subsystem switch
            {
                0 => "Unknown (0x0000)",
                1 => "Native (0x0001)",
                2 => "Windows GUI (0x0002)",
                3 => "Windows CUI (0x0003)",
                5 => "OS/2 CUI (0x0005)",
                7 => "POSIX CUI (0x0007)",
                8 => "Native Windows (0x0008)",
                9 => "Windows CE GUI (0x0009)",
                10 => "EFI Application (0x000A)",
                11 => "EFI Boot Service Driver (0x000B)",
                12 => "EFI Runtime Driver (0x000C)",
                13 => "EFI ROM (0x000D)",
                14 => "XBOX (0x000E)",
                16 => "Windows Boot Application (0x0010)",
                _ => $"Unknown (0x{subsystem:X4})",
            };
        }

        // 获取链接器版本描述
        public static string GetLinkerVersionDescription(byte majorVersion, byte minorVersion)
        {
            // 常见的链接器版本映射
            return $"{majorVersion switch
            {
                1 or 2 or 3 or 4 or 5 => "Microsoft Linker (早期版本)",
                6 => "Microsoft Linker 6.x",
                7 => "Microsoft Linker 7.x (VS.NET 2002)",
                8 => "Microsoft Linker 8.x (VS 2005)",
                9 => "Microsoft Linker 9.x (VS 2008)",
                10 => "Microsoft Linker 10.x (VS 2010)",
                11 => "Microsoft Linker 11.x (VS 2012)",
                12 => "Microsoft Linker 12.x (VS 2013)",
                14 => minorVersion == 10 ? "Microsoft Linker 14.10 (VS 2015)"
                    : (minorVersion == 20) ? "Microsoft Linker 14.20 (VS 2017)"
                    : (minorVersion is >= 26 and <= 29) ? "Microsoft Linker 14.26-14.29 (VS 2019)"
                    : (minorVersion >= 30) ? "Microsoft Linker 14.30+ (VS 2022)"
                    : "Microsoft Linker 14.x (VS 2015/2017/2019)",
                15 => "Microsoft Linker 15.x",
                _ =>
                    majorVersion > 15 ? $"Microsoft Linker {majorVersion}.{minorVersion} (较新版本)" : $"Microsoft Linker {majorVersion}.{minorVersion} (未知版本)"

            }} [{majorVersion}.{minorVersion}]";
        }

        // 获取编译器版本描述
        public static string GetCompilerVersionDescription(byte majorVersion, byte minorVersion, bool isNetAssembly = false)
        {
            // 编译器版本信息通常可以从链接器版本推断
            return $"{majorVersion switch
            {
                6 => "Microsoft Visual C++ 6.0",
                7 => "Microsoft Visual C++ .NET 2002",
                8 => "Microsoft Visual C++ 2005",
                9 => "Microsoft Visual C++ 2008",
                10 => "Microsoft Visual C++ 2010",
                11 => "Microsoft Visual C++ 2012",
                12 => "Microsoft Visual C++ 2013",
                14 => minorVersion == 10 ? "Microsoft Visual C++ 2015"
                    : (minorVersion == 20) ? "Microsoft Visual C++ 2017"
                    : (minorVersion is >= 26 and <= 29) ? "Microsoft Visual C++ 2019"
                    : (minorVersion >= 30) ? "Microsoft Visual C++ 2022"
                    : "Microsoft Visual C++ 2015/2017/2019",
                15 => "Microsoft Visual C++ (未知版本)",
                _ =>
                    isNetAssembly ? "Microsoft .NET Compiler" :
                    majorVersion > 15 ? "Microsoft Visual C++ (较新版本)" : "Microsoft Visual C++ (未知版本)"
            }} [{majorVersion}.{minorVersion}]";
        }

        // 获取操作系统版本描述
        public static string GetOperatingSystemVersionDescription(ushort majorVersion, ushort minorVersion)
        {
            return $"{majorVersion switch
            {
                3 => minorVersion switch
                {
                    10 => "Windows NT 3.1",
                    51 => "Windows NT 3.51",
                    _ => "Unknown OS"
                },
                4 => minorVersion switch
                {
                    0 => "Windows NT 4.0",
                    _ => "Unknown OS"
                },
                5 => minorVersion switch
                {
                    0 => "Windows 2000",
                    1 => "Windows XP",
                    2 => "Windows Server 2003",
                    _ => "Unknown OS"
                },
                6 => minorVersion switch
                {
                    0 => "Windows Vista/Server 2008",
                    1 => "Windows 7/Server 2008 R2",
                    2 => "Windows 8/Server 2012",
                    3 => "Windows 8.1/Server 2012 R2",
                    _ => "Unknown OS"
                },
                10 => minorVersion switch
                {
                    0 => "Windows 10/11/Server 2016/2019/2022",
                    _ => "Unknown OS"
                },
                _ => $"Windows NT {majorVersion}.{minorVersion}"
            }} [{majorVersion}.{minorVersion}]";
        }

        // 获取子系统版本描述
        public static string GetSubsystemVersionDescription(ushort majorVersion, ushort minorVersion)
        {
            return $"{majorVersion switch
            {
                3 => minorVersion switch
                {
                    10 => "Windows NT 3.1",
                    51 => "Windows NT 3.51",
                    _ => "Unknown OS"
                },
                4 => minorVersion switch
                {
                    0 => "Windows NT 4.0",
                    _ => "Unknown OS"
                },
                5 => minorVersion switch
                {
                    0 => "Windows 2000",
                    1 => "Windows XP",
                    2 => "Windows Server 2003",
                    _ => "Unknown OS"
                },
                6 => minorVersion switch
                {
                    0 => "Windows Vista/Server 2008",
                    1 => "Windows 7/Server 2008 R2",
                    2 => "Windows 8/Server 2012",
                    3 => "Windows 8.1/Server 2012 R2",
                    _ => "Unknown OS"
                },
                10 => minorVersion switch
                {
                    0 => "Windows 10/11",
                    _ => "Unknown OS"
                },
                _ => $"Windows NT {majorVersion}.{minorVersion}"
            }} [{majorVersion}.{minorVersion}]";
        }

        // 获取镜像版本描述
        public static string GetImageVersionDescription(ushort majorVersion, ushort minorVersion)
        {
            // 镜像版本通常与应用程序或DLL的版本相关
            return $"Version {majorVersion}.{minorVersion}";
        }

        // 判断是否为64位PE
        internal static bool Is64Bit(IMAGEOPTIONALHEADER optionalHeader)
        {
            return optionalHeader.Magic == PEConstants.Pe32PlusMagic;
        }

        internal static bool Is32Bit(IMAGEOPTIONALHEADER optionalHeader)
        {
            return optionalHeader.Magic == PEConstants.Pe32Magic;
        }

        // 向上对齐到 4 字节边界（版本资源结构广泛使用）
        internal static long AlignTo4(long position)
        {
            return (position + 3) & ~3;
        }


        // 判断文件类型
        public static string GetFileType(ushort characteristics)
        {
            // 检查是否为驱动程序 (系统文件)
            if ((characteristics & 0x1000) != 0)
            {
                // 进一步检查子系统来确定驱动类型
                return $"Driver/System File (0x{characteristics:X4})";
            }
            else
            {
                return (characteristics & 0x2000) != 0
                    ? $"DLL (0x{characteristics:X4})"
                    : (characteristics & 0x0002) != 0 ? $"Executable (0x{characteristics:X4})" : $"Object (0x{characteristics:X4})";
            }
        }

        // 获取更详细的文件类型描述
        public static string GetDetailedFileType(ushort characteristics, ushort subsystem)
        {
            string fileType = (characteristics & 0x1000) != 0
                ? "Driver/System File"
                : (characteristics & 0x2000) != 0 ? "Dynamic Link Library" : (characteristics & 0x0002) != 0 ? "Executable" : "Object File";

            // 根据特征值判断基本类型

            return $"{fileType}{subsystem switch
            {
                1 => fileType switch { "Driver/System File" => " (Native Driver)", _ => "Windows Driver" },
                2 => " (Windows GUI Application)",
                3 => " (Windows Console Application)",
                5 => " (OS/2 Console Application)",
                7 => " (POSIX Console Application)",
                9 => " (Windows CE GUI Application)",
                10 => " (EFI Application)",
                11 => " (EFI Boot Service Driver)",
                12 => " (EFI Runtime Driver)",
                13 => " (EFI ROM)",
                14 => " (Xbox Application)",
                16 => " (Windows Boot Application)",
                _ => $" (Unknown Subsystem: {subsystem})"
            }} (Characteristics: 0x{characteristics:X4}, Subsystem: 0x{subsystem:X4})";
        }

        // 检查是否为Windows NT驱动程序
        public static string GetDriverType(ushort characteristics, ushort subsystem, ushort dllCharacteristics)
        {
            // 检查是否为系统文件
            if ((characteristics & 0x1000) == 0)
            {
                return string.Empty; // 不是系统文件，不是驱动程序
            }

            string driverType = subsystem switch
            {
                1 => "Windows NT Kernel Mode Driver",
                11 => "EFI Boot Service Driver",
                12 => "EFI Runtime Driver",
                16 => "Windows Boot Driver/Application",
                _ => "Windows Driver",
            };

            // 检查DLL特征以获取更多信息
            if ((dllCharacteristics & 0x0020) != 0) // IMAGE_DLLCHARACTERISTICS_WDM_DRIVER
            {
                driverType += " (WDM)";
            }

            return driverType;
        }

        /// <summary>
        /// 将RVA转换为文件偏移量。该原语与位数无关（仅使用 32 位节表字段），供导入/导出/CLR/资源解析共用。
        /// </summary>
        /// <param name="rva">相对虚拟地址</param>
        /// <param name="sections">节头列表</param>
        /// <returns>文件偏移量；无法解析时返回 -1</returns>
        internal static long RvaToOffset(uint rva, List<IMAGESECTIONHEADER> sections)
        {
            // 添加对RVA的基本验证
            if (rva == 0 || sections == null || sections.Count == 0)
            {
                return -1;
            }

            foreach (IMAGESECTIONHEADER section in sections)
            {
                // 确保节头有效并具有文件数据
                if (section.VirtualSize == 0 || section.SizeOfRawData == 0 || section.PointerToRawData == 0)
                {
                    continue;
                }

                // 检查RVA是否在当前节的范围内
                // 使用VirtualSize作为节在内存中的大小
                // 使用SizeOfRawData作为节在文件中的大小
                if (rva >= section.VirtualAddress && rva < (long)section.VirtualAddress + section.VirtualSize)
                {
                    // 计算相对于节起始地址的偏移量
                    uint relativeOffset = rva - section.VirtualAddress;

                    // 如果偏移量超出了文件中节的大小，则返回-1
                    // 这种情况常见于未初始化数据节(.bss等)
                    if (relativeOffset >= section.SizeOfRawData)
                    {
                        return -1;
                    }

                    // 使用 long 运算避免溢出
                    long offset = (long)section.PointerToRawData + relativeOffset;
                    if (offset >= 0)
                    {
                        return offset;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// 读取UNICODE字符串（带最大字符数限制）。
        /// </summary>
        /// <param name="reader">二进制读取器</param>
        /// <param name="maxLength">最大字符数</param>
        /// <returns>读取的字符串</returns>
        internal static string ReadUnicodeStringWithMaxLength(BinaryReader reader, int maxLength)
        {
            try
            {
                StringBuilder sb = new();
                int count = 0;

                // 限制最大读取次数以防止死循环
                int maxReadAttempts = Math.Min(maxLength, 1000);

                while (count < maxReadAttempts)
                {
                    // 检查是否还有数据可读
                    if (reader.BaseStream.Position + 2 > reader.BaseStream.Length)
                    {
                        break;
                    }

                    ushort ch = reader.ReadUInt16();
                    if (ch == 0) // NULL终止符
                    {
                        break;
                    }

                    sb.Append((char)ch);
                    count++;
                }

                return sb.ToString();
            }
            catch (IOException)
            {
                return string.Empty;
            }
            catch (UnauthorizedAccessException)
            {
                return string.Empty;
            }
            catch (ArgumentOutOfRangeException)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 读取UNICODE字符串（带最大字节数限制）。
        /// </summary>
        internal static string ReadUnicodeStringWithMaxBytes(BinaryReader reader, int maxBytes)
        {
            if (maxBytes <= 0)
            {
                return string.Empty;
            }

            return ReadUnicodeStringWithMaxLength(reader, maxBytes / 2);
        }
    }
}