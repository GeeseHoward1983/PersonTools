using PersonalTools.PEAnalyzer.Models;
using System.IO;
using System.Text;

namespace PersonalTools.Parsers
{
    public static class Utilties
    {
        public static string ReadNullTerminatedString(BinaryReader reader)
        {
            var sb = new StringBuilder();
            try
            {
                byte b;

                while ((b = reader.ReadByte()) != 0)
                {
                    // 确保是有效的ASCII字符
                    if (b >= 32 && b <= 126)
                    {
                        sb.Append((char)b);
                    }
                    else if (b == 9 || b == 10 || b == 13)
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
            catch (Exception)
            {
                // 发生异常时返回已读取的部分字符串
            }

            return sb.ToString();
        }

        // 获取机器类型描述
        public static string GetMachineTypeDescription(ushort machine)
        {
            return machine switch
            {
                0x014c => "Intel 386 (x88)",
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
            string linkerName;
            switch (majorVersion)
            {
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                    linkerName = "Microsoft Linker (早期版本)";
                    break;
                case 6:
                    linkerName = "Microsoft Linker 6.x";
                    break;
                case 7:
                    linkerName = "Microsoft Linker 7.x (VS.NET 2002)";
                    break;
                case 8:
                    linkerName = "Microsoft Linker 8.x (VS 2005)";
                    break;
                case 9:
                    linkerName = "Microsoft Linker 9.x (VS 2008)";
                    break;
                case 10:
                    linkerName = "Microsoft Linker 10.x (VS 2010)";
                    break;
                case 11:
                    linkerName = "Microsoft Linker 11.x (VS 2012)";
                    break;
                case 12:
                    linkerName = "Microsoft Linker 12.x (VS 2013)";
                    break;
                case 14:
                    if (minorVersion == 10)
                        linkerName = "Microsoft Linker 14.10 (VS 2015)";
                    else if (minorVersion == 20)
                        linkerName = "Microsoft Linker 14.20 (VS 2017)";
                    else if (minorVersion >= 26 && minorVersion <= 29)
                        linkerName = "Microsoft Linker 14.26-14.29 (VS 2019)";
                    else if (minorVersion >= 30)
                        linkerName = "Microsoft Linker 14.30+ (VS 2022)";
                    else
                        linkerName = "Microsoft Linker 14.x (VS 2015/2017/2019)";
                    break;
                case 15:
                    linkerName = "Microsoft Linker 15.x";
                    break;
                default:
                    if (majorVersion > 15)
                        linkerName = $"Microsoft Linker {majorVersion}.{minorVersion} (较新版本)";
                    else
                        linkerName = $"Microsoft Linker {majorVersion}.{minorVersion} (未知版本)";
                    break;
            }

            return $"{linkerName} [{majorVersion}.{minorVersion}]";
        }

        // 获取编译器版本描述
        public static string GetCompilerVersionDescription(byte majorVersion, byte minorVersion, bool isNetAssembly = false)
        {
            // 编译器版本信息通常可以从链接器版本推断
            string compilerInfo;
            switch (majorVersion)
            {
                case 6:
                    compilerInfo = "Microsoft Visual C++ 6.0";
                    break;
                case 7:
                    compilerInfo = "Microsoft Visual C++ .NET 2002";
                    break;
                case 8:
                    compilerInfo = "Microsoft Visual C++ 2005";
                    break;
                case 9:
                    compilerInfo = "Microsoft Visual C++ 2008";
                    break;
                case 10:
                    compilerInfo = "Microsoft Visual C++ 2010";
                    break;
                case 11:
                    compilerInfo = "Microsoft Visual C++ 2012";
                    break;
                case 12:
                    compilerInfo = "Microsoft Visual C++ 2013";
                    break;
                case 14:
                    if (minorVersion == 10)
                        compilerInfo = "Microsoft Visual C++ 2015";
                    else if (minorVersion == 20)
                        compilerInfo = "Microsoft Visual C++ 2017";
                    else if (minorVersion >= 26 && minorVersion <= 29)
                        compilerInfo = "Microsoft Visual C++ 2019";
                    else if (minorVersion >= 30)
                        compilerInfo = "Microsoft Visual C++ 2022";
                    else
                        compilerInfo = "Microsoft Visual C++ 2015/2017/2019";
                    break;
                case 15:
                    compilerInfo = "Microsoft Visual C++ ???";
                    break;
                default:
                    if (isNetAssembly)
                        compilerInfo = "Microsoft .NET Compiler";
                    else if (majorVersion > 15)
                        compilerInfo = $"Microsoft Visual C++ (较新版本)";
                    else
                        compilerInfo = $"Microsoft Visual C++ (未知版本)";
                    break;
            }

            return $"{compilerInfo} [{majorVersion}.{minorVersion}]";
        }

        // 获取操作系统版本描述
        public static string GetOperatingSystemVersionDescription(ushort majorVersion, ushort minorVersion)
        {
            string osDescription = "Unknown OS";
            switch (majorVersion)
            {
                case 3:
                    if (minorVersion == 10) osDescription = "Windows NT 3.1";
                    else if (minorVersion == 51) osDescription = "Windows NT 3.51";
                    break;
                case 4:
                    if (minorVersion == 0) osDescription = "Windows NT 4.0";
                    break;
                case 5:
                    if (minorVersion == 0) osDescription = "Windows 2000";
                    else if (minorVersion == 1) osDescription = "Windows XP";
                    else if (minorVersion == 2) osDescription = "Windows Server 2003";
                    break;
                case 6:
                    if (minorVersion == 0) osDescription = "Windows Vista/Server 2008";
                    else if (minorVersion == 1) osDescription = "Windows 7/Server 2008 R2";
                    else if (minorVersion == 2) osDescription = "Windows 8/Server 2012";
                    else if (minorVersion == 3) osDescription = "Windows 8.1/Server 2012 R2";
                    break;
                case 10:
                    if (minorVersion == 0) osDescription = "Windows 10/11/Server 2016/2019/2022";
                    break;
                default:
                    osDescription = $"Windows NT {majorVersion}.{minorVersion}";
                    break;
            }

            return $"{osDescription} [{majorVersion}.{minorVersion}]";
        }

        // 获取子系统版本描述
        public static string GetSubsystemVersionDescription(ushort majorVersion, ushort minorVersion)
        {
            string subsystemDescription = "Unknown Subsystem Version";
            // 子系统版本通常与目标操作系统有关
            switch (majorVersion)
            {
                case 3:
                    if (minorVersion == 10) subsystemDescription = "Windows NT 3.1";
                    else if (minorVersion == 51) subsystemDescription = "Windows NT 3.51";
                    break;
                case 4:
                    if (minorVersion == 0) subsystemDescription = "Windows NT 4.0";
                    break;
                case 5:
                    if (minorVersion == 0) subsystemDescription = "Windows 2000";
                    else if (minorVersion == 1) subsystemDescription = "Windows XP";
                    else if (minorVersion == 2) subsystemDescription = "Windows Server 2003";
                    break;
                case 6:
                    if (minorVersion == 0) subsystemDescription = "Windows Vista/Server 2008";
                    else if (minorVersion == 1) subsystemDescription = "Windows 7/Server 2008 R2";
                    else if (minorVersion == 2) subsystemDescription = "Windows 8/Server 2012";
                    else if (minorVersion == 3) subsystemDescription = "Windows 8.1/Server 2012 R2";
                    break;
                case 10:
                    if (minorVersion == 0) subsystemDescription = "Windows 10/11";
                    break;
                default:
                    subsystemDescription = $"Windows NT {majorVersion}.{minorVersion}";
                    break;
            }

            return $"{subsystemDescription} [{majorVersion}.{minorVersion}]";
        }

        // 获取镜像版本描述
        public static string GetImageVersionDescription(ushort majorVersion, ushort minorVersion)
        {
            // 镜像版本通常与应用程序或DLL的版本相关
            return $"Version {majorVersion}.{minorVersion}";
        }

        // 判断是否为64位PE
        public static bool Is64Bit(IMAGE_OPTIONAL_HEADER optionalHeader)
        {
            return optionalHeader.Magic == 0x20b;
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
            else if ((characteristics & 0x2000) != 0)
            {
                return $"DLL (0x{characteristics:X4})";
            }
            else if ((characteristics & 0x0002) != 0)
            {
                return $"Executable (0x{characteristics:X4})";
            }
            else
            {
                return $"Object (0x{characteristics:X4})";
            }
        }

        // 获取更详细的文件类型描述
        public static string GetDetailedFileType(ushort characteristics, ushort subsystem)
        {
            string fileType;

            // 根据特征值判断基本类型
            if ((characteristics & 0x1000) != 0) // 系统文件标志
            {
                fileType = "Driver/System File";
            }
            else if ((characteristics & 0x2000) != 0) // DLL标志
            {
                fileType = "Dynamic Link Library";
            }
            else if ((characteristics & 0x0002) != 0) // 可执行文件标志
            {
                fileType = "Executable";
            }
            else
            {
                fileType = "Object File";
            }

            // 根据子系统进一步细化
            string subsystemDetail;
            switch (subsystem)
            {
                case 1: // Native
                    if (fileType == "Driver/System File")
                        subsystemDetail = " (Native Driver)";
                    else
                        subsystemDetail = " (Native Application)";
                    break;
                case 2: // Windows GUI
                    subsystemDetail = " (Windows GUI Application)";
                    break;
                case 3: // Windows CUI
                    subsystemDetail = " (Windows Console Application)";
                    break;
                case 5: // OS/2 CUI
                    subsystemDetail = " (OS/2 Console Application)";
                    break;
                case 7: // POSIX CUI
                    subsystemDetail = " (POSIX Console Application)";
                    break;
                case 9: // Windows CE GUI
                    subsystemDetail = " (Windows CE GUI Application)";
                    break;
                case 10: // EFI Application
                    subsystemDetail = " (EFI Application)";
                    break;
                case 11: // EFI Boot Service Driver
                    subsystemDetail = " (EFI Boot Service Driver)";
                    break;
                case 12: // EFI Runtime Driver
                    subsystemDetail = " (EFI Runtime Driver)";
                    break;
                case 13: // EFI ROM
                    subsystemDetail = " (EFI ROM)";
                    break;
                case 14: // XBOX
                    subsystemDetail = " (Xbox Application)";
                    break;
                case 16: // Windows Boot Application
                    subsystemDetail = " (Windows Boot Application)";
                    break;
                default:
                    subsystemDetail = $" (Unknown Subsystem: {subsystem})";
                    break;
            }

            return $"{fileType}{subsystemDetail} (Characteristics: 0x{characteristics:X4}, Subsystem: 0x{subsystem:X4})";
        }

        // 检查是否为Windows NT驱动程序
        public static string GetDriverType(ushort characteristics, ushort subsystem, ushort dllCharacteristics)
        {
            // 检查是否为系统文件
            if ((characteristics & 0x1000) == 0)
                return string.Empty; // 不是系统文件，不是驱动程序

            string driverType = "Windows Driver";

            // 根据子系统类型进一步分类
            switch (subsystem)
            {
                case 1: // Native
                    driverType = "Windows NT Kernel Mode Driver";
                    break;
                case 11: // EFI Boot Service Driver
                    driverType = "EFI Boot Service Driver";
                    break;
                case 12: // EFI Runtime Driver
                    driverType = "EFI Runtime Driver";
                    break;
                case 16: // Windows Boot Application
                    driverType = "Windows Boot Driver/Application";
                    break;
            }

            // 检查DLL特征以获取更多信息
            if ((dllCharacteristics & 0x0020) != 0) // IMAGE_DLLCHARACTERISTICS_WDM_DRIVER
                driverType += " (WDM)";

            return driverType;
        }
    }
}