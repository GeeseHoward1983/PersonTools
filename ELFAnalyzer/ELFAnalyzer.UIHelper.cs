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
            sb.AppendLine($"  Magic:                             {GetMagicString()}");
            sb.AppendLine($"  Class:                             {_parser.GetELFClassName()} ({(_parser.Header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? "64-bit" : "32-bit")})");
            sb.AppendLine($"  Data:                              {_parser.GetELFDataName()} ({(_parser.Header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? "2's complement, little endian" : "2's complement, big endian")})");
            sb.AppendLine($"  Version:                           {_parser.GetReadableVersion()} ({_parser.Header.EI_VERSION})");
            sb.AppendLine($"  OS/ABI:                            {_parser.GetOSABIName()} ({_parser.Header.EI_OSABI})");
            sb.AppendLine($"  ABI Ver:                           {_parser.Header.EI_ABIVERSION}");
            sb.AppendLine($"  Type:                              {_parser.GetELFTypeName()} ({_parser.GetFileTypeDescription()})");
            sb.AppendLine($"  Machine:                           {_parser.GetArchitectureName()} ({_parser.GetMachineDescription()})");
            sb.AppendLine($"  Version:                           0x{_parser.Header.e_version:X}");
            sb.AppendLine($"  Entry point address:               {_parser.GetEntryPointAddress()}");
            sb.AppendLine($"  Start of program headers:          {(long)_parser.Header.e_phoff} (bytes into file)");
            sb.AppendLine($"  Start of section headers:          {(long)_parser.Header.e_shoff} (bytes into file)");
            sb.AppendLine($"  Flags:                             0x{_parser.Header.e_flags:X}  {(IsPIC() ? "Position Independent Code" : "")}");
            sb.AppendLine($"  Size of this header:               {_parser.GetHeaderSize()}");
            sb.AppendLine($"  Size of program headers:           {_parser.Header.e_phentsize} (bytes)");
            sb.AppendLine($"  Number of program headers:         {_parser.Header.e_phnum}");
            sb.AppendLine($"  Size of section headers:           {_parser.Header.e_shentsize} (bytes)");
            sb.AppendLine($"  Number of section headers:         {_parser.Header.e_shnum}");
            sb.AppendLine($"  Section header string table index: {_parser.Header.e_shstrndx}");
            return sb.ToString();
        }

        private bool IsPIC()
        {
            // Check if it's a shared object file with no relocations that would prevent PIC
            return _parser.Header.e_type == (ushort)ELFType.ET_DYN;
        }

        public string GetSectionToSegmentMappingInfo()
        {
            var sb = new StringBuilder();
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
                    sb.Append($"{tag,-28} ");
                    
                    // Handle entries that refer to string table
                    string? stringValue = null;
                    if (IsStringTableEntry(entry.d_tag))
                    {
                        // Find the string table to resolve the name
                        var strTabAddr = GetStringValueFromDynamicEntries(DynamicTag.DT_STRTAB);
                        var strTabSize = GetStringValueFromDynamicEntries(DynamicTag.DT_STRSZ);
                        
                        if (strTabAddr != 0 && strTabSize != 0 && _parser.SectionHeaders32 != null)
                        {
                            // Find the section that contains the string table
                            var stringTableSection = FindSectionByAddress(strTabAddr);
                            if (stringTableSection != null)
                            {
                                stringValue = ReadStringFromSection(stringTableSection, (uint)entry.d_val);
                            }
                        }
                    }
                    
                    if (entry.d_tag == (long)DynamicTag.DT_FLAGS)
                    {
                        var flagDesc = ELFParser.GetDynamicFlagDescription(entry.d_val);
                        sb.Append($"{flagDesc} ");
                    }
                    else if (stringValue != null)
                    {
                        sb.Append($"{stringValue} ");
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
                    sb.Append($"{tag,-28} ");
                    
                    // Handle entries that refer to string table
                    string? stringValue = null;
                    if (IsStringTableEntry(entry.d_tag))
                    {
                        // Find the string table to resolve the name
                        var strTabAddr = GetStringValueFromDynamicEntries64(DynamicTag.DT_STRTAB);
                        var strTabSize = GetStringValueFromDynamicEntries64(DynamicTag.DT_STRSZ);
                        
                        if (strTabAddr != 0 && strTabSize != 0 && _parser.SectionHeaders64 != null)
                        {
                            // Find the section that contains the string table
                            var stringTableSection = FindSectionByAddress64(strTabAddr);
                            if (stringTableSection != null)
                            {
                                stringValue = ReadStringFromSection64(stringTableSection, (ulong)entry.d_val);
                            }
                        }
                    }
                    
                    if (entry.d_tag == (long)DynamicTag.DT_FLAGS)
                    {
                        var flagDesc = ELFParser.GetDynamicFlagDescription((uint)entry.d_val);
                        sb.Append($"{flagDesc} ");
                    }
                    else if (stringValue != null)
                    {
                        sb.Append($"{stringValue} ");
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
        
        private static bool IsStringTableEntry(long tag)
        {
            return tag == (long)DynamicTag.DT_NEEDED || 
                   tag == (long)DynamicTag.DT_SONAME || 
                   tag == (long)DynamicTag.DT_RPATH || 
                   tag == (long)DynamicTag.DT_RUNPATH;
        }
        
        private uint GetStringValueFromDynamicEntries(DynamicTag tag)
        {
            if (_parser.DynamicEntries32 != null)
            {
                var entry = _parser.DynamicEntries32.FirstOrDefault(e => e.d_tag == (long)tag);
                if (entry.d_tag != 0)
                {
                    return entry.d_val;
                }
            }
            return 0;
        }
        
        private ulong GetStringValueFromDynamicEntries64(DynamicTag tag)
        {
            if (_parser.DynamicEntries64 != null)
            {
                var entry = _parser.DynamicEntries64.FirstOrDefault(e => e.d_tag == (long)tag);
                if (entry.d_tag != 0)
                {
                    return (ulong)entry.d_val;
                }
            }
            return 0;
        }
        
        private ELFSectionHeader32? FindSectionByAddress(ulong address)
        {
            if (_parser.SectionHeaders32 != null)
            {
                foreach (var section in _parser.SectionHeaders32)
                {
                    if (section.sh_addr == address)
                    {
                        return section;
                    }
                }
            }
            return null;
        }
        
        private ELFSectionHeader64? FindSectionByAddress64(ulong address)
        {
            if (_parser.SectionHeaders64 != null)
            {
                foreach (var section in _parser.SectionHeaders64)
                {
                    if (section.sh_addr == address)
                    {
                        return section;
                    }
                }
            }
            return null;
        }
        
        private string? ReadStringFromSection(ELFSectionHeader32? section, uint offset)
        {
            try
            {
                if (section != null && _parser.FileData != null && offset < _parser.FileData.Length && section.Value.sh_offset + offset < _parser.FileData.Length)
                {
                    var start = (int)(section.Value.sh_offset + offset);
                    var end = start;
                    while (end < _parser.FileData.Length && _parser.FileData[end] != 0)
                    {
                        end++;
                    }
                    
                    if (end > start)
                    {
                        return Encoding.UTF8.GetString(_parser.FileData, start, end - start);
                    }
                }
            }
            catch
            {
                // If there's an error reading the string, return null
            }
            return null;
        }
        
        private string? ReadStringFromSection64(ELFSectionHeader64? section, ulong offset)
        {
            try
            {
                if (section != null && _parser.FileData != null && 
                    offset < (ulong)_parser.FileData.Length && 
                    section.Value.sh_offset + offset < (ulong)_parser.FileData.Length)
                {
                    var start = (int)(section.Value.sh_offset + offset);
                    var end = start;
                    while (end < _parser.FileData.Length && _parser.FileData[end] != 0)
                    {
                        end++;
                    }
                    
                    if (end > start)
                    {
                        return Encoding.UTF8.GetString(_parser.FileData, start, end - start);
                    }
                }
            }
            catch
            {
                // If there's an error reading the string, return null
            }
            return null;
        }
        
        public List<ProgramHeaderInfo> GetProgramHeaderInfoList()
        {
            var result = new List<ProgramHeaderInfo>();

            if (_parser.ProgramHeaders32 != null)
            {
                foreach (var ph in _parser.ProgramHeaders32)
                {
                    result.Add(new ProgramHeaderInfo
                    {
                        Type = ELFParser.GetProgramHeaderType(ph.p_type) ?? "PT_UNKNOWN",
                        Offset = $"0x{ph.p_offset:x8}",
                        VirtAddr = $"0x{ph.p_vaddr:x8}",
                        PhysAddr = $"0x{ph.p_paddr:x8}",
                        FileSize = $"{ph.p_filesz}",
                        MemSize = $"{ph.p_memsz}",
                        Flags = ELFParser.GetProgramHeaderFlags(ph.p_flags) ?? "",
                        Align = $"0x{ph.p_align:x}"
                    });
                }
            }
            else if (_parser.ProgramHeaders64 != null)
            {
                foreach (var ph in _parser.ProgramHeaders64)
                {
                    result.Add(new ProgramHeaderInfo
                    {
                        Type = ELFParser.GetProgramHeaderType(ph.p_type) ?? "PT_UNKNOWN",
                        Offset = $"0x{ph.p_offset:x16}",
                        VirtAddr = $"0x{ph.p_vaddr:x16}",
                        PhysAddr = $"0x{ph.p_paddr:x16}",
                        FileSize = $"{ph.p_filesz}",
                        MemSize = $"{ph.p_memsz}",
                        Flags = ELFParser.GetProgramHeaderFlags(ph.p_flags) ?? "",
                        Align = $"0x{ph.p_align:x}"
                    });
                }
            }

            return result;
        }

        public List<ELFSectionHeaderInfo> GetSectionHeaderInfoList()
        {
            var result = new List<ELFSectionHeaderInfo>();

            if (_parser.SectionHeaders32 != null)
            {
                for (int i = 0; i < _parser.SectionHeaders32.Count; i++)
                {
                    var sh = _parser.SectionHeaders32[i];
                    result.Add(new ELFSectionHeaderInfo
                    {
                        Index = i,
                        Name = _parser.GetSectionName(i) ?? string.Empty,
                        Type = ELFParser.GetSectionType(sh.sh_type) ?? string.Empty,
                        Address = $"0x{sh.sh_addr:x8}",
                        Offset = $"0x{sh.sh_offset:x6}",
                        Size = $"{sh.sh_size}",
                        EntSize = $"{sh.sh_entsize}",
                        Flags = ELFParser.GetSectionFlags(sh.sh_flags, false) ?? string.Empty,
                        Link = $"{sh.sh_link}",
                        Info = $"{sh.sh_info}",
                        Align = $"0x{sh.sh_addralign:x}"
                    });
                }
            }
            else if (_parser.SectionHeaders64 != null)
            {
                for (int i = 0; i < _parser.SectionHeaders64.Count; i++)
                {
                    var sh = _parser.SectionHeaders64[i];
                    result.Add(new ELFSectionHeaderInfo
                    {
                        Index = i,
                        Name = _parser.GetSectionName(i) ?? string.Empty,
                        Type = ELFParser.GetSectionType(sh.sh_type) ?? string.Empty,
                        Address = $"0x{sh.sh_addr:x10}",
                        Offset = $"0x{sh.sh_offset:x8}",
                        Size = $"{sh.sh_size}",
                        EntSize = $"{sh.sh_entsize}",
                        Flags = ELFParser.GetSectionFlags(sh.sh_flags, true) ?? string.Empty,
                        Link = $"{sh.sh_link}",
                        Info = $"{sh.sh_info}",
                        Align = $"0x{sh.sh_addralign:x}"
                    });
                }
            }

            return result;
        }

        public List<ELFSymbolTableInfo> GetSymbolTableInfoList()
        {
            var result = new List<ELFSymbolTableInfo>();

            if (_parser.Symbols32 != null && _parser.Symbols32.Count > 0)
            {
                for (int i = 0; i < _parser.Symbols32.Count; i++)
                {
                    var sym = _parser.Symbols32[i];
                    result.Add(new ELFSymbolTableInfo
                    {
                        Number = i,
                        Value = $"0x{sym.st_value:x8}",
                        Size = $"{sym.st_size}",
                        Type = ELFParser.GetSymbolType(sym.st_info) ?? string.Empty,
                        Bind = ELFParser.GetSymbolBinding(sym.st_info) ?? string.Empty,
                        Vis = ELFParser.GetSymbolVisibility(sym.st_other) ?? string.Empty,
                        Ndx = sym.st_shndx == 0 ? "UND" : $"{sym.st_shndx}",
                        Name = _parser.GetSymbolName(sym) ?? string.Empty
                    });
                }
            }
            else if (_parser.Symbols64 != null && _parser.Symbols64.Count > 0)
            {
                for (int i = 0; i < _parser.Symbols64.Count; i++)
                {
                    var sym = _parser.Symbols64[i];
                    result.Add(new ELFSymbolTableInfo
                    {
                        Number = i,
                        Value = $"0x{sym.st_value:x12}",
                        Size = $"{sym.st_size}",
                        Type = ELFParser.GetSymbolType(sym.st_info) ?? string.Empty,
                        Bind = ELFParser.GetSymbolBinding(sym.st_info) ?? string.Empty,
                        Vis = ELFParser.GetSymbolVisibility(sym.st_other) ?? string.Empty,
                        Ndx = sym.st_shndx == 0 ? "UND" : $"{sym.st_shndx}",
                        Name = _parser.GetSymbolName(sym) ?? string.Empty
                    });
                }
            }

            return result;
        }

        public List<ELFDynamicSectionInfo> GetDynamicSectionInfoList()
        {
            var result = new List<ELFDynamicSectionInfo>();

            if (_parser.DynamicEntries32 != null && _parser.DynamicEntries32.Count > 0)
            {
                foreach (var entry in _parser.DynamicEntries32)
                {
                    var tag = ELFParser.GetDynamicTagDescription(entry.d_tag);
                    string value = string.Empty;

                    // Handle entries that refer to string table
                    if (IsStringTableEntry(entry.d_tag))
                    {
                        // Find the string table to resolve the name
                        var strTabAddr = GetStringValueFromDynamicEntries(DynamicTag.DT_STRTAB);
                        var strTabSize = GetStringValueFromDynamicEntries(DynamicTag.DT_STRSZ);

                        if (strTabAddr != 0 && strTabSize != 0 && _parser.SectionHeaders32 != null)
                        {
                            // Find the section that contains the string table
                            var stringTableSection = FindSectionByAddress(strTabAddr);
                            if (stringTableSection != null)
                            {
                                value = ReadStringFromSection(stringTableSection, (uint)entry.d_val) ?? $"0x{entry.d_val:x}";
                            }
                        }
                    }
                    else if (entry.d_tag == (long)DynamicTag.DT_FLAGS)
                    {
                        value = ELFParser.GetDynamicFlagDescription(entry.d_val);
                    }
                    else
                    {
                        value = $"0x{entry.d_val:x}";
                    }

                    result.Add(new ELFDynamicSectionInfo
                    {
                        Tag = $"0x{entry.d_tag:x16}",
                        Type = tag,
                        Value = value
                    });
                }
            }
            else if (_parser.DynamicEntries64 != null && _parser.DynamicEntries64.Count > 0)
            {
                foreach (var entry in _parser.DynamicEntries64)
                {
                    var tag = ELFParser.GetDynamicTagDescription(entry.d_tag);
                    string value = string.Empty;

                    // Handle entries that refer to string table
                    if (IsStringTableEntry(entry.d_tag))
                    {
                        // Find the string table to resolve the name
                        var strTabAddr = GetStringValueFromDynamicEntries64(DynamicTag.DT_STRTAB);
                        var strTabSize = GetStringValueFromDynamicEntries64(DynamicTag.DT_STRSZ);

                        if (strTabAddr != 0 && strTabSize != 0 && _parser.SectionHeaders64 != null)
                        {
                            // Find the section that contains the string table
                            var stringTableSection = FindSectionByAddress64(strTabAddr);
                            if (stringTableSection != null)
                            {
                                value = ReadStringFromSection64(stringTableSection, (ulong)entry.d_val) ?? $"0x{entry.d_val:x}";
                            }
                        }
                    }
                    else if (entry.d_tag == (long)DynamicTag.DT_FLAGS)
                    {
                        value = ELFParser.GetDynamicFlagDescription((uint)entry.d_val);
                    }
                    else
                    {
                        value = $"0x{entry.d_val:x}";
                    }

                    result.Add(new ELFDynamicSectionInfo
                    {
                        Tag = $"0x{entry.d_tag:x16}",
                        Type = tag,
                        Value = value
                    });
                }
            }

            return result;
        }

        private List<string> GetSectionsInSegment(ELFProgramHeader32 ph)
        {
            var sections = new List<string>();
            if (_parser.SectionHeaders32 != null)
            {
                // Determine if this is a loadable segment
                bool isLoadableSegment = ph.p_type == (uint)ProgramHeaderType.PT_LOAD;
                
                // Calculate the end address of the segment based on memory size
                ulong segEndAddr = ph.p_vaddr + ph.p_memsz;
                
                for (int i = 0; i < _parser.SectionHeaders32.Count; i++)
                {
                    var sh = _parser.SectionHeaders32[i];
                    
                    // Skip sections with zero size or invalid addresses
                    if (sh.sh_size == 0 || string.IsNullOrEmpty(_parser.GetSectionName(i)))
                        continue;
                        
                    // Calculate the end address of the section
                    ulong secEndAddr = sh.sh_addr + sh.sh_size;
                    
                    // Check if section overlaps with segment in virtual memory space
                    // Three cases of overlap:
                    // 1. Section starts within segment: sh.sh_addr >= ph.p_vaddr && sh.sh_addr < segEndAddr
                    // 2. Section ends within segment: secEndAddr > ph.p_vaddr && secEndAddr <= segEndAddr
                    // 3. Section completely contains segment: sh.sh_addr <= ph.p_vaddr && secEndAddr >= segEndAddr
                    bool overlapsInVirtualMemory = (sh.sh_addr >= ph.p_vaddr && sh.sh_addr < segEndAddr) || 
                                                  (secEndAddr > ph.p_vaddr && secEndAddr <= segEndAddr) ||
                                                  (sh.sh_addr <= ph.p_vaddr && secEndAddr >= segEndAddr);
                    
                    if (overlapsInVirtualMemory)
                    {
                        var sectionName = _parser.GetSectionName(i);
                        if (!string.IsNullOrEmpty(sectionName))
                        {
                            // Skip certain sections for loadable segments to match readelf behavior
                            if (isLoadableSegment && 
                               (sectionName == ".comment" || sectionName == ".shstrtab"))
                            {
                                // Skip adding these sections to loadable segments
                                continue;
                            }
                            
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
                // Calculate the end address of the segment based on memory size
                ulong segEndAddr = ph.p_vaddr + ph.p_memsz;
                
                for (int i = 0; i < _parser.SectionHeaders64.Count; i++)
                {
                    var sh = _parser.SectionHeaders64[i];
                    
                    // Skip sections with zero size or invalid addresses
                    if (sh.sh_size == 0 || string.IsNullOrEmpty(_parser.GetSectionName(i)))
                        continue;
                        
                    // Calculate the end address of the section
                    ulong secEndAddr = sh.sh_addr + sh.sh_size;
                    
                    // Check if section overlaps with segment in virtual memory space
                    // Three cases of overlap:
                    // 1. Section starts within segment: sh.sh_addr >= ph.p_vaddr && sh.sh_addr < segEndAddr
                    // 2. Section ends within segment: secEndAddr > ph.p_vaddr && secEndAddr <= segEndAddr
                    // 3. Section completely contains segment: sh.sh_addr <= ph.p_vaddr && secEndAddr >= segEndAddr
                    bool overlapsInVirtualMemory = (sh.sh_addr >= ph.p_vaddr && sh.sh_addr < segEndAddr) || 
                                                  (secEndAddr > ph.p_vaddr && secEndAddr <= segEndAddr) ||
                                                  (sh.sh_addr <= ph.p_vaddr && secEndAddr >= segEndAddr);
                    
                    if (overlapsInVirtualMemory)
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