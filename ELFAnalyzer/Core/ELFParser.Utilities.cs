using PersonalTools.ELFAnalyzer.Models;
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
                ELFType.ET_NONE => "No file type",
                ELFType.ET_REL => "Relocatable file",
                ELFType.ET_EXEC => "Executable file",
                ELFType.ET_DYN => "Shared object file",
                ELFType.ET_CORE => "Core file",
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

    }
}