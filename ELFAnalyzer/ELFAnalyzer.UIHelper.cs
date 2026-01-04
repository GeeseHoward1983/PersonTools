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
            sb.AppendLine($"  Magic:            {GetMagicString()}");
            sb.AppendLine($"  类别:             {_parser.GetELFClassName()} ({(_parser.Header.EI_CLASS == (byte)ELFClass.ELFCLASS64 ? "64-bit" : "32-bit")})");
            sb.AppendLine($"  数据:             {_parser.GetELFDataName()} ({(_parser.Header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? "2's complement, little endian" : "2's complement, big endian")})");
            sb.AppendLine($"  版本:             {_parser.GetReadableVersion()} ({_parser.Header.EI_VERSION})");
            sb.AppendLine($"  OS/ABI:           {_parser.GetOSABIName()} ({_parser.Header.EI_OSABI})");
            sb.AppendLine($"  ABI 版本:         {_parser.Header.EI_ABIVERSION}");
            sb.AppendLine($"  类型:             {_parser.GetELFTypeName()} ({_parser.GetFileTypeDescription()})");
            sb.AppendLine($"  系统架构:         {_parser.GetArchitectureName()} ({_parser.GetMachineDescription()})");
            sb.AppendLine($"  版本:             0x{_parser.Header.e_version:X}");
            sb.AppendLine($"  入口点地址:       {_parser.GetEntryPointAddress()}");
            sb.AppendLine($"  程序头起点:       {(long)_parser.Header.e_phoff} (bytes into file)");
            sb.AppendLine($"  节头的起点:       {(long)_parser.Header.e_shoff} (bytes into file)");
            sb.AppendLine($"  标志:             0x{_parser.Header.e_flags:X}  {(IsPIC() ? "Position Independent Code" : "")}");
            sb.AppendLine($"  本头的大小:       {_parser.GetHeaderSize()}");
            sb.AppendLine($"  程序头的大小:     {_parser.Header.e_phentsize} (bytes)");
            sb.AppendLine($"  程序头数量:       {_parser.Header.e_phnum}");
            sb.AppendLine($"  节头大小:         {_parser.Header.e_shentsize} (bytes)");
            sb.AppendLine($"  节头数量:         {_parser.Header.e_shnum}");
            sb.AppendLine($"  字符串表索引节头: {_parser.Header.e_shstrndx}");
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
            sb.AppendLine("  段节...");
            
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
                        Type = ELFParser.GetProgramHeaderType(ph.p_type) ?? "UNKNOWN",
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
                        Type = ELFParser.GetProgramHeaderType(ph.p_type) ?? "UNKNOWN",
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
                _ = ph.p_type == (uint)ProgramHeaderType.PT_LOAD;

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