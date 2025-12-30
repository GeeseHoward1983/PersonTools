using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MyTool.ELFAnalyzer.Models;

namespace MyTool.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        public string? GetArchitectureName()
        {
            if (Enum.IsDefined(typeof(EMachine), _header.e_machine))
            {
                return Enum.GetName(typeof(EMachine), _header.e_machine);
            }
            return "EM_UNKNOWN";
        }

        public string? GetELFClassName()
        {
            if (Enum.IsDefined(typeof(ELFClass), _header.EI_CLASS))
            {
                return Enum.GetName(typeof(ELFClass), _header.EI_CLASS);
            }
            return "ELFCLASS_UNKNOWN";
        }

        public string? GetELFDataName()
        {
            if (Enum.IsDefined(typeof(ELFData), _header.EI_DATA))
            {
                return Enum.GetName(typeof(ELFData), _header.EI_DATA);
            }
            return "ELFDATA_UNKNOWN";
        }

        public string? GetELFTypeName()
        {
            if (Enum.IsDefined(typeof(ELFType), _header.e_type))
            {
                return Enum.GetName(typeof(ELFType), _header.e_type);
            }
            return "ET_UNKNOWN";
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
                _ => GetArchitectureName(),
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

        public string? GetProgramHeaderInfo()
        {
            if (_is64Bit)
            {
                return $"Program Headers: {_header.e_phnum} entries, {FormatSize(_header.e_phentsize)} each";
            }
            else
            {
                return $"Program Headers: {_header.e_phnum} entries, {FormatSize(_header.e_phentsize)} each";
            }
        }

        public string? GetSectionHeaderInfo()
        {
            if (_is64Bit)
            {
                return $"Section Headers: {_header.e_shnum} entries, {FormatSize(_header.e_shentsize)} each";
            }
            else
            {
                return $"Section Headers: {_header.e_shnum} entries, {FormatSize(_header.e_phentsize)} each";
            }
        }
    }
}