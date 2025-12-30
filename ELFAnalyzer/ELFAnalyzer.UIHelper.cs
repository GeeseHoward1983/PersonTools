using System;
using System.Collections.Generic;
using System.Text;
using MyTool.ELFAnalyzer.Core;
using MyTool.ELFAnalyzer.Models;

namespace MyTool.ELFAnalyzer
{
    public partial class ELFAnalyzer
    {
        public string GetFormattedELFHeaderInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("ELF Header:");
            sb.AppendLine("================================================================================");
            sb.AppendLine($"  Magic:   {GetMagicString()}");
            sb.AppendLine($"  Class:   {_parser.GetELFClassName()} ({(_parser.Header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? "64-bit" : "32-bit")})");
            sb.AppendLine($"  Data:    {_parser.GetELFDataName()} ({(_parser.Header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? "2's complement, little endian" : "2's complement, big endian")})");
            sb.AppendLine($"  Version: {_parser.GetReadableVersion()} ({_parser.Header.EI_VERSION})");
            sb.AppendLine($"  OS/ABI:  {_parser.GetOSABIName()} ({_parser.Header.EI_OSABI})");
            sb.AppendLine($"  ABI Ver: {_parser.Header.EI_ABIVERSION}");
            sb.AppendLine($"  Type:    {_parser.GetELFTypeName()} ({_parser.GetFileTypeDescription()})");
            sb.AppendLine($"  Machine: {_parser.GetArchitectureName()} ({_parser.GetMachineDescription()})");
            sb.AppendLine($"  Version: 0x{_parser.Header.e_version:X}");
            sb.AppendLine($"  Entry point address: {_parser.GetEntryPointAddress()}");
            sb.AppendLine($"  Start of program headers: {(long)_parser.Header.e_phoff} (bytes into file)");
            sb.AppendLine($"  Start of section headers: {(long)_parser.Header.e_shoff} (bytes into file)");
            sb.AppendLine($"  Flags: 0x{_parser.Header.e_flags:X}  {(IsPIC() ? "Position Independent Code" : "")}");
            sb.AppendLine($"  Size of this header: {_parser.GetHeaderSize()}");
            sb.AppendLine($"  Size of program headers: {_parser.Header.e_phentsize} (bytes)");
            sb.AppendLine($"  Number of program headers: {_parser.Header.e_phnum}");
            sb.AppendLine($"  Size of section headers: {_parser.Header.e_shentsize} (bytes)");
            sb.AppendLine($"  Number of section headers: {_parser.Header.e_shnum}");
            sb.AppendLine($"  Section header string table index: {_parser.Header.e_shstrndx}");
            return sb.ToString();
        }

        private bool IsPIC()
        {
            // Check if it's a shared object file with no relocations that would prevent PIC
            return _parser.Header.e_type == (ushort)ELFType.ET_DYN;
        }

        public string GetFormattedProgramHeadersInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Program Headers:");
            sb.AppendLine("================================================================================");
            
            if (_parser.Header.e_phnum == 0)
            {
                sb.AppendLine("There are no program headers in this file.");
                return sb.ToString();
            }

            // Calculate column widths
            string[] headers = ["Type", "Offset", "VirtAddr", "PhysAddr", "FileSize", "MemSize", "Flags", "Align"];
            int[] widths = [12, 10, 12, 12, 10, 10, 7, 15];

            // Print header
            sb.Append("  ");
            for (int i = 0; i < headers.Length; i++)
            {
                sb.Append(headers[i].PadRight(widths[i] + 2));
            }
            sb.AppendLine();

            if (_parser.ProgramHeaders32 != null)
            {
                foreach (var ph in _parser.ProgramHeaders32)
                {
                    sb.Append($"  {ELFParser.GetProgramHeaderType(ph.p_type)?.PadRight(widths[0] + 2)}");
                    sb.Append($"0x{ph.p_offset:x8}".PadRight(widths[1] + 2));
                    sb.Append($"0x{ph.p_vaddr:x8}".PadRight(widths[2] + 2));
                    sb.Append($"0x{ph.p_paddr:x8}".PadRight(widths[3] + 2));
                    sb.Append($"{ph.p_filesz,8} ".PadRight(widths[4] + 2));
                    sb.Append($"{ph.p_memsz,8} ".PadRight(widths[5] + 2));
                    sb.Append($"{ELFParser.GetProgramHeaderFlags(ph.p_flags)?.PadRight(widths[6] + 2)}");
                    sb.Append($"0x{ph.p_align:x}");
                    sb.AppendLine();
                }
            }
            else if (_parser.ProgramHeaders64 != null)
            {
                foreach (var ph in _parser.ProgramHeaders64)
                {
                    sb.Append($"  {ELFParser.GetProgramHeaderType(ph.p_type)?.PadRight(widths[0] + 2)}");
                    sb.Append($"0x{ph.p_offset:x8}".PadRight(widths[1] + 2));
                    sb.Append($"0x{ph.p_vaddr:x10}".PadRight(widths[2] + 2));
                    sb.Append($"0x{ph.p_paddr:x10}".PadRight(widths[3] + 2));
                    sb.Append($"{ph.p_filesz,10} ".PadRight(widths[4] + 2));
                    sb.Append($"{ph.p_memsz,10} ".PadRight(widths[5] + 2));
                    sb.Append($"{ELFParser.GetProgramHeaderFlags(ph.p_flags)?.PadRight(widths[6] + 2)}");
                    sb.Append($"0x{ph.p_align:x}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine();
            sb.AppendLine(" Section to Segment mapping:");
            sb.AppendLine("  Segment Sections...");
            
            if (_parser.ProgramHeaders32 != null)
            {
                for (int i = 0; i < _parser.ProgramHeaders32.Count; i++)
                {
                    var ph = _parser.ProgramHeaders32[i];
                    sb.Append($"   {i:D2}     ");
                    var sections = GetSectionsInSegment(ph);
                    sb.AppendLine(string.Join(" ", sections));
                }
            }
            else if (_parser.ProgramHeaders64 != null)
            {
                for (int i = 0; i < _parser.ProgramHeaders64.Count; i++)
                {
                    var ph = _parser.ProgramHeaders64[i];
                    sb.Append($"   {i:D2}     ");
                    var sections = GetSectionsInSegment(ph);
                    sb.AppendLine(string.Join(" ", sections));
                }
            }

            return sb.ToString();
        }

        public string GetFormattedSectionHeadersInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Section Headers:");
            sb.AppendLine("================================================================================");
            
            if (_parser.Header.e_shnum == 0)
            {
                sb.AppendLine("There are no section headers in this file.");
                return sb.ToString();
            }

            // Calculate column widths
            string[] headers = ["[Nr]", "Name", "Type", "Address", "Offset", "Size", "EntSize", "Flags", "Link", "Info", "Align"];
            int[] widths = [6, 15, 12, 12, 10, 10, 10, 10, 6, 6, 8];

            // Print header
            sb.Append("  ");
            for (int i = 0; i < headers.Length; i++)
            {
                sb.Append(headers[i].PadRight(widths[i] + 2));
            }
            sb.AppendLine();

            if (_parser.SectionHeaders32 != null)
            {
                for (int i = 0; i < _parser.SectionHeaders32.Count; i++)
                {
                    var sh = _parser.SectionHeaders32[i];
                    sb.Append($"  [{i,2}] ");
                    sb.Append($"{_parser.GetSectionName(i)?.PadRight(widths[1] + 2)}");
                    sb.Append($"{ELFParser.GetSectionType(sh.sh_type)?.PadRight(widths[2] + 2)}");
                    sb.Append($"0x{sh.sh_addr:x8}".PadRight(widths[3] + 2));
                    sb.Append($"0x{sh.sh_offset:x6}".PadRight(widths[4] + 2));
                    sb.Append($"{sh.sh_size,8} ".PadRight(widths[5] + 2));
                    sb.Append($"{sh.sh_entsize,8} ".PadRight(widths[6] + 2));
                    sb.Append($"{ELFParser.GetSectionFlags(sh.sh_flags, false)?.PadRight(widths[7] + 2)}");
                    sb.Append($"{sh.sh_link,4} ".PadRight(widths[8] + 2));
                    sb.Append($"{sh.sh_info,4} ".PadRight(widths[9] + 2));
                    sb.Append($"0x{sh.sh_addralign:x}");
                    sb.AppendLine();
                }
            }
            else if (_parser.SectionHeaders64 != null)
            {
                for (int i = 0; i < _parser.SectionHeaders64.Count; i++)
                {
                    var sh = _parser.SectionHeaders64[i];
                    sb.Append($"  [{i,2}] ");
                    sb.Append($"{_parser.GetSectionName(i)?.PadRight(widths[1] + 2)}");
                    sb.Append($"{ELFParser.GetSectionType(sh.sh_type)?.PadRight(widths[2] + 2)}");
                    sb.Append($"0x{sh.sh_addr:x10}".PadRight(widths[3] + 2));
                    sb.Append($"0x{sh.sh_offset:x8}".PadRight(widths[4] + 2));
                    sb.Append($"{sh.sh_size,10} ".PadRight(widths[5] + 2));
                    sb.Append($"{sh.sh_entsize,10} ".PadRight(widths[6] + 2));
                    sb.Append($"{ELFParser.GetSectionFlags(sh.sh_flags, true)?.PadRight(widths[7] + 2)}");
                    sb.Append($"{sh.sh_link,4} ".PadRight(widths[8] + 2));
                    sb.Append($"{sh.sh_info,4} ".PadRight(widths[9] + 2));
                    sb.Append($"0x{sh.sh_addralign:x}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine();
            sb.AppendLine("Key to Flags:");
            sb.AppendLine("  W (write), A (alloc), X (execute), M (merge), S (strings), I (info),");
            sb.AppendLine("  L (link order), O (extra OS processing required), G (group), T (TLS),");
            sb.AppendLine("  C (compressed), x (unknown), o (OS specific), E (exclude),");
            sb.AppendLine("  l (large), p (processor specific)");

            return sb.ToString();
        }

        public string GetFormattedSymbolTableInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Symbol table '.dynsym' contains information:");
            sb.AppendLine("================================================================================");
            
            if (_parser.Symbols32 != null && _parser.Symbols32.Count > 0)
            {
                // Print header
                string[] headers = ["Num", "Value", "Size", "Type", "Bind", "Vis", "Ndx", "Name"];
                int[] widths = [5, 12, 6, 8, 8, 6, 6, 20];

                sb.Append("  ");
                for (int i = 0; i < headers.Length; i++)
                {
                    sb.Append(headers[i].PadRight(widths[i] + 2));
                }
                sb.AppendLine();

                for (int i = 0; i < _parser.Symbols32.Count; i++)
                {
                    var sym = _parser.Symbols32[i];
                    sb.Append($"  {i,3} ");
                    sb.Append($"0x{sym.st_value:x8}".PadRight(widths[1] + 2));
                    sb.Append($"{sym.st_size,6} ".PadRight(widths[2] + 2));
                    sb.Append($"{ELFParser.GetSymbolType(sym.st_info)?.PadRight(widths[3] + 2)}");
                    sb.Append($"{ELFParser.GetSymbolBinding(sym.st_info)?.PadRight(widths[4] + 2)}");
                    sb.Append($"{ELFParser.GetSymbolVisibility(sym.st_other)?.PadRight(widths[5] + 2)}");
                    if (sym.st_shndx == 0)
                        sb.Append($"UND ".PadRight(widths[6] + 2));
                    else
                        sb.Append($"{sym.st_shndx,4} ".PadRight(widths[6] + 2));
                    sb.Append($"{_parser.GetSymbolName(sym) ?? string.Empty}");
                    sb.AppendLine();
                }
            }
            else if (_parser.Symbols64 != null && _parser.Symbols64.Count > 0)
            {
                // Print header
                string[] headers = ["Num", "Value", "Size", "Type", "Bind", "Vis", "Ndx", "Name"];
                int[] widths = [5, 16, 6, 8, 8, 6, 6, 20];

                sb.Append("  ");
                for (int i = 0; i < headers.Length; i++)
                {
                    sb.Append(headers[i].PadRight(widths[i] + 2));
                }
                sb.AppendLine();

                for (int i = 0; i < _parser.Symbols64.Count; i++)
                {
                    var sym = _parser.Symbols64[i];
                    sb.Append($"  {i,3} ");
                    sb.Append($"0x{sym.st_value:x12}".PadRight(widths[1] + 2));
                    sb.Append($"{sym.st_size,6} ".PadRight(widths[2] + 2));
                    sb.Append($"{ELFParser.GetSymbolType(sym.st_info)?.PadRight(widths[3] + 2)}");
                    sb.Append($"{ELFParser.GetSymbolBinding(sym.st_info)?.PadRight(widths[4] + 2)}");
                    sb.Append($"{ELFParser.GetSymbolVisibility(sym.st_other)?.PadRight(widths[5] + 2)}");
                    if (sym.st_shndx == 0)
                        sb.Append($"UND ".PadRight(widths[6] + 2));
                    else
                        sb.Append($"{sym.st_shndx,4} ".PadRight(widths[6] + 2));
                    sb.Append($"{_parser.GetSymbolName(sym) ?? string.Empty}");
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("No symbol table found in this file.");
            }

            return sb.ToString();
        }

        public string GetFormattedDynamicSectionInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Dynamic section contains information:");
            sb.AppendLine("================================================================================");
            
            if (_parser.DynamicEntries32 != null && _parser.DynamicEntries32.Count > 0)
            {
                sb.AppendLine("  Tag        Type                         Name/Value");
                foreach (var entry in _parser.DynamicEntries32)
                {
                    var tag = ELFParser.GetDynamicTagDescription(entry.d_tag);
                    sb.Append($" 0x{entry.d_tag:x16} ");
                    sb.Append($"{tag ?? string.Empty,-28} ");
                    
                    if (entry.d_tag == (long)DynamicTag.DT_FLAGS)
                    {
                        var flagDesc = ELFParser.GetDynamicFlagDescription(entry.d_val);
                        sb.Append($"{flagDesc} ");
                    }
                    else
                    {
                        sb.Append($"0x{entry.d_val:x} ");
                    }
                    sb.AppendLine();
                }
            }
            else if (_parser.DynamicEntries64 != null && _parser.DynamicEntries64.Count > 0)
            {
                sb.AppendLine("  Tag        Type                         Name/Value");
                foreach (var entry in _parser.DynamicEntries64)
                {
                    var tag = ELFParser.GetDynamicTagDescription(entry.d_tag);
                    sb.Append($" 0x{entry.d_tag:x16} ");
                    sb.Append($"{tag ?? string.Empty,-28} ");
                    
                    if (entry.d_tag == (long)DynamicTag.DT_FLAGS)
                    {
                        var flagDesc = ELFParser.GetDynamicFlagDescription((uint)entry.d_val);
                        sb.Append($"{flagDesc} ");
                    }
                    else
                    {
                        sb.Append($"0x{entry.d_val:x} ");
                    }
                    sb.AppendLine();
                }
            }
            else
            {
                sb.AppendLine("No dynamic section found in this file.");
            }

            return sb.ToString();
        }
        
        private List<string> GetSectionsInSegment(ELFProgramHeader32 ph)
        {
            var sections = new List<string>();
            if (_parser.SectionHeaders32 != null)
            {
                for (int i = 0; i < _parser.SectionHeaders32.Count; i++)
                {
                    var sh = _parser.SectionHeaders32[i];
                    if (sh.sh_addr >= ph.p_vaddr && sh.sh_addr < ph.p_vaddr + ph.p_memsz)
                    {
                        var sectionName = _parser.GetSectionName(i);
                        if (!string.IsNullOrEmpty(sectionName))
                        {
                            sections.Add(sectionName);
                        }
                    }
                }
            }
            return sections;
        }

        private List<string> GetSectionsInSegment(ELFProgramHeader64 ph)
        {
            var sections = new List<string>();
            if (_parser.SectionHeaders64 != null)
            {
                for (int i = 0; i < _parser.SectionHeaders64.Count; i++)
                {
                    var sh = _parser.SectionHeaders64[i];
                    if (sh.sh_addr >= ph.p_vaddr && sh.sh_addr < ph.p_vaddr + ph.p_memsz)
                    {
                        var sectionName = _parser.GetSectionName(i);
                        if (!string.IsNullOrEmpty(sectionName))
                        {
                            sections.Add(sectionName);
                        }
                    }
                }
            }
            return sections;
        }
    }
}