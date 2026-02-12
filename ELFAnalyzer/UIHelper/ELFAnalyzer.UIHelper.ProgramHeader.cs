using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.Globalization;
using System.Text;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class ProgrameHeaderHelper
    {
        internal static List<ProgramHeaderInfo> GetProgramHeaderInfoList(ELFParser Parser)
        {
            List<ProgramHeaderInfo> result = [];

            if (Parser.ProgramHeaders != null)
            {
                foreach (ELFProgramHeader ph in Parser.ProgramHeaders)
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

        internal static string GetSectionToSegmentMappingInfo(ELFParser Parser)
        {
            StringBuilder sb = new();
            sb.AppendLine(" Section to Segment mapping:");
            sb.AppendLine("  段节...");

            if (Parser.ProgramHeaders != null)
            {
                for (int i = 0; i < Parser.ProgramHeaders.Count; i++)
                {
                    ELFProgramHeader ph = Parser.ProgramHeaders[i];
                    sb.Append(CultureInfo.InvariantCulture, $"   {i:D2}     ");
                    List<string> sections = GetSectionsInSegment(Parser, ph);
                    sb.AppendLine(Utils.EnumerableToString(" ", sections));
                }
            }

            return sb.ToString();
        }

        private static List<string> GetSectionsInSegment(ELFParser _parser, ELFProgramHeader ph)
        {
            List<string> sections = [];
            if (_parser.SectionHeaders != null)
            {
                // Calculate the end address of the segment based on memory size
                ulong segEndAddr = ph.p_vaddr + ph.p_memsz;

                for (int i = 0; i < _parser.SectionHeaders.Count; i++)
                {
                    Models.ELFSectionHeader sh = _parser.SectionHeaders[i];

                    // Skip sections with zero size or invalid addresses
                    if (sh.sh_size == 0 || string.IsNullOrEmpty(SymbleName.GetSectionName(_parser, i)))
                    {
                        continue;
                    }

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
                        string sectionName = SymbleName.GetSectionName(_parser, i);
                        if (!string.IsNullOrEmpty(sectionName))
                        {
                            sections.Add(sectionName);
                        }
                    }
                }
            }
            return sections;
        }

        internal static string GetInterpreterInfo(ELFParser Parser)
        {
            if (Parser.ProgramHeaders != null)
            {
                // Find the PT_INTERP segment in 64-bit headers
                ELFProgramHeader interpHeader = Parser.ProgramHeaders.FirstOrDefault(ph => ph.p_type == (uint)ProgramHeaderType.PT_INTERP);
                if (interpHeader.p_type != 0)
                {
                    // Read the interpreter string from the file data
                    int start = (int)interpHeader.p_offset;
                    int end = start;
                    while (end < Parser.FileData.Length && Parser.FileData[end] != 0 && (ulong)(end - start) < interpHeader.p_filesz)
                    {
                        end++;
                    }

                    if (end > start)
                    {
                        return Encoding.UTF8.GetString(Parser.FileData, start, end - start);
                    }
                }
            }

            return string.Empty;
        }
    }
}