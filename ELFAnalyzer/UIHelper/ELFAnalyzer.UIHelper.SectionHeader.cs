using PersonalTools.ELFAnalyzer.Models;

namespace PersonalTools.ELFAnalyzer
{
    public partial class ELFAnalyzer
    {
        public List<ELFSectionHeaderInfo> GetSectionHeaderInfoList()
        {
            var result = new List<ELFSectionHeaderInfo>();

            if (_parser.SectionHeaders != null)
            {
                for (int i = 0; i < _parser.SectionHeaders.Count; i++)
                {
                    var sh = _parser.SectionHeaders[i];
                    result.Add(new ELFSectionHeaderInfo
                    {
                        Index = i,
                        Name = _parser.GetSectionName(i) ?? string.Empty,
                        Type = Core.ELFSectionHeader.GetSectionType(sh.sh_type) ?? string.Empty,
                        Address = $"0x{sh.sh_addr:x10}",
                        Offset = $"0x{sh.sh_offset:x8}",
                        Size = $"{sh.sh_size}",
                        EntSize = $"{sh.sh_entsize}",
                        Flags = Core.ELFSectionHeader.GetSectionFlags(sh.sh_flags) ?? string.Empty,
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