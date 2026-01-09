using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    public static class ELFHeaderInfo
    {
        public static string GetArchitectureName(ELFHeader header)
        {
            if (Enum.IsDefined(typeof(EMachine), header.e_machine))
            {
                return Enum.GetName(typeof(EMachine), header.e_machine)?.Replace("EM_", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        public static string GetELFClassName(ELFHeader header)
        {
            if (Enum.IsDefined(typeof(ELFClass), header.EI_CLASS))
            {
                return Enum.GetName(typeof(ELFClass), header.EI_CLASS)?.Replace("CLASS", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        public static string GetELFDataName(ELFHeader header)
        {
            if (Enum.IsDefined(typeof(ELFData), header.EI_DATA))
            {
                return Enum.GetName(typeof(ELFData), header.EI_DATA)?.Replace("ELFDATA", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        public static string GetELFTypeName(ELFHeader header)
        {
            if (Enum.IsDefined(typeof(ELFType), header.e_type))
            {
                return Enum.GetName(typeof(ELFType), header.e_type)?.Replace("ET_", "") ?? "UNKNOWN";
            }
            return "UNKNOWN";
        }

        public static string GetOSABIName(ELFHeader header)
        {
            return header.EI_OSABI switch
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

        public static string GetReadableVersion(ELFHeader header)
        {
            return $"{header.e_version}";
        }

        public static string GetMachineDescription(ELFHeader header)
        {
            return header.e_machine switch
            {
                (ushort)EMachine.EM_NONE => "No machine",
                (ushort)EMachine.EM_M32 => "AT&T WE 32100",
                (ushort)EMachine.EM_SPARC => "SPARC",
                (ushort)EMachine.EM_386 => "Intel 80386",
                (ushort)EMachine.EM_68K => "Motorola 68000",
                (ushort)EMachine.EM_88K => "Motorola 88000",
                (ushort)EMachine.EM_860 => "Intel 80860",
                (ushort)EMachine.EM_MIPS => "MIPS I Architecture",
                (ushort)EMachine.EM_S370 => "IBM System/370 Processor",
                (ushort)EMachine.EM_MIPS_RS3_LE => "MIPS RS3000 Little-endian",
                (ushort)EMachine.EM_PARISC => "Hewlett-Packard PA-RISC",
                (ushort)EMachine.EM_VPP500 => "Fujitsu VPP500",
                (ushort)EMachine.EM_SPARC32PLUS => "Enhanced instruction set SPARC",
                (ushort)EMachine.EM_960 => "Intel 80960",
                (ushort)EMachine.EM_PPC => "PowerPC",
                (ushort)EMachine.EM_PPC64 => "64-bit PowerPC",
                (ushort)EMachine.EM_S390 => "IBM System/370 Processor",
                (ushort)EMachine.EM_V800 => "NEC V800",
                (ushort)EMachine.EM_FR20 => "Fujitsu FR20",
                (ushort)EMachine.EM_RH32 => "TRW RH-32",
                (ushort)EMachine.EM_RCE => "Motorola RCE",
                (ushort)EMachine.EM_ARM => "ARM 32-bit architecture",
                (ushort)EMachine.EM_ALPHA => "Digital Alpha",
                (ushort)EMachine.EM_SH => "Hitachi SH",
                (ushort)EMachine.EM_SPARCV9 => "SPARC Version 9",
                (ushort)EMachine.EM_TRICORE => "Siemens TriCore embedded processor",
                (ushort)EMachine.EM_ARC => "Argonaut RISC Core, Argonaut Technologies Inc.",
                (ushort)EMachine.EM_H8_300 => "Hitachi H8/300",
                (ushort)EMachine.EM_H8_300H => "Hitachi H8/300H",
                (ushort)EMachine.EM_H8S => "Hitachi H8S",
                (ushort)EMachine.EM_H8_500 => "Hitachi H8/500",
                (ushort)EMachine.EM_IA_64 => "Intel IA-64 processor architecture",
                (ushort)EMachine.EM_MIPS_X => "Stanford MIPS-X",
                (ushort)EMachine.EM_COLDFIRE => "Motorola ColdFire",
                (ushort)EMachine.EM_68HC12 => "Motorola M68HC12",
                (ushort)EMachine.EM_MMA => "Fujitsu MMA Multimedia Accelerator",
                (ushort)EMachine.EM_PCP => "Siemens PCP",
                (ushort)EMachine.EM_NCPU => "Sony nCPU embedded RISC processor",
                (ushort)EMachine.EM_NDR1 => "Denso NDR1 microprocessor",
                (ushort)EMachine.EM_STARCORE => "Motorola Star*Core processor",
                (ushort)EMachine.EM_ME16 => "Toyota ME16 processor",
                (ushort)EMachine.EM_ST100 => "STMicroelectronics ST100 processor",
                (ushort)EMachine.EM_TINYJ => "Advanced Logic Corp. TinyJ embedded processor family",
                (ushort)EMachine.EM_X86_64 => "AMD x86-64 architecture",
                (ushort)EMachine.EM_PDSP => "Sony DSP Processor",
                (ushort)EMachine.EM_PDP10 => "Digital Equipment Corp. PDP-10",
                (ushort)EMachine.EM_PDP11 => "Digital Equipment Corp. PDP-11",
                (ushort)EMachine.EM_FX66 => "Siemens FX66 microcontroller",
                (ushort)EMachine.EM_ST9PLUS => "STMicroelectronics ST9+ 8/16 bit microcontroller",
                (ushort)EMachine.EM_ST7 => "STMicroelectronics ST7 8-bit microcontroller",
                (ushort)EMachine.EM_68HC16 => "Motorola MC68HC16 Microcontroller",
                (ushort)EMachine.EM_68HC11 => "Motorola MC68HC11 Microcontroller",
                (ushort)EMachine.EM_68HC08 => "Motorola MC68HC08 Microcontroller",
                (ushort)EMachine.EM_68HC05 => "Motorola MC68HC05 Microcontroller",
                (ushort)EMachine.EM_SVX => "Silicon Graphics SVx",
                (ushort)EMachine.EM_ST19 => "STMicroelectronics ST19 8-bit microcontroller",
                (ushort)EMachine.EM_VAX => "Digital VAX",
                (ushort)EMachine.EM_CRIS => "Axis Communications 32-bit embedded processor",
                (ushort)EMachine.EM_JAVELIN => "Infineon Technologies 32-bit embedded processor",
                (ushort)EMachine.EM_FIREPATH => "Element 14 64-bit DSP Processor",
                (ushort)EMachine.EM_ZSP => "LSI Logic 16-bit DSP Processor",
                (ushort)EMachine.EM_MMIX => "Donald Knuth's educational 64-bit processor",
                (ushort)EMachine.EM_HUANY => "Harvard University machine-independent object files",
                (ushort)EMachine.EM_PRISM => "SiTera Prism",
                (ushort)EMachine.EM_AVR => "Atmel AVR 8-bit microcontroller",
                (ushort)EMachine.EM_FR30 => "Fujitsu FR30",
                (ushort)EMachine.EM_D10V => "Mitsubishi D10V",
                (ushort)EMachine.EM_D30V => "Mitsubishi D30V",
                (ushort)EMachine.EM_V850 => "NEC v850",
                (ushort)EMachine.EM_M32R => "Mitsubishi M32R",
                (ushort)EMachine.EM_MN10300 => "Matsushita MN10300",
                (ushort)EMachine.EM_MN10200 => "Matsushita MN10200",
                (ushort)EMachine.EM_PJ => "picoJava",
                (ushort)EMachine.EM_OPENRISC => "OpenRISC 32-bit embedded processor",
                (ushort)EMachine.EM_ARC_COMPACT => "ARC International ARCompact processor",
                (ushort)EMachine.EM_XTENSA => "Tensilica Xtensa Architecture",
                (ushort)EMachine.EM_VIDEOCORE => "Alphamosaic VideoCore processor",
                (ushort)EMachine.EM_TMM_GPP => "Thompson Multimedia General Purpose Processor",
                (ushort)EMachine.EM_NS32K => "National Semiconductor 32000 series",
                (ushort)EMachine.EM_TPC => "Tenor Network TPC processor",
                (ushort)EMachine.EM_SNP1K => "Trebia SNP 1000 processor",
                (ushort)EMachine.EM_ST200 => "STMicroelectronics ST200 microcontroller",
                (ushort)EMachine.EM_IP2K => "Ubicom IP2xxx microcontroller family",
                (ushort)EMachine.EM_MAX => "MAX processor",
                (ushort)EMachine.EM_CR => "National Semiconductor CompactRISC microprocessor",
                (ushort)EMachine.EM_F2MC16 => "Fujitsu F2MC16",
                (ushort)EMachine.EM_MSP430 => "Texas Instruments embedded microcontroller msp430",
                (ushort)EMachine.EM_BLACKFIN => "Analog Devices Blackfin (DSP) processor",
                (ushort)EMachine.EM_SE_C33 => "S1C33 Family of Seiko Epson processors",
                (ushort)EMachine.EM_SEP => "Sharp embedded microprocessor",
                (ushort)EMachine.EM_ARCA => "Arca RISC Microprocessor",
                (ushort)EMachine.EM_UNICORE => "Microprocessor series from PKU-Unity Ltd. and MPRC of Peking University",
                (ushort)EMachine.EM_EXCESS => "eXcess: 16/32/64-bit configurable embedded CPU",
                (ushort)EMachine.EM_DXP => "Icera Semiconductor Inc. Deep Execution Processor",
                (ushort)EMachine.EM_ALTERA_NIOS2 => "Altera Nios II soft-core processor",
                (ushort)EMachine.EM_CRX => "National Semiconductor CompactRISC CRX microprocessor",
                (ushort)EMachine.EM_XGATE => "Motorola XGATE embedded processor",
                (ushort)EMachine.EM_C166 => "Infineon C16x/XC16x processor",
                (ushort)EMachine.EM_M16C => "Renesas M16C series microprocessors",
                (ushort)EMachine.EM_DSPIC30F => "Microchip Technology dsPIC30F Digital Signal Controller",
                (ushort)EMachine.EM_CE => "Freescale Communication Engine RISC core",
                (ushort)EMachine.EM_M32C => "Renesas M32C series microprocessors",
                (ushort)EMachine.EM_TSK3000 => "Altium TSK3000 core",
                (ushort)EMachine.EM_RS08 => "Freescale RS08 embedded processor",
                (ushort)EMachine.EM_SHARC => "Analog Devices SHARC family of 32-bit DSP processors",
                (ushort)EMachine.EM_ECOG2 => "Cyan Technology eCOG2 microprocessor",
                (ushort)EMachine.EM_SCORE7 => "Sunplus S+core7 RISC processor",
                (ushort)EMachine.EM_DSP24 => "New Japan Radio (NJR) 24-bit DSP Processor",
                (ushort)EMachine.EM_VIDEOCORE3 => "Broadcom VideoCore III processor",
                (ushort)EMachine.EM_LATTICEMICO32 => "RISC processor for Lattice FPGA architecture",
                (ushort)EMachine.EM_SE_C17 => "Seiko Epson C17 family",
                (ushort)EMachine.EM_TI_C6000 => "The Texas Instruments TMS320C6000 DSP family",
                (ushort)EMachine.EM_TI_C2000 => "The Texas Instruments TMS320C2000 DSP family",
                (ushort)EMachine.EM_TI_C5500 => "The Texas Instruments TMS320C55x DSP family",
                (ushort)EMachine.EM_TI_ARP32 => "Texas Instruments Application Specific RISC Processor, 32bit fetch",
                (ushort)EMachine.EM_TI_PRU => "Texas Instruments Programmable Realtime Unit",
                (ushort)EMachine.EM_MMDSP_PLUS => "STMicroelectronics 64bit VLIW Data Signal Processor",
                (ushort)EMachine.EM_CYPRESS_M8C => "Cypress M8C microprocessor",
                (ushort)EMachine.EM_R32C => "Renesas R32C series microprocessors",
                (ushort)EMachine.EM_TRIMEDIA => "NXP Semiconductors TriMedia architecture family",
                (ushort)EMachine.EM_QDSP6 => "QUALCOMM DSP6 Processor",
                (ushort)EMachine.EM_8051 => "Intel 8051 and variants",
                (ushort)EMachine.EM_STXP7X => "STMicroelectronics STxP7x family of configurable and extensible RISC processors",
                (ushort)EMachine.EM_NDS32 => "Andes Technology compact code size embedded RISC processor family",
                (ushort)EMachine.EM_ECOG1X => "Cyan Technology eCOG1X family",
                (ushort)EMachine.EM_MAXQ30 => "Dallas Semiconductor MAXQ30 Core Micro-controllers",
                (ushort)EMachine.EM_XIMO16 => "New Japan Radio (NJR) 16-bit DSP Processor",
                (ushort)EMachine.EM_MANIK => "M2000 Reconfigurable RISC Microprocessor",
                (ushort)EMachine.EM_CRAYNV2 => "Cray Inc. NV2 vector architecture",
                (ushort)EMachine.EM_RX => "Renesas RX family",
                (ushort)EMachine.EM_METAG => "Imagination Technologies META processor architecture",
                (ushort)EMachine.EM_MCST_ELBRUS => "MCST Elbrus general purpose hardware architecture",
                (ushort)EMachine.EM_ECOG16 => "Cyan Technology eCOG16 family",
                (ushort)EMachine.EM_CR16 => "National Semiconductor CompactRISC CR16 16-bit microprocessor",
                (ushort)EMachine.EM_ETPU => "Freescale Extended Time Processing Unit",
                (ushort)EMachine.EM_SLE9X => "Infineon Technologies SLE9X core",
                (ushort)EMachine.EM_L10M => "Intel L10M",
                (ushort)EMachine.EM_K10M => "Intel K10M",
                (ushort)EMachine.EM_AARCH64 => "ARM 64-bit architecture (AARCH64)",
                (ushort)EMachine.EM_AVR32 => "Atmel Corporation 32-bit microprocessor family",
                (ushort)EMachine.EM_STM8 => "STMicroeletronics STM8 8-bit microcontroller",
                (ushort)EMachine.EM_TILE64 => "Tilera TILE64 multicore architecture family",
                (ushort)EMachine.EM_TILEPRO => "Tilera TILEPro multicore architecture family",
                (ushort)EMachine.EM_MICROBLAZE => "Xilinx MicroBlaze 32-bit RISC soft processor core",
                (ushort)EMachine.EM_CUDA => "NVIDIA CUDA architecture",
                (ushort)EMachine.EM_TILEGX => "Tilera TILE-Gx multicore architecture family",
                (ushort)EMachine.EM_CLOUDSHIELD => "CloudShield architecture family",
                (ushort)EMachine.EM_COREA_1ST => "KIPO-KAIST Core-A 1st generation processor family",
                (ushort)EMachine.EM_COREA_2ND => "KIPO-KAIST Core-A 2nd generation processor family",
                (ushort)EMachine.EM_ARC_COMPACT2 => "Synopsys ARCompact V2",
                (ushort)EMachine.EM_OPEN8 => "Open8 8-bit RISC soft processor core",
                (ushort)EMachine.EM_RL78 => "Renesas RL78 family",
                (ushort)EMachine.EM_VIDEOCORE5 => "Broadcom VideoCore V processor",
                (ushort)EMachine.EM_78KOR => "Renesas 78KOR family",
                (ushort)EMachine.EM_56800EF => "Freescale 56800EF Digital Signal Controller (with embedded Flash)",
                (ushort)EMachine.EM_BA1 => "Beyond BA1 CPU architecture",
                (ushort)EMachine.EM_BA2 => "Beyond BA2 CPU architecture",
                (ushort)EMachine.EM_XCORE => "XMOS xCORE processor family",
                (ushort)EMachine.EM_MCHP_PIC => "Microchip 8-bit PIC(r) family",
                (ushort)EMachine.EM_INTEL205 => "Reserved by Intel",
                (ushort)EMachine.EM_INTEL206 => "Reserved by Intel",
                (ushort)EMachine.EM_INTEL207 => "Reserved by Intel",
                (ushort)EMachine.EM_INTEL208 => "Reserved by Intel",
                (ushort)EMachine.EM_INTEL209 => "Reserved by Intel",
                (ushort)EMachine.EM_KM32 => "KM211 KM32 32-bit processor",
                (ushort)EMachine.EM_KMX32 => "KM211 KMX32 32-bit processor",
                (ushort)EMachine.EM_KMX16 => "KM211 KMX16 16-bit processor",
                (ushort)EMachine.EM_KMX8 => "KM211 KMX8 8-bit processor",
                (ushort)EMachine.EM_KVARC => "KM211 KVARC processor",
                (ushort)EMachine.EM_CDP => "Paneve CDP architecture family",
                (ushort)EMachine.EM_COGE => "Cognitive Smart Memory Processor",
                (ushort)EMachine.EM_COOL => "Bluechip CoolEngine",
                (ushort)EMachine.EM_NORC => "Nanoradio Optimized RISC",
                (ushort)EMachine.EM_CSR_KALIMBA => "CSR Kalimba architecture family",
                (ushort)EMachine.EM_Z80 => "Zilog Z80",
                (ushort)EMachine.EM_VISIUM => "Controls and Data Services VISIUMcore processor",
                (ushort)EMachine.EM_FT32 => "FTDI Chip FT32 high performance 32-bit RISC architecture",
                (ushort)EMachine.EM_MOXIE => "Moxie processor family",
                (ushort)EMachine.EM_AMDGPU => "AMD GPU architecture",
                (ushort)EMachine.EM_RISCV => "RISC-V",
                (ushort)EMachine.EM_LANAI => "Lanai processor",
                (ushort)EMachine.EM_CEVA => "CEVA Processor Architecture Family",
                (ushort)EMachine.EM_CEVA_X2 => "CEVA X2 Processor Family",
                (ushort)EMachine.EM_BPF => "Linux BPF - in-kernel virtual machine",
                (ushort)EMachine.EM_GRAPHCORE_GCN => "Graphcore GCN architecture",
                (ushort)EMachine.EM_RISCV32 => "RISC-V 32-bit",
                (ushort)EMachine.EM_RISCV64 => "RISC-V 64-bit",
                (ushort)EMachine.EM_LOONGARCH => "LoongArch",
                (ushort)EMachine.EM_COGEY => "Codeplay Software Ltd. COGEY",
                (ushort)EMachine.EM_COFFEE => "Codeplay Software Ltd. COFFEE",
                (ushort)EMachine.EM_CISCO_IOS => "Cisco IOS",
                (ushort)EMachine.EM_CISCO_IOS64 => "Cisco IOS 64-bit",
                (ushort)EMachine.EM_HELIOX => "Cohrence's Heliox",
                _ => GetArchitectureName(header),
            };
        }

        public static string GetFileTypeDescription(ELFHeader header)
        {
            return (ELFType)header.e_type switch
            {
                ELFType.ET_NONE => "未指定类型",
                ELFType.ET_REL => "可重定位文件",
                ELFType.ET_EXEC => "可执行文件",
                ELFType.ET_DYN => "共享对象文件",
                ELFType.ET_CORE => "核心转储文件",
                _ => GetELFTypeName(header),
            };
        }

        public static string GetEntryPointAddress(ELFHeader header)
        {
            return ELFParserUtils.FormatAddress(header.e_entry);
        }

        public static string GetHeaderSize(ELFHeader header)
        {
            return $"{header.e_ehsize} (bytes)";
        }

        public static string GetFormattedELFFlags(ELFHeader header)
        {
            var descriptions = new List<string>();
            uint flags = header.e_flags;

            // 根据架构类型解析不同的标志
            switch (header.e_machine)
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