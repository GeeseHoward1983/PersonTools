using System.Collections.Frozen;

namespace PersonalTools.PEAnalyzer
{
    /// <summary>
    /// PE 头字段的展示描述。版本号仅能提供工具链/系统版本族线索，不能确认编译器、IDE 或具体 OS。
    /// 查表数据与格式化逻辑分离，避免将协议常量扩展为长分支。
    /// </summary>
    internal static class PEHeaderDescriptions
    {
        private const ushort ExecutableImage = 0x0002;
        private const ushort SystemFile = 0x1000;
        private const ushort DynamicLibrary = 0x2000;
        private const ushort WdmDriver = 0x2000;
        private const ushort NativeSubsystem = 1;
        private const string UnknownToolchain = "未知厂商/工具链";

        // PE/COFF Machine 常量；保留历史架构，补充混合 ARM、RISC-V、LoongArch。
        private static readonly FrozenDictionary<ushort, string> s_machineTypes = PEHeaderDescriptionCatalog.CreateMachineTypes();

        // 同一份名称表用于子系统字段与文件类型，避免两处映射漂移。
        private static readonly FrozenDictionary<ushort, string> s_subsystems = PEHeaderDescriptionCatalog.CreateSubsystems();

        // 注意：PE Linker 7.10 对应 VS.NET 2003；MSVC 编译器/工具集的版本名是 7.1。
        private static readonly FrozenDictionary<(byte Major, byte Minor), string> s_legacyToolchains = PEHeaderDescriptionCatalog.CreateLegacyToolchains();

        // 工具集版本不是 IDE 主版本：14.4x 仍是 VS 2022，14.5x 从 VS 2026 起独立发布。
        // 这里只给版本族线索，不能据此确定实际使用的 IDE（旧工具集也能用于新 IDE）。
        private static readonly FrozenDictionary<int, string> s_msvc14Families = PEHeaderDescriptionCatalog.CreateMsvc14Families();

        // 所需版本号的产品族；不能据此判断生成/运行本文件的系统或具体构建号。
        private static readonly FrozenDictionary<(ushort Major, ushort Minor), string> s_windowsVersions = PEHeaderDescriptionCatalog.CreateWindowsVersions();

        private static readonly FrozenDictionary<ushort, string> s_fileKinds = PEHeaderDescriptionCatalog.CreateFileKinds();

        private static readonly FrozenDictionary<ushort, string> s_efiDrivers = PEHeaderDescriptionCatalog.CreateEfiDrivers();

        public static string GetMachineTypeDescription(ushort machine)
            => s_machineTypes.TryGetValue(machine, out string? name) ? name : $"Unknown (0x{machine:X4})";

        public static string GetSubsystemDescription(ushort subsystem)
            => $"{GetSubsystemName(subsystem)} (0x{subsystem:X4})";

        public static string GetLinkerVersionDescription(byte majorVersion, byte minorVersion)
            => FormatVersion($"Linker (可能的工具链: {GetToolchainFamily(majorVersion, minorVersion)})", majorVersion, minorVersion);

        // 显式重载保留原有两参数调用方式，将默认行为与托管/非托管分派分离。
        public static string GetCompilerVersionDescription(byte majorVersion, byte minorVersion)
            => GetCompilerVersionDescription(majorVersion, minorVersion, false);

        /// <summary>参数是 PE 链接器版本，不是编译器版本；CLR 目录也不能证明使用了微软编译器。</summary>
        public static string GetCompilerVersionDescription(byte majorVersion, byte minorVersion, bool isNetAssembly)
            => FormatVersion(GetCompilerIdentity(majorVersion, minorVersion, isNetAssembly), majorVersion, minorVersion);

        public static string GetOperatingSystemVersionDescription(ushort majorVersion, ushort minorVersion)
            => DescribeWindowsVersion(majorVersion, minorVersion, "所需 OS 版本族");

        public static string GetSubsystemVersionDescription(ushort majorVersion, ushort minorVersion)
            => DescribeWindowsVersion(majorVersion, minorVersion, "所需子系统版本族");

        public static string GetImageVersionDescription(ushort majorVersion, ushort minorVersion)
            => $"Version {majorVersion}.{minorVersion}";

        public static string GetDetailedFileType(ushort characteristics, ushort subsystem)
            => $"{GetFileKind(characteristics)}{GetSystemFileSuffix(characteristics)} ({GetSubsystemName(subsystem)})"
                + $" (Characteristics: 0x{characteristics:X4}, Subsystem: 0x{subsystem:X4})";

        /// <summary>识别 WDM/EFI 标志；SYSTEM+NATIVE 仅提示可能的内核驱动，Boot Application 不是驱动。</summary>
        public static string GetDriverType(ushort characteristics, ushort subsystem, ushort dllCharacteristics)
            => s_efiDrivers.TryGetValue(subsystem, out string? name)
                ? DescribeEfiDriver(name, dllCharacteristics)
                : DescribeWindowsDriver(characteristics, subsystem, dllCharacteristics);

        private static string GetSubsystemName(ushort subsystem)
            => s_subsystems.GetValueOrDefault(subsystem, "Unknown");

        private static string GetToolchainFamily(byte majorVersion, byte minorVersion)
            => majorVersion == 14 ? s_msvc14Families.GetValueOrDefault(minorVersion / 10, UnknownToolchain)
                : s_legacyToolchains.GetValueOrDefault((majorVersion, minorVersion), UnknownToolchain);

        private static string GetCompilerIdentity(byte majorVersion, byte minorVersion, bool isNetAssembly)
            => isNetAssembly ? ".NET 程序集（无法由链接器版本确定编译器或 .NET 版本）"
                : DescribeNativeCompiler(majorVersion, minorVersion);

        private static string DescribeNativeCompiler(byte majorVersion, byte minorVersion)
            => $"编译器未知（链接器版本线索: {GetToolchainFamily(majorVersion, minorVersion)}）";

        private static string FormatVersion(string description, ushort majorVersion, ushort minorVersion)
            => $"{description} [{majorVersion}.{minorVersion}]";

        private static string DescribeWindowsVersion(ushort majorVersion, ushort minorVersion, string fieldName)
            => FormatVersion($"{fieldName}: {s_windowsVersions.GetValueOrDefault((majorVersion, minorVersion), "Unknown OS")}",
                majorVersion, minorVersion);

        private static string GetFileKind(ushort characteristics)
            => s_fileKinds[(ushort)(characteristics & (ExecutableImage | DynamicLibrary))];

        private static string GetSystemFileSuffix(ushort characteristics)
            => HasFlag(characteristics, SystemFile) ? " (System File)" : string.Empty;

        private static string DescribeEfiDriver(string name, ushort dllCharacteristics)
            => HasFlag(dllCharacteristics, WdmDriver) ? $"{name} (WDM 标志与 EFI 子系统不一致)" : name;

        private static string DescribeWindowsDriver(ushort characteristics, ushort subsystem, ushort dllCharacteristics)
            => HasFlag(dllCharacteristics, WdmDriver) ? "Windows WDM Driver (WDM)"
                : DescribeNativeSystemImage(characteristics, subsystem);

        private static string DescribeNativeSystemImage(ushort characteristics, ushort subsystem)
            => HasFlag(characteristics, SystemFile) && subsystem == NativeSubsystem
                ? "Native System Image (可能为内核驱动)" : string.Empty;

        private static bool HasFlag(ushort value, ushort flag)
            => (value & flag) != 0;
    }
}
