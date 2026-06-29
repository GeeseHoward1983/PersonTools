using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.Utils;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.Globalization;
using System.Text;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class ProgramHeaderHelper
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
                    sb.AppendLine(ConvertUtils.EnumerableToString(" ", sections));
                }
            }

            return sb.ToString();
        }

        private static List<string> GetSectionsInSegment(ELFParser _parser, ELFProgramHeader ph)
        {
            List<string> sections = [];
            if (_parser.SectionHeaders == null)
            {
                return sections;
            }

            // 段在虚拟内存中的结束地址（防 ulong 相加回绕，溢出时夹紧到 ulong.MaxValue）
            ulong segEndAddr;
            if (ph.p_memsz > ulong.MaxValue - ph.p_vaddr)
            {
                segEndAddr = ulong.MaxValue;
            }
            else
            {
                segEndAddr = ph.p_vaddr + ph.p_memsz;
            }
            for (int i = 0; i < _parser.SectionHeaders.Count; i++)
            {
                Models.ELFSectionHeader sh = _parser.SectionHeaders[i];
                if (sh.sh_size == 0)
                {
                    continue;
                }

                // 仅加载(SHF_ALLOC)节参与段映射：非分配节(.symtab/.strtab/.comment/.debug* 等)的 sh_addr 多为 0，
                // 在 PIE/共享库(首个 PT_LOAD 的 p_vaddr=0)时会与段地址 0 伪重叠而被错误归入起始段，与 readelf 不符
                if ((sh.sh_flags & (ulong)SectionAttributes.SHF_ALLOC) == 0)
                {
                    continue;
                }

                string sectionName = ELFSymbolNameResolver.GetSectionName(_parser, i);
                if (string.IsNullOrEmpty(sectionName))
                {
                    continue;
                }

                if (SectionOverlapsSegment(sh, ph.p_vaddr, segEndAddr))
                {
                    sections.Add(sectionName);
                }
            }
            return sections;
        }

        // 节是否与段在虚拟内存空间重叠：起始落入段 / 结束落入段 / 节包含段，三者之一即重叠
        private static bool SectionOverlapsSegment(Models.ELFSectionHeader sh, ulong segStart, ulong segEnd)
        {
            ulong secEndAddr = sh.sh_addr + sh.sh_size;
            return (sh.sh_addr >= segStart && sh.sh_addr < segEnd) ||
                   (secEndAddr > segStart && secEndAddr <= segEnd) ||
                   (sh.sh_addr <= segStart && secEndAddr >= segEnd);
        }

        internal static string GetInterpreterInfo(ELFParser Parser)
        {
            if (Parser.ProgramHeaders != null)
            {
                // Find the PT_INTERP segment in 64-bit headers
                ELFProgramHeader interpHeader = Parser.ProgramHeaders.FirstOrDefault(ph => ph.p_type == (uint)ProgramHeaderType.PT_INTERP);
                if (interpHeader.p_type != 0)
                {
                    // p_offset/p_filesz 为 ulong，>int.MaxValue 时 (int) 强转会变负，须先做越界夹紧
                    if (interpHeader.p_offset >= (ulong)Parser.FileData.Length)
                    {
                        return string.Empty;
                    }
                    int interpOffset = (int)interpHeader.p_offset;
                    int interpLength = (int)Math.Min(interpHeader.p_filesz, (ulong)Parser.FileData.Length - interpHeader.p_offset);
                    // 从 PT_INTERP 段按 p_filesz 上限提取解释器路径（ExtractStringFromBytes 自带越界夹紧并去除尾部 NUL）
                    return ELFParserUtils.ExtractStringFromBytes(Parser.FileData, interpOffset, interpLength);
                }
            }

            return string.Empty;
        }
    }
}