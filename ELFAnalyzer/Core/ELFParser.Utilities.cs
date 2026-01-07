using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        public string? GetArchitectureName()
        {
            if (Enum.IsDefined(typeof(EMachine), _header.e_machine))
            {
                return Enum.GetName(typeof(EMachine), _header.e_machine)?.Replace("EM_", "");
            }
            return "UNKNOWN";
        }

        public string? GetELFClassName()
        {
            if (Enum.IsDefined(typeof(ELFClass), _header.EI_CLASS))
            {
                return Enum.GetName(typeof(ELFClass), _header.EI_CLASS)?.Replace("CLASS", "");
            }
            return "UNKNOWN";
        }

        public string? GetELFDataName()
        {
            if (Enum.IsDefined(typeof(ELFData), _header.EI_DATA))
            {
                return Enum.GetName(typeof(ELFData), _header.EI_DATA)?.Replace("ELFDATA", "");
            }
            return "UNKNOWN";
        }

        public string? GetELFTypeName()
        {
            if (Enum.IsDefined(typeof(ELFType), _header.e_type))
            {
                return Enum.GetName(typeof(ELFType), _header.e_type)?.Replace("ET_", "");
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

        public string? GetReadableVersion()
        {
            return $"{_header.e_version}";
        }

        public string? GetMachineDescription()
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

        public string? GetFileTypeDescription()
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

        public string? GetEntryPointAddress()
        {
            return FormatAddress(_header.e_entry);
        }

        public string? GetHeaderSize()
        {
            return $"{_header.e_ehsize} (bytes)";
        }

        public static string? GetSymbolType(byte stInfo)
        {
            byte type = (byte)(stInfo & 0x0F);
            if (Enum.IsDefined(typeof(SymbolType), type))
            {
                return Enum.GetName(typeof(SymbolType), type)?.Replace("STT_", "");
            }
            return "UNKNOWN";
        }

        public static string? GetSymbolBinding(byte stInfo)
        {
            byte binding = (byte)(stInfo >> 4);
            if (Enum.IsDefined(typeof(SymbolBinding), binding))
            {
                return Enum.GetName(typeof(SymbolBinding), binding)?.Replace("STB_", "");
            }
            return "UNKNOWN";
        }

        public static string? GetSymbolVisibility(byte stOther)
        {
            byte visibility = (byte)(stOther & 0x03);
            if (Enum.IsDefined(typeof(SymbolVisibility), visibility))
            {
                return Enum.GetName(typeof(SymbolVisibility), visibility)?.Replace("STV_", "");
            }
            return "UNKNOWN";
        }

        // 添加重定位类型名称获取方法
        public static string? GetRelocationTypeName(uint type, ushort machine)
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
                _ => $"R_UNKNOWN({type})",
            };
        }

        private static string? GetX86_64RelocationTypeName(uint type)
        {
            return type switch
            {
                1 => "R_X86_64_64",
                2 => "R_X86_64_PC32",
                3 => "R_X86_64_GOT32",
                4 => "R_X86_64_PLT32",
                5 => "R_X86_64_COPY",
                6 => "R_X86_64_GLOB_DAT",
                7 => "R_X86_64_JUMP_SLOT",
                8 => "R_X86_64_RELATIVE",
                9 => "R_X86_64_GOTPCREL",
                10 => "R_X86_64_32",
                11 => "R_X86_64_32S",
                12 => "R_X86_64_16",
                13 => "R_X86_64_PC16",
                14 => "R_X86_64_8",
                15 => "R_X86_64_PC8",
                16 => "R_X86_64_DTPMOD64",
                17 => "R_X86_64_DTPOFF64",
                18 => "R_X86_64_TPOFF64",
                19 => "R_X86_64_TLSGD",
                20 => "R_X86_64_TLSLD",
                21 => "R_X86_64_DTPOFF32",
                22 => "R_X86_64_GOTTPOFF",
                23 => "R_X86_64_TPOFF32",
                24 => "R_X86_64_PC64",
                25 => "R_X86_64_GOTOFF64",
                26 => "R_X86_64_GOTPC32",
                27 => "R_X86_64_GOT64",
                28 => "R_X86_64_GOTPCREL64",
                29 => "R_X86_64_GOTPC64",
                30 => "R_X86_64_GOTPLT64",
                31 => "R_X86_64_PLTOFF64",
                32 => "R_X86_64_SIZE32",
                33 => "R_X86_64_SIZE64",
                34 => "R_X86_64_GOTPC32_TLSDESC",
                35 => "R_X86_64_TLSDESC_CALL",
                36 => "R_X86_64_TLSDESC",
                37 => "R_X86_64_IRELATIVE",
                38 => "R_X86_64_RELATIVE64",
                _ => $"R_X86_64_UNKNOWN({type})"
            };
        }

        private static string? GetX86RelocationTypeName(uint type)
        {
            return type switch
            {
                1 => "R_386_32",
                2 => "R_386_PC32",
                3 => "R_386_GOT32",
                4 => "R_386_PLT32",
                5 => "R_386_COPY",
                6 => "R_386_GLOB_DAT",
                7 => "R_386_JUMP_SLOT",
                8 => "R_386_RELATIVE",
                9 => "R_386_GOTOFF",
                10 => "R_386_GOTPC",
                11 => "R_386_32PLT",
                12 => "R_386_TLS_TPOFF",
                13 => "R_386_TLS_IE",
                14 => "R_386_TLS_GOTIE",
                15 => "R_386_TLS_LE",
                16 => "R_386_TLS_GD",
                17 => "R_386_TLS_LDM",
                18 => "R_386_16",
                19 => "R_386_PC16",
                20 => "R_386_8",
                21 => "R_386_PC8",
                22 => "R_386_TLS_GD_32",
                23 => "R_386_TLS_GD_PUSH",
                24 => "R_386_TLS_GD_CALL",
                25 => "R_386_TLS_GD_POP",
                26 => "R_386_TLS_LDM_32",
                27 => "R_386_TLS_LDM_PUSH",
                28 => "R_386_TLS_LDM_CALL",
                29 => "R_386_TLS_LDM_POP",
                30 => "R_386_TLS_LDO_32",
                31 => "R_386_TLS_IE_32",
                32 => "R_386_TLS_LE_32",
                33 => "R_386_TLS_DTPMOD32",
                34 => "R_386_TLS_DTPOFF32",
                35 => "R_386_TLS_TPOFF32",
                36 => "R_386_SIZE32",
                37 => "R_386_TLS_GOTDESC",
                38 => "R_386_TLS_DESC_CALL",
                39 => "R_386_TLS_DESC",
                40 => "R_386_IRELATIVE",
                41 => "R_386_GOT32X",
                _ => $"R_386_UNKNOWN({type})"
            };
        }

        private static string? GetArmRelocationTypeName(uint type)
        {
            return type switch
            {
                2 => "R_ARM_ABS32",
                3 => "R_ARM_REL32",
                5 => "R_ARM_THM_CALL",
                7 => "R_ARM_JUMP24",
                8 => "R_ARM_THM_JUMP24",
                9 => "R_ARM_TARGET1",
                10 => "R_ARM_SBREL32",
                11 => "R_ARM_THM_SWI8",
                12 => "R_ARM_XPC25",
                13 => "R_ARM_THM_XPC25",
                16 => "R_ARM_SWI24",
                17 => "R_ARM_THM_SWI12",
                18 => "R_ARM_THM_SWI16",
                21 => "R_ARM_CALL",
                22 => "R_ARM_JUMP11",
                23 => "R_ARM_THM_JUMP11",
                24 => "R_ARM_TARGET2",
                25 => "R_ARM_PREL31",
                26 => "R_ARM_MOVW_ABS_NC",
                27 => "R_ARM_MOVT_ABS",
                28 => "R_ARM_MOVW_PREL_NC",
                29 => "R_ARM_MOVT_PREL",
                30 => "R_ARM_THM_MOVW_ABS_NC",
                31 => "R_ARM_THM_MOVT_ABS",
                32 => "R_ARM_THM_MOVW_PREL_NC",
                33 => "R_ARM_THM_MOVT_PREL",
                34 => "R_ARM_THM_JUMP6",
                35 => "R_ARM_THM_ALU_PREL_11_0",
                36 => "R_ARM_THM_PC12",
                37 => "R_ARM_ABS32_NOI",
                38 => "R_ARM_REL32_NOI",
                39 => "R_ARM_ALU_PC_G0_NC",
                40 => "R_ARM_ALU_PC_G0",
                41 => "R_ARM_ALU_PC_G1_NC",
                42 => "R_ARM_ALU_PC_G1",
                43 => "R_ARM_ALU_PC_G2",
                44 => "R_ARM_LDR_PC_G1",
                45 => "R_ARM_LDR_PC_G2",
                46 => "R_ARM_LDRS_PC_G0",
                47 => "R_ARM_LDRS_PC_G1",
                48 => "R_ARM_LDRS_PC_G2",
                49 => "R_ARM_LDC_PC_G0",
                50 => "R_ARM_LDC_PC_G1",
                51 => "R_ARM_LDC_PC_G2",
                52 => "R_ARM_ALU_SB_G0_NC",
                53 => "R_ARM_ALU_SB_G0",
                54 => "R_ARM_ALU_SB_G1_NC",
                55 => "R_ARM_ALU_SB_G1",
                56 => "R_ARM_ALU_SB_G2",
                57 => "R_ARM_LDR_SB_G0",
                58 => "R_ARM_LDR_SB_G1",
                59 => "R_ARM_LDR_SB_G2",
                60 => "R_ARM_LDRS_SB_G0",
                61 => "R_ARM_LDRS_SB_G1",
                62 => "R_ARM_LDRS_SB_G2",
                63 => "R_ARM_LDC_SB_G0",
                64 => "R_ARM_LDC_SB_G1",
                65 => "R_ARM_LDC_SB_G2",
                66 => "R_ARM_MOVW_BREL_NC",
                67 => "R_ARM_MOVT_BREL",
                68 => "R_ARM_MOVW_BREL",
                69 => "R_ARM_THM_MOVW_BREL_NC",
                70 => "R_ARM_THM_MOVT_BREL",
                71 => "R_ARM_THM_MOVW_BREL",
                72 => "R_ARM_TLS_GOTDESC",
                73 => "R_ARM_TLS_CALL",
                74 => "R_ARM_TLS_DESCSEQ",
                75 => "R_ARM_THM_TLS_CALL",
                76 => "R_ARM_PLT32_ABS",
                77 => "R_ARM_GOT_ABS",
                78 => "R_ARM_GOT_PREL",
                79 => "R_ARM_GOT_BREL12",
                80 => "R_ARM_GOTOFF12",
                81 => "R_ARM_GOTRELAX",
                82 => "R_ARM_GNU_VTENTRY",
                83 => "R_ARM_GNU_VTINHERIT",
                84 => "R_ARM_THM_JUMP11",
                85 => "R_ARM_THM_JUMP8",
                86 => "R_ARM_TLS_GD32",
                87 => "R_ARM_TLS_LDM32",
                88 => "R_ARM_TLS_LDO32",
                89 => "R_ARM_TLS_IE32",
                90 => "R_ARM_TLS_LE32",
                91 => "R_ARM_TLS_LDO12",
                92 => "R_ARM_TLS_LE12",
                93 => "R_ARM_TLS_IE12GP",
                94 => "R_ARM_ME_TOO",
                95 => "R_ARM_THM_TLS_DESCSEQ16",
                96 => "R_ARM_THM_TLS_DESCSEQ32",
                97 => "R_ARM_THM_GOT_BREL12",
                98 => "R_ARM_THM_ALU_ABS_G0_NC",
                99 => "R_ARM_THM_ALU_ABS_G1_NC",
                100 => "R_ARM_THM_ALU_ABS_G2_NC",
                101 => "R_ARM_THM_ALU_ABS_G3",
                102 => "R_ARM_IRELATIVE",
                103 => "R_ARM_RXPC25",
                104 => "R_ARM_RSBREL32",
                105 => "R_ARM_THM_RPC22",
                106 => "R_ARM_RREL32",
                107 => "R_ARM_RABS32",
                108 => "R_ARM_RPC24",
                109 => "R_ARM_RBASE",
                _ => $"R_ARM_UNKNOWN({type})"
            };
        }

        private static string? GetAArch64RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(AArch64RelocationType), type))
            {
                string? ret = Enum.GetName(typeof(AArch64RelocationType), type);
                return ret ?? $"R_AARCH64_UNKNOWN({type})";
            }
            return $"R_AARCH64_UNKNOWN({type})";
        }

        private static string? GetMipsRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(MipsRelocationType), type))
            {
                string? ret = Enum.GetName(typeof(MipsRelocationType), type);
                return ret ?? $"R_MIPS_UNKNOWN({type})";
            }
            return $"R_MIPS_UNKNOWN({type})";
        }

        private static string? GetLoongArchRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(LoongArchRelocationType), type))
            {
                string? ret = Enum.GetName(typeof(LoongArchRelocationType), type);
                return ret ?? $"R_LARCH_UNKNOWN({type})";
            }
            return $"R_LARCH_UNKNOWN({type})";
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
                string? ret = Enum.GetName(typeof(DynamicTag), dTag)?.Replace("DT_", "");
                return ret ?? "UNKNOWN";
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
                return Enum.GetName(typeof(ProgramHeaderType), pType)?.Replace("PT_", "");
            }
            return "UNKNOWN";
        }

        public static string? GetProgramHeaderFlags(uint pFlags)
        {
            var descriptions = new List<string>();

            if ((pFlags & (uint)ProgramHeaderFlags.PF_R) != 0) descriptions.Add("R");
            if ((pFlags & (uint)ProgramHeaderFlags.PF_W) != 0) descriptions.Add("W");
            if ((pFlags & (uint)ProgramHeaderFlags.PF_X) != 0) descriptions.Add("E");

            return string.Join("", descriptions);
        }

        public static string? GetSectionType(uint shType)
        {
            if (Enum.IsDefined(typeof(SectionType), shType))
            {
                return Enum.GetName(typeof(SectionType), shType)?.Replace("SHT_", "");
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

        private static string? ExtractStringFromBytes(byte[] data, int startOffset)
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

        public string? GetFormattedELFFlags()
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