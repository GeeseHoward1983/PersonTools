namespace PersonalTools.PEAnalyzer
{
    /// <summary>
    /// PE 头各字段的人类可读描述（机器类型 / 子系统 / 链接器/编译器/OS 版本 / 文件类型 / 驱动类型）。
    /// 把展示用描述串从 Utilities（二进制读原语）中分离，降低 Utilities 的类耦合。
    /// </summary>
    internal static class PEHeaderDescriptions
    {
        // 机器类型码 → 描述
        private static readonly Dictionary<ushort, string> s_machineTypes = new()
        {
            [0x014c] = "Intel 386 (x86)",
            [0x0162] = "MIPS R3000",
            [0x0166] = "MIPS R4000",
            [0x0168] = "MIPS R10000",
            [0x0169] = "MIPS WCI v2",
            [0x0184] = "Alpha AXP",
            [0x01A2] = "SH3",
            [0x01A3] = "SH3 DSP",
            [0x01A4] = "SH3E",
            [0x01A6] = "SH4",
            [0x01A8] = "SH5",
            [0x01C0] = "ARM",
            [0x01C2] = "ARM Thumb/Thumb-2",
            [0x01C4] = "ARM Thumb-2",
            [0x01D3] = "AM33",
            [0x01F0] = "PowerPC",
            [0x01F1] = "PowerPC FP",
            [0x0200] = "Intel IA64",
            [0x0266] = "MIPS16",
            [0x0268] = "Motorola 68000 series",
            [0x0284] = "Alpha AXP 64-bit",
            [0x0366] = "MIPS with FPU",
            [0x0466] = "MIPS16 with FPU",
            [0x0520] = "TRICORE",
            [0x0CEF] = "CEF",
            [0x0EBC] = "EFI Byte Code",
            [0x8664] = "AMD64 (x64)",
            [0x9041] = "Mitsubishi M32R",
            [0xAA64] = "ARM64",
            [0xC0EE] = "CEE",
        };

        public static string GetMachineTypeDescription(ushort machine)
        {
            return s_machineTypes.TryGetValue(machine, out string? name) ? name : $"Unknown (0x{machine:X4})";
        }

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

        public static string GetLinkerVersionDescription(byte majorVersion, byte minorVersion)
        {
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

        public static string GetCompilerVersionDescription(byte majorVersion, byte minorVersion, bool isNetAssembly = false)
        {
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

        public static string GetOperatingSystemVersionDescription(ushort majorVersion, ushort minorVersion)
            => DescribeWindowsVersion(majorVersion, minorVersion, "Windows 10/11/Server 2016/2019/2022");

        public static string GetSubsystemVersionDescription(ushort majorVersion, ushort minorVersion)
            => DescribeWindowsVersion(majorVersion, minorVersion, "Windows 10/11");

        // OS / 子系统版本描述共用核心（仅 Windows 10 主版本的描述串不同）
        private static string DescribeWindowsVersion(ushort majorVersion, ushort minorVersion, string win10Text)
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
                    0 => win10Text,
                    _ => "Unknown OS"
                },
                _ => $"Windows NT {majorVersion}.{minorVersion}"
            }} [{majorVersion}.{minorVersion}]";
        }

        public static string GetImageVersionDescription(ushort majorVersion, ushort minorVersion)
        {
            return $"Version {majorVersion}.{minorVersion}";
        }

        public static string GetDetailedFileType(ushort characteristics, ushort subsystem)
        {
            string fileType = (characteristics & 0x1000) != 0
                ? "Driver/System File"
                : (characteristics & 0x2000) != 0 ? "Dynamic Link Library" : (characteristics & 0x0002) != 0 ? "Executable" : "Object File";

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

        public static string GetDriverType(ushort characteristics, ushort subsystem, ushort dllCharacteristics)
        {
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

            if ((dllCharacteristics & 0x0020) != 0) // IMAGE_DLLCHARACTERISTICS_WDM_DRIVER
            {
                driverType += " (WDM)";
            }

            return driverType;
        }
    }
}
