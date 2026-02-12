using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

namespace PersonalTools.PEAnalyzer.Models
{
    // DOS头结构
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGEDOSHEADER
    {
        public ushort e_magic;       // 魔数 "MZ"
        public ushort e_cblp;
        public ushort e_cp;
        public ushort e_crlc;
        public ushort e_cparhdr;
        public ushort e_minalloc;
        public ushort e_maxalloc;
        public ushort e_ss;
        public ushort e_sp;
        public ushort e_csum;
        public ushort e_ip;
        public ushort e_cs;
        public ushort e_lfarlc;
        public ushort e_ovno;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] e_res1;
        public ushort e_oemid;
        public ushort e_oeminfo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public ushort[] e_res2;
        public uint e_lfanew;        // NT头偏移
    }

    // NT头签名
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGENTHEADERS
    {
        public uint Signature;
        public IMAGEFILEHEADER FileHeader;
        public IMAGEOPTIONALHEADER OptionalHeader;
    }

    // 文件头
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGEFILEHEADER
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    }

    // 统一的可选头结构
    internal struct IMAGEOPTIONALHEADER
    {
        public ushort Magic { get; set; }
        public byte MajorLinkerVersion { get; set; }
        public byte MinorLinkerVersion { get; set; }
        public uint SizeOfCode { get; set; }
        public uint SizeOfInitializedData { get; set; }
        public uint SizeOfUninitializedData { get; set; }
        public uint AddressOfEntryPoint { get; set; }
        public uint BaseOfCode { get; set; }
        public uint BaseOfData { get; set; }
        public ulong ImageBase { get; set; }
        public uint SectionAlignment { get; set; }
        public uint FileAlignment { get; set; }
        public ushort MajorOperatingSystemVersion { get; set; }
        public ushort MinorOperatingSystemVersion { get; set; }
        public ushort MajorImageVersion { get; set; }
        public ushort MinorImageVersion { get; set; }
        public ushort MajorSubsystemVersion { get; set; }
        public ushort MinorSubsystemVersion { get; set; }
        public uint Win32VersionValue { get; set; }
        public uint SizeOfImage { get; set; }
        public uint SizeOfHeaders { get; set; }
        public uint CheckSum { get; set; }
        public ushort Subsystem { get; set; }
        public ushort DllCharacteristics { get; set; }
        public ulong SizeOfStackReserve { get; set; }
        public ulong SizeOfStackCommit { get; set; }
        public ulong SizeOfHeapReserve { get; set; }
        public ulong SizeOfHeapCommit { get; set; }
        public uint LoaderFlags { get; set; }
        public uint NumberOfRvaAndSizes { get; set; }
        public IMAGEDATADIRECTORY[] DataDirectory { get; set; }
    }

    // 数据目录项
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGEDATADIRECTORY
    {
        public uint VirtualAddress;
        public uint Size;
    }

    // 节头
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGESECTIONHEADER
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Name;
        public uint VirtualSize;
        public uint VirtualAddress;
        public uint SizeOfRawData;
        public uint PointerToRawData;
        public uint PointerToRelocations;
        public uint PointerToLinenumbers;
        public ushort NumberOfRelocations;
        public ushort NumberOfLinenumbers;
        public uint Characteristics;
    }

    // 导入描述符
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGEIMPORTDESCRIPTOR
    {
        public uint OriginalFirstThunk;
        public uint TimeDateStamp;
        public uint ForwarderChain;
        public uint Name;
        public uint FirstThunk;
    }

    // 导出目录
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGEEXPORTDIRECTORY
    {
        public uint Characteristics;
        public uint TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public uint Name;
        public uint Base;
        public uint NumberOfFunctions;
        public uint NumberOfNames;
        public uint AddressOfFunctions;
        public uint AddressOfNames;
        public uint AddressOfNameOrdinals;
    }

    // 延迟加载导入描述符
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGEDELAYLOADDESCRIPTOR
    {
        public uint Attributes;          // 可能包含标志位
        public uint DllNameRVA;         // 指向DLL名称的RVA
        public uint ModuleHandleRVA;    // 指向模块句柄的RVA
        public uint ImportAddressTableRVA;  // 导入地址表的RVA
        public uint ImportNameTableRVA;     // 导入名称表的RVA
        public uint BoundImportAddressTableRVA; // 边界导入地址表的RVA
        public uint UnloadInformationTableRVA;  // 卸载信息表的RVA
        public uint TimeDateStamp;              // 时间戳
    }

    // 版权信息头
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGEARCHITECTUREHEADER
    {
        public uint AmaskValue;
        public uint Reserved1;  // 以前称为 Adummy1
        public uint Reserved2;  // 以前称为 Adummy2
        public uint Signature;
        public uint Reserved3;  // 以前称为 StrucLen
        public uint AddressOfData;  // RVA to the data
        public uint SizeOfData;    // 大小
    }

    // 证书结构
    [StructLayout(LayoutKind.Sequential)]
    internal struct WINCERTIFICATE
    {
        public uint dwLength;
        public ushort wRevision;
        public ushort wCertificateType;
        // BYTE bCertificate[ANYSIZE_ARRAY];  // 实际证书数据
    }

    // 版本信息结构
    [StructLayout(LayoutKind.Sequential)]
    internal struct VSFIXEDFILEINFO
    {
        public uint dwSignature;
        public uint dwStrucVersion;
        public uint dwFileVersionMS;
        public uint dwFileVersionLS;
        public uint dwProductVersionMS;
        public uint dwProductVersionLS;
        public uint dwFileFlagsMask;
        public uint dwFileFlags;
        public uint dwFileOS;
        public uint dwFileType;
        public uint dwFileSubtype;
        public uint dwFileDateMS;
        public uint dwFileDateLS;
    }

    // 资源目录项
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGERESOURCEDIRECTORYENTRY
    {
        public uint NameOrId;
        public uint OffsetToData;
    }

    // 资源目录
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGERESOURCEDIRECTORY
    {
        public uint Characteristics;
        public uint TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public ushort NumberOfNamedEntries;
        public ushort NumberOfIdEntries;
    }

    // 资源数据条目
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGERESOURCEDATAENTRY
    {
        public uint OffsetToData;
        public uint Size;
        public uint CodePage;
        public uint Reserved;
    }
}