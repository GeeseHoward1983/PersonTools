using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using System.Text;

namespace PersonalTools.ELFAnalyzer
{
    public partial class ELFAnalyzer
    {
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
    }
}