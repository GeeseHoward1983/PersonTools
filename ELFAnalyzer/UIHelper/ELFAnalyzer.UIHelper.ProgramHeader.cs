using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using System.Text;

namespace PersonalTools.ELFAnalyzer
{
    public partial class ELFAnalyzer
    {
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

        public string? GetInterpreterInfo()
        {
            if (_parser.ProgramHeaders32 != null)
            {
                // Find the PT_INTERP segment in 32-bit headers
                var interpHeader = _parser.ProgramHeaders32.FirstOrDefault(ph => ph.p_type == (uint)ProgramHeaderType.PT_INTERP);
                if (interpHeader.p_type != 0)
                {
                    // Read the interpreter string from the file data
                    var start = (int)interpHeader.p_offset;
                    var end = start;
                    while (end < _parser.FileData.Length && _parser.FileData[end] != 0 && (end - start) < interpHeader.p_filesz)
                    {
                        end++;
                    }

                    if (end > start)
                    {
                        return Encoding.UTF8.GetString(_parser.FileData, start, end - start);
                    }
                }
            }
            else if (_parser.ProgramHeaders64 != null)
            {
                // Find the PT_INTERP segment in 64-bit headers
                var interpHeader = _parser.ProgramHeaders64.FirstOrDefault(ph => ph.p_type == (uint)ProgramHeaderType.PT_INTERP);
                if (interpHeader.p_type != 0)
                {
                    // Read the interpreter string from the file data
                    var start = (int)interpHeader.p_offset;
                    var end = start;
                    while (end < _parser.FileData.Length && _parser.FileData[end] != 0 && (ulong)(end - start) < interpHeader.p_filesz)
                    {
                        end++;
                    }

                    if (end > start)
                    {
                        return Encoding.UTF8.GetString(_parser.FileData, start, end - start);
                    }
                }
            }

            return null;
        }
    }
}