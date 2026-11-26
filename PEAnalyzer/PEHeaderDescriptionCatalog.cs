using System.Collections.Frozen;
using System.ComponentModel;
using System.Reflection;

namespace PersonalTools.PEAnalyzer
{
    /// <summary>
    /// 强类型 PE 展示数据。每个协议项独立声明，通用构建器仅在静态初始化时读取协议码与描述元数据。
    /// 不使用长集合初始化器、运行时文本解析或未经检查的外部资源。
    /// </summary>
    internal static class PEHeaderDescriptionCatalog
    {
        internal static FrozenDictionary<ushort, string> CreateMachineTypes()
            => CreateMap<MachineEntry, ushort>(ReadUShortCode);

        internal static FrozenDictionary<ushort, string> CreateSubsystems()
            => CreateMap<SubsystemEntry, ushort>(ReadUShortCode);

        internal static FrozenDictionary<(byte Major, byte Minor), string> CreateLegacyToolchains()
            => CreateMap<LegacyToolchainEntry, (byte, byte)>(GetLinkerVersion);

        internal static FrozenDictionary<int, string> CreateMsvc14Families()
            => CreateMap<Msvc14FamilyEntry, int>(ReadIntCode);

        internal static FrozenDictionary<(ushort Major, ushort Minor), string> CreateWindowsVersions()
            => CreateMap<WindowsVersionEntry, (ushort, ushort)>(GetWindowsVersion);

        internal static FrozenDictionary<ushort, string> CreateFileKinds()
            => CreateMap<FileKindEntry, ushort>(ReadUShortCode);

        internal static FrozenDictionary<ushort, string> CreateEfiDrivers()
            => CreateMap<EfiDriverEntry, ushort>(ReadUShortCode);

        private static FrozenDictionary<TKey, string> CreateMap<TEnum, TKey>(Func<TEnum, TKey> keySelector)
            where TEnum : struct, Enum
            where TKey : notnull
            => Enum.GetValues<TEnum>().ToFrozenDictionary(keySelector, ReadDescription);

        // 枚举项仅是目录标识，真实协议码在同一条元数据中，避免重复维护值与描述。
        private static string ReadDescription<TEnum>(TEnum value) where TEnum : struct, Enum
            => ReadEntry(value).Description;

        private static PEHeaderDescriptionAttribute ReadEntry<TEnum>(TEnum value) where TEnum : struct, Enum
            => typeof(TEnum).GetField(value.ToString())!.GetCustomAttribute<PEHeaderDescriptionAttribute>()!;

        private static ushort ReadUShortCode<TEnum>(TEnum value) where TEnum : struct, Enum
            => checked((ushort)ReadEntry(value).Code);

        private static int ReadIntCode<TEnum>(TEnum value) where TEnum : struct, Enum
            => checked((int)ReadEntry(value).Code);

        // 链接器键按 (Major << 8) | Minor 编码；7.10 的 Minor 是十进制 10，不是 1。
        private static (byte Major, byte Minor) GetLinkerVersion(LegacyToolchainEntry value)
            => SplitLinkerVersion(ReadUShortCode(value));

        private static (byte Major, byte Minor) SplitLinkerVersion(ushort code)
            => ((byte)(code >> 8), (byte)(code & byte.MaxValue));

        // Windows 键按 (Major << 16) | Minor 编码，完整保留两个 ushort。
        private static (ushort Major, ushort Minor) GetWindowsVersion(WindowsVersionEntry value)
            => SplitWindowsVersion(ReadEntry(value).Code);

        private static (ushort Major, ushort Minor) SplitWindowsVersion(uint code)
            => ((ushort)(code >> 16), (ushort)(code & ushort.MaxValue));

        [AttributeUsage(AttributeTargets.Field)]
        private sealed class PEHeaderDescriptionAttribute(uint code, string description) : DescriptionAttribute(description)
        {
            public uint Code => code;
        }

        private enum MachineEntry
        {
            [PEHeaderDescription(0x00000000, "Unknown")] Unknown,
            [PEHeaderDescription(0x0000014C, "Intel 386 (x86)")] I386,
            [PEHeaderDescription(0x00000160, "MIPS R3000 (big-endian)")] R3000BigEndian,
            [PEHeaderDescription(0x00000162, "MIPS R3000")] R3000,
            [PEHeaderDescription(0x00000166, "MIPS R4000")] R4000,
            [PEHeaderDescription(0x00000168, "MIPS R10000")] R10000,
            [PEHeaderDescription(0x00000169, "MIPS WCE v2")] WceMipsV2,
            [PEHeaderDescription(0x00000184, "Alpha AXP")] Alpha,
            [PEHeaderDescription(0x000001A2, "SH3")] Sh3,
            [PEHeaderDescription(0x000001A3, "SH3 DSP")] Sh3Dsp,
            [PEHeaderDescription(0x000001A4, "SH3E")] Sh3E,
            [PEHeaderDescription(0x000001A6, "SH4")] Sh4,
            [PEHeaderDescription(0x000001A8, "SH5")] Sh5,
            [PEHeaderDescription(0x000001C0, "ARM")] Arm,
            [PEHeaderDescription(0x000001C2, "ARM Thumb")] Thumb,
            [PEHeaderDescription(0x000001C4, "ARM Thumb-2")] ArmNt,
            [PEHeaderDescription(0x000001D3, "AM33")] Am33,
            [PEHeaderDescription(0x000001F0, "PowerPC")] PowerPc,
            [PEHeaderDescription(0x000001F1, "PowerPC FP")] PowerPcFp,
            [PEHeaderDescription(0x00000200, "Intel IA64")] Ia64,
            [PEHeaderDescription(0x00000266, "MIPS16")] Mips16,
            [PEHeaderDescription(0x00000284, "Alpha AXP 64-bit")] Alpha64,
            [PEHeaderDescription(0x00000366, "MIPS with FPU")] MipsFpu,
            [PEHeaderDescription(0x00000466, "MIPS16 with FPU")] MipsFpu16,
            [PEHeaderDescription(0x00000520, "TRICORE")] TriCore,
            [PEHeaderDescription(0x00000CEF, "CEF")] Cef,
            [PEHeaderDescription(0x00000EBC, "EFI Byte Code")] Ebc,
            [PEHeaderDescription(0x00005032, "RISC-V 32-bit")] RiscV32,
            [PEHeaderDescription(0x00005064, "RISC-V 64-bit")] RiscV64,
            [PEHeaderDescription(0x00005128, "RISC-V 128-bit")] RiscV128,
            [PEHeaderDescription(0x00006232, "LoongArch 32-bit")] LoongArch32,
            [PEHeaderDescription(0x00006264, "LoongArch 64-bit")] LoongArch64,
            [PEHeaderDescription(0x00008664, "AMD64 (x64)")] Amd64,
            [PEHeaderDescription(0x00009041, "Mitsubishi M32R")] M32R,
            [PEHeaderDescription(0x0000A641, "ARM64EC")] Arm64Ec,
            [PEHeaderDescription(0x0000A64E, "ARM64X")] Arm64X,
            [PEHeaderDescription(0x0000AA64, "ARM64")] Arm64,
            [PEHeaderDescription(0x0000C0EE, "CEE")] Cee,
        }

        private enum SubsystemEntry
        {
            [PEHeaderDescription(0x00000000, "Unknown")] Unknown,
            [PEHeaderDescription(0x00000001, "Native")] Native,
            [PEHeaderDescription(0x00000002, "Windows GUI")] WindowsGui,
            [PEHeaderDescription(0x00000003, "Windows CUI")] WindowsCui,
            [PEHeaderDescription(0x00000005, "OS/2 CUI")] Os2Cui,
            [PEHeaderDescription(0x00000007, "POSIX CUI")] PosixCui,
            [PEHeaderDescription(0x00000008, "Native Windows")] NativeWindows,
            [PEHeaderDescription(0x00000009, "Windows CE GUI")] WindowsCeGui,
            [PEHeaderDescription(0x0000000A, "EFI Application")] EfiApplication,
            [PEHeaderDescription(0x0000000B, "EFI Boot Service Driver")] EfiBootServiceDriver,
            [PEHeaderDescription(0x0000000C, "EFI Runtime Driver")] EfiRuntimeDriver,
            [PEHeaderDescription(0x0000000D, "EFI ROM")] EfiRom,
            [PEHeaderDescription(0x0000000E, "XBOX")] Xbox,
            [PEHeaderDescription(0x00000010, "Windows Boot Application")] WindowsBootApplication,
            [PEHeaderDescription(0x00000011, "Xbox Code Catalog")] XboxCodeCatalog,
        }

        private enum LegacyToolchainEntry
        {
            [PEHeaderDescription(0x00000600, "Microsoft Visual C++ 6.0")] VisualCpp6,
            [PEHeaderDescription(0x00000700, "Microsoft Visual Studio .NET 2002")] VisualStudioNet2002,
            [PEHeaderDescription(0x0000070A, "Microsoft Visual Studio .NET 2003")] VisualStudioNet2003,
            [PEHeaderDescription(0x00000800, "Microsoft Visual Studio 2005")] VisualStudio2005,
            [PEHeaderDescription(0x00000900, "Microsoft Visual Studio 2008")] VisualStudio2008,
            [PEHeaderDescription(0x00000A00, "Microsoft Visual Studio 2010")] VisualStudio2010,
            [PEHeaderDescription(0x00000B00, "Microsoft Visual Studio 2012")] VisualStudio2012,
            [PEHeaderDescription(0x00000C00, "Microsoft Visual Studio 2013")] VisualStudio2013,
        }

        private enum Msvc14FamilyEntry
        {
            [PEHeaderDescription(0x00000000, "MSVC 14.0x / VS 2015")] VisualStudio2015,
            [PEHeaderDescription(0x00000001, "MSVC 14.1x / VS 2017")] VisualStudio2017,
            [PEHeaderDescription(0x00000002, "MSVC 14.2x / VS 2019")] VisualStudio2019,
            [PEHeaderDescription(0x00000003, "MSVC 14.3x / VS 2022")] VisualStudio2022Early,
            [PEHeaderDescription(0x00000004, "MSVC 14.4x / VS 2022")] VisualStudio2022Later,
            [PEHeaderDescription(0x00000005, "MSVC 14.5x / VS 2026 起（工具集独立版本）")] VisualStudio2026AndLater,
        }

        private enum WindowsVersionEntry
        {
            [PEHeaderDescription(0x0003000A, "Windows NT 3.1")] Nt31,
            [PEHeaderDescription(0x00030032, "Windows NT 3.5")] Nt35,
            [PEHeaderDescription(0x00030033, "Windows NT 3.51")] Nt351,
            [PEHeaderDescription(0x00040000, "Windows NT 4.0/Windows 95")] Nt4OrWindows95,
            [PEHeaderDescription(0x0004000A, "Windows 98")] Windows98,
            [PEHeaderDescription(0x0004005A, "Windows Me")] WindowsMe,
            [PEHeaderDescription(0x00050000, "Windows 2000")] Windows2000,
            [PEHeaderDescription(0x00050001, "Windows XP")] WindowsXp,
            [PEHeaderDescription(0x00050002, "Windows XP x64/Server 2003/Server 2003 R2")] WindowsXp64OrServer2003,
            [PEHeaderDescription(0x00060000, "Windows Vista/Server 2008")] VistaOrServer2008,
            [PEHeaderDescription(0x00060001, "Windows 7/Server 2008 R2")] Windows7OrServer2008R2,
            [PEHeaderDescription(0x00060002, "Windows 8/Server 2012")] Windows8OrServer2012,
            [PEHeaderDescription(0x00060003, "Windows 8.1/Server 2012 R2")] Windows81OrServer2012R2,
            [PEHeaderDescription(0x000A0000, "Windows 10/11/Server 2016/2019/2022/2025")] Windows10Family,
        }

        private enum FileKindEntry
        {
            [PEHeaderDescription(0x00000000, "Object File")] Object,
            [PEHeaderDescription(0x00000002, "Executable")] Executable,
            [PEHeaderDescription(0x00002000, "Dynamic Link Library")] Dll,
            [PEHeaderDescription(0x00002002, "Dynamic Link Library")] ExecutableDll,
        }

        private enum EfiDriverEntry
        {
            [PEHeaderDescription(0x0000000B, "EFI Boot Service Driver")] BootService,
            [PEHeaderDescription(0x0000000C, "EFI Runtime Driver")] Runtime,
        }
    }
}
