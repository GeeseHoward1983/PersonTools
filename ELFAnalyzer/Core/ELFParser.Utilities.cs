using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        public string GetArchitectureName()
        {
            if (Enum.IsDefined(typeof(EMachine), _header.e_machine))
            {
                return Enum.GetName(typeof(EMachine), _header.e_machine)?.Replace("EM_", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        public string GetELFClassName()
        {
            if (Enum.IsDefined(typeof(ELFClass), _header.EI_CLASS))
            {
                return Enum.GetName(typeof(ELFClass), _header.EI_CLASS)?.Replace("CLASS", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        public string GetELFDataName()
        {
            if (Enum.IsDefined(typeof(ELFData), _header.EI_DATA))
            {
                return Enum.GetName(typeof(ELFData), _header.EI_DATA)?.Replace("ELFDATA", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        public string GetELFTypeName()
        {
            if (Enum.IsDefined(typeof(ELFType), _header.e_type))
            {
                return Enum.GetName(typeof(ELFType), _header.e_type)?.Replace("ET_", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        public string? GetOSABIName()
        {
            return _header.EI_OSABI switch
            {
                0 => "UNIX - System V",
                1 => "HP-UX",
                2 => "NetBSD",
                3 => "Linux",
                4 => "GNU Hurd",
                6 => "Solaris",
                7 => "AIX",
                8 => "IRIX",
                9 => "FreeBSD",
                10 => "Tru64",
                11 => "Novell Modesto",
                12 => "OpenBSD",
                13 => "OpenVMS",
                14 => "NonStop Kernel",
                15 => "AROS",
                16 => "FenixOS",
                17 => "CloudABI",
                18 => "Stratus Technologies OpenVOS",
                _ => "OS/ABI Unknown",
            };
        }

        public static string FormatAddress(ulong address)
        {
            return $"0x{address:X}";
        }

        public static string FormatSize(ulong size)
        {
            return $"0x{size:X} ({size} bytes)";
        }

        public string GetReadableVersion()
        {
            return $"{_header.e_version}";
        }

        public string GetMachineDescription()
        {
            return _header.e_machine switch
            {
                (ushort)EMachine.EM_386 => "Intel 80386/80486",
                (ushort)EMachine.EM_X86_64 => "Advanced Micro Devices X86-64",
                (ushort)EMachine.EM_ARM => "ARM",
                (ushort)EMachine.EM_AARCH64 => "AArch64 (ARM 64-bit)",
                (ushort)EMachine.EM_MIPS => "MIPS",
                (ushort)EMachine.EM_PPC => "PowerPC",
                (ushort)EMachine.EM_PPC64 => "PowerPC 64-bit",
                (ushort)EMachine.EM_SPARC => "SPARC",
                (ushort)EMachine.EM_IA_64 => "Intel IA-64",
                (ushort)EMachine.EM_RISCV => "RISC-V",
                (ushort)EMachine.EM_LOONGARCH => "LoongArch",
                _ =>
                    GetArchitectureName(),
            };
        }

        public string GetFileTypeDescription()
        {
            return (ELFType)_header.e_type switch
            {
                ELFType.ET_NONE => "未指定类型",
                ELFType.ET_REL => "可重定位文件",
                ELFType.ET_EXEC => "可执行文件",
                ELFType.ET_DYN => "共享对象文件",
                ELFType.ET_CORE => "核心转储文件",
                _ => GetELFTypeName(),
            };
        }

        public string GetEntryPointAddress()
        {
            return FormatAddress(_header.e_entry);
        }

        public string GetHeaderSize()
        {
            return $"{_header.e_ehsize} (bytes)";
        }

        public static string GetSymbolType(byte stInfo)
        {
            byte type = (byte)(stInfo & 0x0F);
            if (Enum.IsDefined(typeof(SymbolType), type))
            {
                return Enum.GetName(typeof(SymbolType), type)?.Replace("STT_", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        public static string GetSymbolBinding(byte stInfo)
        {
            byte binding = (byte)(stInfo >> 4);
            if (Enum.IsDefined(typeof(SymbolBinding), binding))
            {
                return Enum.GetName(typeof(SymbolBinding), binding)?.Replace("STB_", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        public static string GetSymbolVisibility(byte stOther)
        {
            byte visibility = (byte)(stOther & 0x03);
            if (Enum.IsDefined(typeof(SymbolVisibility), visibility))
            {
                return Enum.GetName(typeof(SymbolVisibility), visibility)?.Replace("STV_", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        // 添加重定位类型名称获取方法
        public static string GetRelocationTypeName(uint type, ushort machine)
        {
            // 根据机器类型返回不同的重定位类型名称
            return machine switch
            {
                (ushort)EMachine.EM_X86_64 => GetX86_64RelocationTypeName(type),
                (ushort)EMachine.EM_386 => GetX86RelocationTypeName(type),
                (ushort)EMachine.EM_ARM => GetArmRelocationTypeName(type),
                (ushort)EMachine.EM_AARCH64 => GetAArch64RelocationTypeName(type),
                (ushort)EMachine.EM_MIPS => GetMipsRelocationTypeName(type),
                (ushort)EMachine.EM_MIPS_RS3_LE => GetMipsRelocationTypeName(type), // MIPS RS3000 Little-endian
                (ushort)EMachine.EM_LOONGARCH => GetLoongArchRelocationTypeName(type),
                
                // 新增其他架构支持
                (ushort)EMachine.EM_68K => GetM68kRelocationTypeName(type),
                (ushort)EMachine.EM_SPARC => GetSparcRelocationTypeName(type),
                (ushort)EMachine.EM_SPARCV9 => GetSparcRelocationTypeName(type), // SPARC V9 64-bit
                (ushort)EMachine.EM_PARISC => GetHPPARelocationTypeName(type),
                (ushort)EMachine.EM_ALPHA => GetAlphaRelocationTypeName(type),
                (ushort)EMachine.EM_PPC => GetPowerPCRelocationTypeName(type),
                (ushort)EMachine.EM_PPC64 => GetPowerPC64RelocationTypeName(type),
                (ushort)EMachine.EM_IA_64 => GetIA64RelocationTypeName(type),
                (ushort)EMachine.EM_S390 => GetS390RelocationTypeName(type),
                (ushort)EMachine.EM_CRIS => GetCRISRelocationTypeName(type),
                (ushort)EMachine.EM_MN10300 => GetAM33RelocationTypeName(type), // AM33
                (ushort)EMachine.EM_M32R => GetM32RRelocationTypeName(type),
                (ushort)EMachine.EM_MICROBLAZE => GetMicroBlazeRelocationTypeName(type),
                (ushort)EMachine.EM_ALTERA_NIOS2 => GetNios2RelocationTypeName(type),
                (ushort)EMachine.EM_TILEPRO => GetTILEProRelocationTypeName(type),
                (ushort)EMachine.EM_TILEGX => GetTILEGxRelocationTypeName(type),
                
                _ => $"R_UNKNOWN({type})",
            };
        }

        private static string GetX86_64RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(X86_64RelocationType), type))
            {
                return Enum.GetName(typeof(X86_64RelocationType), type) ?? $"R_X86_64_UNKNOWN({type})";
            }
            return $"R_X86_64_UNKNOWN({type})";
        }

        private static string GetX86RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(I386RelocationType), type))
            {
                return Enum.GetName(typeof(I386RelocationType), type) ?? $"R_386_UNKNOWN({type})";
            }
            return $"R_386_UNKNOWN({type})";
        }

        private static string GetArmRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(ARMRelocationType), type))
            {
                return Enum.GetName(typeof(ARMRelocationType), type) ?? $"R_ARM_UNKNOWN({type})";
            }
            return $"R_ARM_UNKNOWN({type})";
        }

        private static string GetAArch64RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(AArch64RelocationType), type))
            {
                return Enum.GetName(typeof(AArch64RelocationType), type) ?? $"R_AARCH64_UNKNOWN({type})";
            }
            return $"R_AARCH64_UNKNOWN({type})";
        }

        private static string GetMipsRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(MipsRelocationType), type))
            {
                return Enum.GetName(typeof(MipsRelocationType), type) ?? $"R_MIPS_UNKNOWN({type})";
            }
            return $"R_MIPS_UNKNOWN({type})";
        }

        private static string GetLoongArchRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(LoongArchRelocationType), type))
            {
                return Enum.GetName(typeof(LoongArchRelocationType), type) ?? $"R_LARCH_UNKNOWN({type})";
            }
            return $"R_LARCH_UNKNOWN({type})";
        }
        
        // 新增各种架构的重定位类型名称获取方法
        private static string GetM68kRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(M68kRelocationType), type))
            {
                return Enum.GetName(typeof(M68kRelocationType), type) ?? $"R_68K_UNKNOWN({type})";
            }
            return $"R_68K_UNKNOWN({type})";
        }
        
        private static string GetSparcRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(SPARCRelocationType), type))
            {
                return Enum.GetName(typeof(SPARCRelocationType), type) ?? $"R_SPARC_UNKNOWN({type})";
            }
            return $"R_SPARC_UNKNOWN({type})";
        }
        
        private static string GetHPPARelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(HPPARelocationType), type))
            {
                return Enum.GetName(typeof(HPPARelocationType), type) ?? $"R_PARISC_UNKNOWN({type})";
            }
            return $"R_PARISC_UNKNOWN({type})";
        }
        
        private static string GetAlphaRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(AlphaRelocationType), type))
            {
                return Enum.GetName(typeof(AlphaRelocationType), type) ?? $"R_ALPHA_UNKNOWN({type})";
            }
            return $"R_ALPHA_UNKNOWN({type})";
        }
        
        private static string GetPowerPCRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(PowerPCRelocationType), type))
            {
                return Enum.GetName(typeof(PowerPCRelocationType), type) ?? $"R_PPC_UNKNOWN({type})";
            }
            return $"R_PPC_UNKNOWN({type})";
        }
        
        private static string GetPowerPC64RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(PowerPC64RelocationType), type))
            {
                return Enum.GetName(typeof(PowerPC64RelocationType), type) ?? $"R_PPC64_UNKNOWN({type})";
            }
            return $"R_PPC64_UNKNOWN({type})";
        }
        
        private static string GetIA64RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(IA64RelocationType), type))
            {
                return Enum.GetName(typeof(IA64RelocationType), type) ?? $"R_IA64_UNKNOWN({type})";
            }
            return $"R_IA64_UNKNOWN({type})";
        }
        
        private static string GetS390RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(S390RelocationType), type))
            {
                return Enum.GetName(typeof(S390RelocationType), type) ?? $"R_390_UNKNOWN({type})";
            }
            return $"R_390_UNKNOWN({type})";
        }
        
        private static string GetCRISRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(CRISRelocationType), type))
            {
                return Enum.GetName(typeof(CRISRelocationType), type) ?? $"R_CRIS_UNKNOWN({type})";
            }
            return $"R_CRIS_UNKNOWN({type})";
        }
        
        private static string GetAM33RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(AM33RelocationType), type))
            {
                return Enum.GetName(typeof(AM33RelocationType), type) ?? $"R_MN10300_UNKNOWN({type})";
            }
            return $"R_MN10300_UNKNOWN({type})";
        }
        
        private static string GetM32RRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(M32RRelocationType), type))
            {
                return Enum.GetName(typeof(M32RRelocationType), type) ?? $"R_M32R_UNKNOWN({type})";
            }
            return $"R_M32R_UNKNOWN({type})";
        }
        
        private static string GetMicroBlazeRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(MicroBlazeRelocationType), type))
            {
                return Enum.GetName(typeof(MicroBlazeRelocationType), type) ?? $"R_MICROBLAZE_UNKNOWN({type})";
            }
            return $"R_MICROBLAZE_UNKNOWN({type})";
        }
        
        private static string GetNios2RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(Nios2RelocationType), type))
            {
                return Enum.GetName(typeof(Nios2RelocationType), type) ?? $"R_NIOS2_UNKNOWN({type})";
            }
            return $"R_NIOS2_UNKNOWN({type})";
        }
        
        private static string GetTILEProRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(TILEProRelocationType), type))
            {
                return Enum.GetName(typeof(TILEProRelocationType), type) ?? $"R_TILEPRO_UNKNOWN({type})";
            }
            return $"R_TILEPRO_UNKNOWN({type})";
        }
        
        private static string GetTILEGxRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(TILEGxRelocationType), type))
            {
                return Enum.GetName(typeof(TILEGxRelocationType), type) ?? $"R_TILEGX_UNKNOWN({type})";
            }
            return $"R_TILEGX_UNKNOWN({type})";
        }

        private static long ReadInt64LE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8);
            if (BitConverter.IsLittleEndian) return BitConverter.ToInt64(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        private static long ReadInt64BE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(8);
            if (!BitConverter.IsLittleEndian) return BitConverter.ToInt64(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        private static int ReadInt32LE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            if (BitConverter.IsLittleEndian) return BitConverter.ToInt32(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        private static int ReadInt32BE(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(4);
            if (!BitConverter.IsLittleEndian) return BitConverter.ToInt32(bytes, 0);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static string GetDynamicTagDescription(long dTag)
        {
            if (Enum.IsDefined(typeof(DynamicTag), dTag))
            {
                return Enum.GetName(typeof(DynamicTag), dTag)?.Replace("DT_", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        public static string GetDynamicFlagDescription(uint flags)
        {
            var descriptions = new List<string>();

            if ((flags & (uint)DynamicFlags.DF_ORIGIN) != 0) descriptions.Add("ORIGIN");
            if ((flags & (uint)DynamicFlags.DF_SYMBOLIC) != 0) descriptions.Add("SYMBOLIC");
            if ((flags & (uint)DynamicFlags.DF_TEXTREL) != 0) descriptions.Add("TEXTREL");
            if ((flags & (uint)DynamicFlags.DF_BIND_NOW) != 0) descriptions.Add("BIND_NOW");
            if ((flags & (uint)DynamicFlags.DF_STATIC_TLS) != 0) descriptions.Add("STATIC_TLS");

            return string.Join(", ", descriptions);
        }

        public static string GetDynamicFlag1Description(uint flags)
        {
            var descriptions = new List<string>();

            // DT_FLAGS_1 flags values
            if ((flags & 0x00000001) != 0) descriptions.Add("NOW");           // Set RTLD_NOW for this object
            if ((flags & 0x00000002) != 0) descriptions.Add("GLOBAL");        // Set RTLD_GLOBAL for this object
            if ((flags & 0x00000004) != 0) descriptions.Add("GROUP");         // Set RTLD_GROUP for this object
            if ((flags & 0x00000008) != 0) descriptions.Add("NODELETE");      // Set RTLD_NODELETE for this object
            if ((flags & 0x00000010) != 0) descriptions.Add("LOADFLTR");      // Immediate loading of filters
            if ((flags & 0x00000020) != 0) descriptions.Add("INITFIRST");     // Set RTLD_INITFIRST for this object
            if ((flags & 0x00000040) != 0) descriptions.Add("NOOPEN");        // Set RTLD_NOOPEN for this object
            if ((flags & 0x00000080) != 0) descriptions.Add("ORIGIN");        // $ORIGIN must be resolved
            if ((flags & 0x00000100) != 0) descriptions.Add("DIRECT");        // Direct binding enabled
            if ((flags & 0x00000200) != 0) descriptions.Add("TRANS");         // Object is a transition
            if ((flags & 0x00000400) != 0) descriptions.Add("INTERPOSE");     // Object is an interposer
            if ((flags & 0x00000800) != 0) descriptions.Add("NODEFLIB");      // Ignore default lib search path
            if ((flags & 0x00001000) != 0) descriptions.Add("NOKSYMS");       // Do not allow RTLD_NOLOAD
            if ((flags & 0x00002000) != 0) descriptions.Add("NOHDR");         // Object has no headers
            if ((flags & 0x00004000) != 0) descriptions.Add("EDITED");        // Object has been modified
            if ((flags & 0x00008000) != 0) descriptions.Add("NORELOC");       // Object has no relocations
            if ((flags & 0x00010000) != 0) descriptions.Add("SYMINTPOSE");    // Object has individual interposers
            if ((flags & 0x00020000) != 0) descriptions.Add("GLOBAUDIT");     // Global auditing required
            if ((flags & 0x00040000) != 0) descriptions.Add(" SINGLETON");    // Singleton symbols are used

            return string.Join(", ", descriptions);
        }

        public static string? GetProgramHeaderType(uint pType)
        {
            if (Enum.IsDefined(typeof(ProgramHeaderType), pType))
            {
                return Enum.GetName(typeof(ProgramHeaderType), pType)?.Replace("PT_", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        public static string GetProgramHeaderFlags(uint pFlags)
        {
            var descriptions = new List<string>();

            if ((pFlags & (uint)ProgramHeaderFlags.PF_R) != 0) descriptions.Add("R");
            if ((pFlags & (uint)ProgramHeaderFlags.PF_W) != 0) descriptions.Add("W");
            if ((pFlags & (uint)ProgramHeaderFlags.PF_X) != 0) descriptions.Add("E");

            return string.Join("", descriptions);
        }

        public static string GetSectionType(uint shType)
        {
            if (Enum.IsDefined(typeof(SectionType), shType))
            {
                return Enum.GetName(typeof(SectionType), shType)?.Replace("SHT_", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        public static string? GetSectionFlags(ulong shFlags, bool is64Bit)
        {
            var descriptions = new List<string>();

            if (is64Bit)
            {
                if ((shFlags & (ulong)SectionFlags.SHF_WRITE) != 0) descriptions.Add("W");
                if ((shFlags & (ulong)SectionFlags.SHF_ALLOC) != 0) descriptions.Add("A");
                if ((shFlags & (ulong)SectionFlags.SHF_EXECINSTR) != 0) descriptions.Add("X");
                if ((shFlags & (ulong)SectionFlags.SHF_MERGE) != 0) descriptions.Add("M");
                if ((shFlags & (ulong)SectionFlags.SHF_STRINGS) != 0) descriptions.Add("S");
                if ((shFlags & (ulong)SectionFlags.SHF_INFO_LINK) != 0) descriptions.Add("I");
                if ((shFlags & (ulong)SectionFlags.SHF_LINK_ORDER) != 0) descriptions.Add("L");
                if ((shFlags & (ulong)SectionFlags.SHF_OS_NONCONFORMING) != 0) descriptions.Add("O");
                if ((shFlags & (ulong)SectionFlags.SHF_GROUP) != 0) descriptions.Add("G");
                if ((shFlags & (ulong)SectionFlags.SHF_TLS) != 0) descriptions.Add("T");
                if ((shFlags & (ulong)SectionFlags.SHF_COMPRESSED) != 0) descriptions.Add("C");
            }
            else
            {
                uint flags32 = (uint)shFlags;
                if ((flags32 & (uint)SectionFlags.SHF_WRITE) != 0) descriptions.Add("W");
                if ((flags32 & (uint)SectionFlags.SHF_ALLOC) != 0) descriptions.Add("A");
                if ((flags32 & (uint)SectionFlags.SHF_EXECINSTR) != 0) descriptions.Add("X");
                if ((flags32 & (uint)SectionFlags.SHF_MERGE) != 0) descriptions.Add("M");
                if ((flags32 & (uint)SectionFlags.SHF_STRINGS) != 0) descriptions.Add("S");
                if ((flags32 & (uint)SectionFlags.SHF_INFO_LINK) != 0) descriptions.Add("I");
                if ((flags32 & (uint)SectionFlags.SHF_LINK_ORDER) != 0) descriptions.Add("L");
                if ((flags32 & (uint)SectionFlags.SHF_OS_NONCONFORMING) != 0) descriptions.Add("O");
                if ((flags32 & (uint)SectionFlags.SHF_GROUP) != 0) descriptions.Add("G");
                if ((flags32 & (uint)SectionFlags.SHF_TLS) != 0) descriptions.Add("T");
                if ((flags32 & (uint)SectionFlags.SHF_COMPRESSED) != 0) descriptions.Add("C");
            }

            return string.Join("", descriptions);
        }

        private static string ExtractStringFromBytes(byte[] data, int startOffset)
        {
            int endOffset = startOffset;
            while (endOffset < data.Length && data[endOffset] != 0)
            {
                endOffset++;
            }

            if (endOffset > startOffset)
            {
                return Encoding.UTF8.GetString(data, startOffset, endOffset - startOffset);
            }
            return string.Empty;
        }

        public string GetFormattedELFFlags()
        {
            var descriptions = new List<string>();
            uint flags = _header.e_flags;

            // 根据架构类型解析不同的标志
            switch (_header.e_machine)
            {
                case (ushort)EMachine.EM_MIPS:
                    // 解析MIPS架构的标志
                    if ((flags & 0x00000001) != 0) descriptions.Add("noreorder");
                    if ((flags & 0x00000002) != 0) descriptions.Add("pic");
                    if ((flags & 0x00000004) != 0) descriptions.Add("cpic");
                    
                    // MIPS ABI类型
                    if ((flags & 0x00001000) != 0) descriptions.Add("o32");
                    if ((flags & 0x00002000) != 0) descriptions.Add("o64");
                    if ((flags & 0x00004000) != 0) descriptions.Add("n32");
                    if ((flags & 0x00000010) != 0) descriptions.Add("nan2008");
                    if ((flags & 0x00000020) != 0) descriptions.Add("nan2001");
                    
                    // MIPS架构类型 - 检查EF_MIPS_ARCH标志 (高4位)
                    // 对于MIPS32和MIPS64架构，高4位表示架构版本
                    uint arch = flags & 0xF0000000;
                    switch (arch)
                    {
                        case 0x00000000: descriptions.Add("mips1"); break;
                        case 0x10000000: descriptions.Add("mips2"); break;
                        case 0x20000000: descriptions.Add("mips3"); break;
                        case 0x30000000: descriptions.Add("mips4"); break;
                        case 0x40000000: descriptions.Add("mips5"); break;
                        case 0x50000000: descriptions.Add("mips32"); break;
                        case 0x60000000: descriptions.Add("mips32r2"); break;  // MIPS32r2
                        case 0x70000000: descriptions.Add("mips32r2"); break;  // MIPS32r2 with o32 ABI
                        case 0x80000000: descriptions.Add("mips64"); break;
                        case 0x90000000: descriptions.Add("mips64r2"); break;
                    }
                    break;

                case (ushort)EMachine.EM_ARM:
                    // 解析ARM架构的标志
                    // 首先检查EABI版本 (高8位)
                    uint abiVersion = (flags & 0xFF000000);
                    if ((abiVersion & 0x05000000) != 0) descriptions.Add("Version5 EABI");
                    else if ((abiVersion & 0x04000000) != 0) descriptions.Add("Version4 EABI");
                    else if (abiVersion == 0) descriptions.Add("unknown");
                    
                    // 检查浮点ABI类型
                    if ((flags & 0x00000400) != 0) descriptions.Add("hard-float ABI");
                    else if ((flags & 0x00000200) != 0) descriptions.Add("soft-float ABI");
                    
                    // 检查其他ARM标志
                    if ((flags & 0x00000002) != 0) descriptions.Add("has entry point");
                    if ((flags & 0x00800000) != 0) descriptions.Add("BE8");
                    if ((flags & 0x00400000) != 0) descriptions.Add("interworking enabled");
                    break;

                case (ushort)EMachine.EM_PPC:
                case (ushort)EMachine.EM_PPC64:
                    // 解析PowerPC架构的标志
                    if ((flags & 0x00000001) != 0) descriptions.Add("ppc_elfv1_abi");
                    if ((flags & 0x00000002) != 0) descriptions.Add("ppc_elfv2_abi");
                    break;

                case (ushort)EMachine.EM_SPARC:
                case (ushort)EMachine.EM_SPARC32PLUS:
                case (ushort)EMachine.EM_SPARCV9:
                    // 解析SPARC架构的标志
                    if ((flags & 0x00000001) != 0) descriptions.Add("sparc_ext");
                    if ((flags & 0x00000002) != 0) descriptions.Add("sparc_32bit");
                    if ((flags & 0x00000004) != 0) descriptions.Add("sparc_64bit");
                    break;

                default:
                    // 对于其他架构，只显示十六进制值
                    break;
            }

            return string.Join(", ", descriptions);
        }

    }
}