using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class SectionHeaderHelper
    {
        internal static List<ELFSectionHeaderInfo> GetSectionHeaderInfoList(ELFParser Parser)
        {
            List<ELFSectionHeaderInfo> result = [];

            if (Parser.SectionHeaders != null)
            {
                for (int i = 0; i < Parser.SectionHeaders.Count; i++)
                {
                    Models.ELFSectionHeader sh = Parser.SectionHeaders[i];
                    result.Add(new ELFSectionHeaderInfo
                    {
                        Index = i,
                        Name = ELFSymbolNameResolver.GetSectionName(Parser, i),
                        Type = Core.ELFSectionHeaderReader.GetSectionType(sh.sh_type),
                        // 十六进制宽度按位宽区分(64位=16位宽/32位=8位宽)，与 SymbolTableHelper 及 readelf 一致
                        Address = Parser.Is64Bit ? $"0x{sh.sh_addr:x16}" : $"0x{sh.sh_addr:x8}",
                        Offset = Parser.Is64Bit ? $"0x{sh.sh_offset:x16}" : $"0x{sh.sh_offset:x8}",
                        Size = $"{sh.sh_size}",
                        EntSize = $"{sh.sh_entsize}",
                        Flags = Core.ELFSectionHeaderReader.GetSectionFlags(sh.sh_flags),
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