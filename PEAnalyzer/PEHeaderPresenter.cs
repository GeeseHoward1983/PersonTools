using PersonalTools.PEAnalyzer.Models;
using PersonalTools.Utils;
using PersonalTools.PEAnalyzer.Parsers;

namespace PersonalTools.PEAnalyzer
{
    /// <summary>
    /// 将 PEInfo 转换为「分组标题 → 键值表」的展示数据。
    /// 把对 PE 头/可选头/CLR/节等模型与 Utilities 的引用从 UI 控件中剥离，降低控件类耦合。
    /// </summary>
    internal static class PEHeaderPresenter
    {
        // PE 头信息分组（文件/DOS/NT/可选头/CLR/节）
        public static List<(string Title, Dictionary<string, string> Info)> BuildHeaderSections(PEInfo peInfo)
        {
            List<(string, Dictionary<string, string>)> sections = [];

            Dictionary<string, string> fileInfo = new()
            {
                { "文件路径", peInfo.FilePath },
                { "文件类型", PEHeaderDescriptions.GetDetailedFileType(peInfo.NtHeaders.FileHeader.Characteristics, peInfo.OptionalHeader.Subsystem) },
                { "架构", GetArchitectureInfo(peInfo) },
                { "位数", GetBitInfo(peInfo) }
            };
            string driverType = PEHeaderDescriptions.GetDriverType(peInfo.NtHeaders.FileHeader.Characteristics, peInfo.OptionalHeader.Subsystem, peInfo.OptionalHeader.DllCharacteristics);
            if (!string.IsNullOrEmpty(driverType))
            {
                fileInfo.Add("驱动程序类型", driverType);
            }
            sections.Add(("文件信息", fileInfo));

            sections.Add(("DOS头信息", new Dictionary<string, string>
            {
                { "签名(e_magic)", $"0x{peInfo.DosHeader.e_magic:X4} ('{ToPrintableChar(peInfo.DosHeader.e_magic & 0xFF)}{ToPrintableChar(peInfo.DosHeader.e_magic >> 8)}')" },
                { "NT头偏移(e_lfanew)", $"0x{peInfo.DosHeader.e_lfanew:X8}" }
            }));

            sections.Add(("NT头信息", new Dictionary<string, string>
            {
                { "签名", $"0x{peInfo.NtHeaders.Signature:X8}" },
                { "机器类型", PEHeaderDescriptions.GetMachineTypeDescription(peInfo.NtHeaders.FileHeader.Machine) },
                { "节数量", $"0x{peInfo.NtHeaders.FileHeader.NumberOfSections:X4}" },
                { "时间戳", $"{peInfo.NtHeaders.FileHeader.TimeDateStamp:X8} ({UnixTimeStampToDateTime(peInfo.NtHeaders.FileHeader.TimeDateStamp):yyyy-MM-dd HH:mm:ss})" },
                { "可选头大小", $"0x{peInfo.NtHeaders.FileHeader.SizeOfOptionalHeader:X4}" },
                { "特征标志", $"0x{peInfo.NtHeaders.FileHeader.Characteristics:X4}" }
            }));

            sections.Add(("可选头信息", new Dictionary<string, string>
            {
                { "魔数", $"0x{peInfo.OptionalHeader.Magic:X4} ({peInfo.OptionalHeader.Magic switch { 0x10b => "PE32", 0x20b => "PE32+", _ => "Unknown" }})" },
                { "链接器版本", $"{PEHeaderDescriptions.GetLinkerVersionDescription(peInfo.OptionalHeader.MajorLinkerVersion, peInfo.OptionalHeader.MinorLinkerVersion)}" },
                { "编译器版本", $"{PEHeaderDescriptions.GetCompilerVersionDescription(peInfo.OptionalHeader.MajorLinkerVersion, peInfo.OptionalHeader.MinorLinkerVersion, peInfo.CLRInfo != null)}" },
                { "代码大小", $"0x{peInfo.OptionalHeader.SizeOfCode:X8}" },
                { "已初始化数据大小", $"0x{peInfo.OptionalHeader.SizeOfInitializedData:X8}" },
                { "未初始化数据大小", $"0x{peInfo.OptionalHeader.SizeOfUninitializedData:X8}" },
                { "入口点RVA", $"0x{peInfo.OptionalHeader.AddressOfEntryPoint:X8}" },
                { "代码基址", $"0x{peInfo.OptionalHeader.BaseOfCode:X8}" },
                { "数据基址", PEParserUtils.Is64Bit(peInfo.OptionalHeader) ? "N/A (PE32+)" : $"0x{peInfo.OptionalHeader.BaseOfData:X8}" },
                { "镜像基址", $"0x{peInfo.OptionalHeader.ImageBase:X8}" },
                { "节对齐", $"0x{peInfo.OptionalHeader.SectionAlignment:X8}" },
                { "文件对齐", $"0x{peInfo.OptionalHeader.FileAlignment:X8}" },
                { "操作系统版本", PEHeaderDescriptions.GetOperatingSystemVersionDescription(peInfo.OptionalHeader.MajorOperatingSystemVersion, peInfo.OptionalHeader.MinorOperatingSystemVersion) },
                { "镜像版本", PEHeaderDescriptions.GetImageVersionDescription(peInfo.OptionalHeader.MajorImageVersion, peInfo.OptionalHeader.MinorImageVersion) },
                { "子系统版本", PEHeaderDescriptions.GetSubsystemVersionDescription(peInfo.OptionalHeader.MajorSubsystemVersion, peInfo.OptionalHeader.MinorSubsystemVersion) },
                { "镜像大小", $"0x{peInfo.OptionalHeader.SizeOfImage:X8}" },
                { "头部大小", $"0x{peInfo.OptionalHeader.SizeOfHeaders:X8}" },
                { "校验和", $"0x{peInfo.OptionalHeader.CheckSum:X8}" },
                { "子系统", PEHeaderDescriptions.GetSubsystemDescription(peInfo.OptionalHeader.Subsystem) },
                { "DLL特征", $"0x{peInfo.OptionalHeader.DllCharacteristics:X4}" },
                { "栈保留大小", $"0x{peInfo.OptionalHeader.SizeOfStackReserve:X8}" },
                { "栈提交大小", $"0x{peInfo.OptionalHeader.SizeOfStackCommit:X8}" },
                { "堆保留大小", $"0x{peInfo.OptionalHeader.SizeOfHeapReserve:X8}" },
                { "堆提交大小", $"0x{peInfo.OptionalHeader.SizeOfHeapCommit:X8}" },
                { "数据目录数量", $"0x{peInfo.OptionalHeader.NumberOfRvaAndSizes:X8}" }
            }));

            if (peInfo.CLRInfo != null)
            {
                sections.Add((".NET CLR信息", new Dictionary<string, string>
                {
                    { "运行时版本", $"v{peInfo.CLRInfo.RuntimeVersion}" },
                    { "架构类型", peInfo.CLRInfo.Architecture },
                    { "标志位", ConvertUtils.EnumerableToString(", ", peInfo.CLRInfo.FlagDescriptions) },
                    { "入口点", $"0x{peInfo.CLRInfo.EntryPointTokenOrRva:X8}" },
                    { "包含元数据", peInfo.CLRInfo.HasMetaData ? "是" : "否" },
                    { "包含资源", peInfo.CLRInfo.HasResources ? "是" : "否" },
                    { "强名称签名", peInfo.CLRInfo.HasStrongNameSignature ? "是" : "否" }
                }));
            }

            sections.Add(("节信息", CreateSectionInfo(peInfo)));
            return sections;
        }

        // 附加信息分组（版本/公司/版权/证书/翻译）
        public static List<(string Title, Dictionary<string, string> Info)> BuildAdditionalSections(PEInfo peInfo)
        {
            List<(string, Dictionary<string, string>)> sections =
            [
                ("版本信息", new Dictionary<string, string>
                {
                    { "文件版本", peInfo.AdditionalInfo.FileVersion },
                    { "产品版本", peInfo.AdditionalInfo.ProductVersion }
                }),
                ("公司和产品信息", new Dictionary<string, string>
                {
                    { "公司名称", peInfo.AdditionalInfo.CompanyName },
                    { "产品名称", peInfo.AdditionalInfo.ProductName },
                    { "文件描述", peInfo.AdditionalInfo.FileDescription }
                }),
                ("版权和商标信息", new Dictionary<string, string>
                {
                    { "版权信息", peInfo.AdditionalInfo.LegalCopyright },
                    { "商标信息", peInfo.AdditionalInfo.LegalTrademarks },
                    { "原始文件名", peInfo.AdditionalInfo.OriginalFileName },
                    { "内部名称", peInfo.AdditionalInfo.InternalName }
                }),
                ("证书信息", new Dictionary<string, string>
                {
                    { "是否签名", peInfo.AdditionalInfo.IsSigned ? "是" : "否" },
                    { "证书详情", peInfo.AdditionalInfo.CertificateInfo }
                }),
            ];

            if (!string.IsNullOrEmpty(peInfo.AdditionalInfo.TranslationInfo))
            {
                sections.Add(("翻译信息", new Dictionary<string, string>
                {
                    { "语言和代码页", peInfo.AdditionalInfo.TranslationInfo }
                }));
            }

            return sections;
        }

        private static Dictionary<string, string> CreateSectionInfo(PEInfo peInfo)
        {
            Dictionary<string, string> sectionInfo = [];

            for (int i = 0; i < peInfo.SectionHeaders.Count; i++)
            {
                IMAGE_SECTION_HEADER section = peInfo.SectionHeaders[i];
                string sectionName = System.Text.Encoding.Latin1.GetString(section.Name).Trim('\0');
                sectionInfo[$"节 {i} ({sectionName})"] = $"RVA: 0x{section.VirtualAddress:X8}, 大小: 0x{section.VirtualSize:X8}, Raw大小: 0x{section.SizeOfRawData:X8}, 特征: 0x{section.Characteristics:X8}";
            }

            sectionInfo["总计"] = $"{peInfo.SectionHeaders.Count} 个节";
            return sectionInfo;
        }

        private static string GetArchitectureInfo(PEInfo peInfo)
        {
            return peInfo.CLRInfo != null
                ? $"{PEHeaderDescriptions.GetMachineTypeDescription(peInfo.NtHeaders.FileHeader.Machine)} (.NET: {peInfo.CLRInfo.Architecture})"
                : PEHeaderDescriptions.GetMachineTypeDescription(peInfo.NtHeaders.FileHeader.Machine);
        }

        private static string GetBitInfo(PEInfo peInfo)
        {
            return peInfo.CLRInfo switch
            {
                null => PEParserUtils.Is64Bit(peInfo.OptionalHeader) ? "64位" : "32位",
                _ => peInfo.CLRInfo.Architecture switch
                {
                    "x86" => "32位",
                    "x64" => "64位",
                    "ARM64" => "64位",
                    "Any CPU" => "Any CPU",
                    _ => "未知"
                }
            };
        }

        // 仅当字节可打印（ASCII 0x20~0x7E）时转为字符，否则用 '.' 占位，避免控制字节破坏排版
        private static char ToPrintableChar(int value)
        {
            int b = value & 0xFF;
            return b >= 0x20 && b < 0x7F ? (char)b : '.';
        }

        private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            try
            {
                // FromUnixTimeSeconds 接受 long 秒，有效范围远宽于 uint 时间戳；
                // 极端越界值（理论上的畸形输入）会抛 ArgumentOutOfRangeException，捕获后夹紧到最小值兜底，避免冒泡。
                return DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp).LocalDateTime;
            }
            catch (ArgumentOutOfRangeException)
            {
                return DateTimeOffset.UnixEpoch.LocalDateTime;
            }
        }
    }
}
