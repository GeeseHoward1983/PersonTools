using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class SectionHeaderHelper
    {
        private readonly record struct SectionIdentity(int Index, string Name, string Type);
        private readonly record struct SectionLayout(string Address, string Offset, string Size, string EntrySize, string Alignment);
        private readonly record struct SectionMetadata(string Flags, string Link, string Info);

        internal static List<ELFSectionHeaderInfo> GetSectionHeaderInfoList(ELFParser parser)
        {
            return GetSectionHeaders(parser).Select((section, index) => CreateSectionInfo(parser, section, index)).ToList();
        }

        private static List<ELFSectionHeader> GetSectionHeaders(ELFParser parser) => parser.SectionHeaders ?? [];

        private static ELFSectionHeaderInfo CreateSectionInfo(ELFParser parser, ELFSectionHeader section, int index)
        {
            return AssembleSectionInfo(CreateIdentity(parser, section, index),
                CreateLayout(section, parser.Is64Bit), CreateMetadata(section));
        }

        private static ELFSectionHeaderInfo AssembleSectionInfo(SectionIdentity identity,
            SectionLayout layout, SectionMetadata metadata)
        {
            // 行组装只映射已格式化字段，不混入布局、标志和数值的转换规则。
            return new ELFSectionHeaderInfo
            {
                Index = identity.Index,
                Name = identity.Name,
                Type = identity.Type,
                Address = layout.Address,
                Offset = layout.Offset,
                Size = layout.Size,
                EntSize = layout.EntrySize,
                Flags = metadata.Flags,
                Link = metadata.Link,
                Info = metadata.Info,
                Align = layout.Alignment
            };
        }

        private static SectionIdentity CreateIdentity(ELFParser parser, ELFSectionHeader section, int index)
        {
            return new SectionIdentity(index, ELFSymbolNameResolver.GetSectionName(parser, index),
                ELFSectionHeaderReader.GetSectionType(section.sh_type));
        }

        private static SectionLayout CreateLayout(ELFSectionHeader section, bool is64Bit)
        {
            // 地址和偏移保留原有位宽，大小与对齐仍采用原显示格式。
            return new SectionLayout(
                ELFDisplayFormatting.Address(section.sh_addr, is64Bit),
                ELFDisplayFormatting.Address(section.sh_offset, is64Bit),
                ELFDisplayFormatting.Number(section.sh_size),
                ELFDisplayFormatting.Number(section.sh_entsize),
                $"0x{section.sh_addralign:x}");
        }

        private static SectionMetadata CreateMetadata(ELFSectionHeader section)
        {
            return new SectionMetadata(ELFSectionHeaderReader.GetSectionFlags(section.sh_flags),
                ELFDisplayFormatting.Number(section.sh_link), ELFDisplayFormatting.Number(section.sh_info));
        }
    }
}
