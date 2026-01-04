using System.Windows.Media.Imaging;

namespace PersonalTools.PEAnalyzer.Models
{
    // PE信息类
    public class PEInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public IMAGE_DOS_HEADER DosHeader { get; set; }
        public IMAGE_NT_HEADERS NtHeaders { get; set; }
        public IMAGE_OPTIONAL_HEADER OptionalHeader { get; set; }
        public List<IMAGE_SECTION_HEADER> SectionHeaders { get; set; } = [];
        public List<ImportFunctionInfo> ImportFunctions { get; set; } = [];
        public List<ExportFunctionInfo> ExportFunctions { get; set; } = [];
        public List<DependencyInfo> Dependencies { get; set; } = [];
        public List<IconInfo> Icons { get; set; } = [];
        public CLRInfo? CLRInfo { get; set; } = null;
        public PEAdditionalInfo AdditionalInfo { get; set; } = new PEAdditionalInfo();
    }

    // PE文件附加信息类
    public class PEAdditionalInfo
    {
        public string Copyright { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string FileDescription { get; set; } = string.Empty;
        public string FileVersion { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductVersion { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string InternalName { get; set; } = string.Empty;
        public string LegalCopyright { get; set; } = string.Empty;
        public string LegalTrademarks { get; set; } = string.Empty;
        public bool IsSigned { get; set; } = false;
        public string CertificateInfo { get; set; } = string.Empty;

        // 翻译信息（来自VarFileInfo）
        public string TranslationInfo { get; set; } = string.Empty;

        // 是否已解析StringTable
        public bool StringTableParsed { get; set; } = false;

        // StringTable结束位置
        public long StringTableEndPosition { get; set; } = 0;
    }

    // 导入函数信息
    public class ImportFunctionInfo
    {
        public string DllName { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public int Ordinal { get; set; }
        public bool IsOrdinalImport { get; set; } = false;
        public bool IsDelayLoaded { get; set; } = false;  // 添加延迟加载标记

        // 添加序号显示属性，同时显示十进制和十六进制
        public string OrdinalDisplay => $"{Ordinal} (0x{Ordinal:X8})";
    }

    // 导出函数信息
    public class ExportFunctionInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Ordinal { get; set; }
        public uint RVA { get; set; }

        // 添加序号显示属性，同时显示十进制和十六进制
        public string OrdinalDisplay => $"{Ordinal} (0x{Ordinal:X8})";
    }

    // 依赖信息
    public class DependencyInfo
    {
        public string Name { get; set; } = string.Empty;
        public string ForwardedTo { get; set; } = string.Empty;
        public bool IsForwarded { get; set; }
        public List<DependencyInfo> Dependencies { get; set; } = [];
    }

    // 图标目录头
    public struct ICON_DIR_HEADER
    {
        public ushort Reserved;
        public ushort Type;
        public ushort Count;
    }

    // 图标目录项
    public struct ICON_DIR_ENTRY
    {
        public byte Width;
        public byte Height;
        public byte ColorCount;
        public byte Reserved;
        public ushort Planes;
        public ushort BitCount;
        public uint BytesInRes;
        public uint ImageOffset;
    }

    // 图标信息
    public class IconInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int BitsPerPixel { get; set; }
        public int Size { get; set; }
        public byte[] Data { get; set; } = [];
    }

    // 图标视图模型，用于在DataGrid中显示图标
    public class IconViewModel
    {
        public BitmapSource? ImageSource { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int BitsPerPixel { get; set; }
        public int Size { get; set; }
    }

    /// <summary>
    /// CLR运行时头结构
    /// </summary>
    public struct IMAGE_COR20_HEADER
    {
        public uint cb;                              // 结构大小
        public ushort MajorRuntimeVersion;           // 主版本号
        public ushort MinorRuntimeVersion;           // 次版本号
        public IMAGE_DATA_DIRECTORY MetaData;        // 元数据
        public uint Flags;                           // 标志位
        public uint EntryPointTokenOrRva;            // 入口点标记或RVA
        public IMAGE_DATA_DIRECTORY Resources;       // 资源
        public IMAGE_DATA_DIRECTORY StrongNameSignature; // 强名称签名
        public IMAGE_DATA_DIRECTORY CodeManagerTable;    // 代码管理器表
        public IMAGE_DATA_DIRECTORY VTableFixups;        // V表修复
        public IMAGE_DATA_DIRECTORY ExportAddressTableJumps; // 导出地址表跳转
        public IMAGE_DATA_DIRECTORY ManagedNativeHeader;     // 托管本地头
    }

    /// <summary>
    /// CLR信息
    /// </summary>
    public class CLRInfo
    {
        public ushort MajorRuntimeVersion { get; set; }
        public ushort MinorRuntimeVersion { get; set; }
        public uint Flags { get; set; }
        public uint EntryPointTokenOrRva { get; set; }
        public bool HasMetaData { get; set; }
        public bool HasResources { get; set; }
        public bool HasStrongNameSignature { get; set; }
        public bool IsILonly { get; set; }
        public bool Is32BitRequired { get; set; }
        public bool Is32BitPreferred { get; set; }
        public bool IsStrongNameSigned { get; set; }

        // 保存PE头中的Machine字段，用于更准确地判断架构
        public ushort PEMachineType { get; set; }

        // 获取运行时版本描述
        public string RuntimeVersion => $"{MajorRuntimeVersion}.{MinorRuntimeVersion}";

        /// <summary>
        /// 获取.NET程序的架构类型
        /// </summary>
        public string Architecture
        {
            get
            {
                // 首先根据CLR头中的标志位判断.NET程序的目标架构类型
                if (Is32BitRequired)
                {
                    return "x86"; // 明确要求32位运行
                }
                else if (!Is32BitRequired && Is32BitPreferred)
                {
                    return "x86"; // 32位首选（在64位系统上通过WoW64运行）
                }
                else if (!Is32BitRequired && !Is32BitPreferred)
                {
                    return "Any CPU"; // 可以在任何CPU架构上运行
                }

                return "Unknown"; // 无法确定的架构
            }
        }

        // 获取标志位描述
        public List<string> FlagDescriptions
        {
            get
            {
                var descriptions = new List<string>();
                if (IsILonly) descriptions.Add("IL Only");
                if (Is32BitRequired) descriptions.Add("32-Bit Required");
                if (Is32BitPreferred) descriptions.Add("32-Bit Preferred");
                if (IsStrongNameSigned) descriptions.Add("Strong Name Signed");
                return descriptions;
            }
        }
    }
}