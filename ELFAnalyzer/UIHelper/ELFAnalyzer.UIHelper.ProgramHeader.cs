using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.Text;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    public class ProgrameHeaderHelper
    {
        public static List<ProgramHeaderInfo> GetProgramHeaderInfoList(ELFParser _parser)
        {
            var result = new List<ProgramHeaderInfo>();

            if (_parser.ProgramHeaders != null)
            {
                foreach (var ph in _parser.ProgramHeaders)
                {
                    result.Add(new ProgramHeaderInfo
                    {
                        Type = ELFProgramHeaderInfo.GetProgramHeaderType(ph.p_type) ?? "UNKNOWN",
                        Offset = $"0x{ph.p_offset:x16}",
                        VirtAddr = $"0x{ph.p_vaddr:x16}",
                        PhysAddr = $"0x{ph.p_paddr:x16}",
                        FileSize = $"{ph.p_filesz}",
                        MemSize = $"{ph.p_memsz}",
                        Flags = ELFProgramHeaderInfo.GetProgramHeaderFlags(ph.p_flags) ?? "",
                        Align = $"0x{ph.p_align:x}"
                    });
                }
            }

            return result;
        }

        public static string GetSectionToSegmentMappingInfo(ELFParser _parser)
        {
            var sb = new StringBuilder();
            sb.AppendLine(" Section to Segment mapping:");
            sb.AppendLine("  段节...");
            
            if (_parser.ProgramHeaders != null)
            {
                for (int i = 0; i < _parser.ProgramHeaders.Count; i++)
                {
                    var ph = _parser.ProgramHeaders[i];
                    sb.Append($"   {i:D2}     ");
                    var sections = GetSectionsInSegment(_parser, ph);
                    sb.AppendLine(string.Join(" ", sections));
                }
            }

            return sb.ToString();
        }

        private static List<string> GetSectionsInSegment(ELFParser _parser, ELFProgramHeader ph)
        {
            var sections = new List<string>();
            if (_parser.SectionHeaders != null)
            {               
                // Calculate the end address of the segment based on memory size
                ulong segEndAddr = ph.p_vaddr + ph.p_memsz;
                
                for (int i = 0; i < _parser.SectionHeaders.Count; i++)
                {
                    var sh = _parser.SectionHeaders[i];
                    
                    // Skip sections with zero size or invalid addresses
                    if (sh.sh_size == 0 || string.IsNullOrEmpty(SymbleName.GetSectionName(_parser, i)))
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
                        var sectionName = SymbleName.GetSectionName(_parser, i);
                        if (!string.IsNullOrEmpty(sectionName))
                        {                            
                            sections.Add(sectionName);
                        }
                    }
                }
            }
            return sections;
        }

        public static string GetInterpreterInfo(ELFParser _parser)
        {
            if (_parser.ProgramHeaders != null)
            {
                // Find the PT_INTERP segment in 64-bit headers
                var interpHeader = _parser.ProgramHeaders.FirstOrDefault(ph => ph.p_type == (uint)ProgramHeaderType.PT_INTERP);
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

            return string.Empty;
        }
    }
}