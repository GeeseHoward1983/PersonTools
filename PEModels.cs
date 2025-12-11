using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace MyTool
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
}