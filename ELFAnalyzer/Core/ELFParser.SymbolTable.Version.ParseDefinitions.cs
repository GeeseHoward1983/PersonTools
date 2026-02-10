using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class VersionSymbleTable
    {
        private static void ParseVersionDefinitions(ELFParser parser)
        {
            // 查找版本定义 (DT_VERDEF)
            long verdefAddr = 0;
            long verdefNum = 0;

            if (parser.DynamicEntries != null)
            {
                foreach (var entry in parser.DynamicEntries)
                {
                    if (entry.d_tag == (long)DynamicTag.DT_VERDEF)
                    {
                        verdefAddr = (long)entry.d_val;
                    }
                    else if (entry.d_tag == (long)DynamicTag.DT_VERDEFNUM)
                    {
                        verdefNum = (long)entry.d_val;
                    }
                }
            }

            if (verdefAddr > 0 && verdefNum > 0)
            {
                parser.VersionDefinitions = [];

                var verdefSection = FindSectionByAddress(parser, (ulong)verdefAddr);
                if (verdefSection != null)
                {
                    ParseVerDefEntries(parser,  verdefSection.Value, (int)verdefNum);
                }
            }
            else
            {
                // 如果动态段中没有找到版本定义信息，则直接查找SHT_GNU_VERDEF类型的节
                FindAndParseVersionDefinitionSection(parser);
            }
        }

        private static void FindAndParseVersionDefinitionSection(ELFParser parser)
        {
            if (parser.SectionHeaders == null || parser.VersionDefinitions != null) return;

            // 确保_versionDefinitions字典存在
            if (parser.VersionDefinitions == null)
            {
                parser.VersionDefinitions = [];
            }

            // 遍历所有节头查找SHT_GNU_VERDEF类型的节（即.gnu.version_d）
            for (int i = 0; i < parser.SectionHeaders.Count; i++)
            {
                var section = parser.SectionHeaders[i];
                if (section.sh_type == (uint)SectionType.SHT_GNU_verdef)
                {
                    ParseVerDefEntries(parser, section, CalculateVerDefEntryCount(section));
                }
            }
        }

        private static int CalculateVerDefEntryCount(Models.ELFSectionHeader section)
        {
            if (section.sh_entsize == 0) return 0;
            return (int)(section.sh_size / section.sh_entsize);
        }

        private static void ParseVerDefEntries(ELFParser parser, Models.ELFSectionHeader section, int count)
        {
            if (parser.SectionHeaders == null || parser.VersionDefinitions == null) return;
            
            // 找到版本定义字符串表
            int strTabIdx = (int)section.sh_link;
            if (strTabIdx >= parser.SectionHeaders.Count) return;
            
            var strTabSection = parser.SectionHeaders[strTabIdx];
            var strTabData = new byte[strTabSection.sh_size];
            Array.Copy(parser.FileData, (long)strTabSection.sh_offset, strTabData, 0, (int)strTabSection.sh_size);
            
            ulong offset = section.sh_offset;
            int processed = 0;
            
            while (processed < count && offset < (ulong)parser.FileData.Length)
            {
                if (!parser.Header.IsLittleEndian()) // 如果不是小端序
                {
                    Array.Reverse(parser.FileData, (int)offset, 2);
                    Array.Reverse(parser.FileData, (int)offset + 2, 2);
                    Array.Reverse(parser.FileData, (int)offset + 4, 2);
                    Array.Reverse(parser.FileData, (int)offset + 6, 2);
                    if (parser.Is64Bit)
                    { 
                        Array.Reverse(parser.FileData, (int)offset + 8, 8);
                        Array.Reverse(parser.FileData, (int)offset + 16, 8);
                    }
                    else {
                        Array.Reverse(parser.FileData, (int)offset + 12, 4);
                        Array.Reverse(parser.FileData, (int)offset + 16, 4);
                    }
                }

                _ = BitConverter.ToUInt16(parser.FileData, (int)offset);
                _ = BitConverter.ToUInt16(parser.FileData, (int)offset + 2);
                var vd_ndx = BitConverter.ToUInt16(parser.FileData, (int)offset + 4);
                _ = BitConverter.ToUInt16(parser.FileData, (int)offset + 6);
                // Skip vd_hash at offset+8

                var vd_aux = parser.Is64Bit ? BitConverter.ToUInt64(parser.FileData, (int)offset + 8) : BitConverter.ToUInt32(parser.FileData, (int)offset + 12);  // 64位的vd_aux和vd_next
                var vd_next = parser.Is64Bit ? BitConverter.ToUInt64(parser.FileData, (int)offset + 16) : BitConverter.ToUInt32(parser.FileData, (int)offset + 16);

                // 获取版本名称
                // vernaux 结构紧跟在 verdef 结构之后
                ulong nameOffset = offset + vd_aux;
                if (!parser.Header.IsLittleEndian()) // 如果不是小端序
                {
                    Array.Reverse(parser.FileData, (int)nameOffset + 8, 4);
                }
                var nameOffsetInStrTab = BitConverter.ToUInt32(parser.FileData, (int)nameOffset + 8); // vernaux.vna_name
                string versionName = ELFParserUtils.ExtractStringFromBytes(strTabData, (int)nameOffsetInStrTab);
                
                // 存储版本定义
                ushort index = (ushort)(vd_ndx & 0x7fff); // 去除隐藏标志
                if (!parser.VersionDefinitions.ContainsKey(index))
                {
                    parser.VersionDefinitions.Add(index, versionName);
                }
                
                // 如果没有更多版本定义或者偏移量为0，则退出循环
                if (vd_next == 0 || offset + vd_next >= (ulong)parser.FileData.Length)
                {
                    break;
                }
                
                offset += vd_next; // 移动到下一个版本定义
                processed++;
            }
        }

        public static string GetFormattedVersionDefinitionInfo(ELFParser parser)
        {
            if (parser.VersionDefinitions == null || parser.VersionDefinitions.Count == 0)
            {
                return "";
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Version definition section '.gnu.version_d' contains 1 entries:");

            // 获取.gnu.version_d节的信息
            var verdefSection = parser.SectionHeaders?.Find(sh => sh.sh_type == (uint)SectionType.SHT_GNU_verdef);
            if (verdefSection != null)
            {
                int entryIndex = 0;
                foreach (var kvp in parser.VersionDefinitions.OrderBy(k => k.Key))
                {
                    string flags = kvp.Key == 1 ? "BASE" : "";
                    sb.AppendLine($"  地址：0x{(kvp.Key == 1 ? ((Models.ELFSectionHeader)verdefSection).sh_addr : ((Models.ELFSectionHeader)verdefSection).sh_addr + ((ulong)entryIndex * ((Models.ELFSectionHeader)verdefSection).sh_entsize)):x8}  Offset: 0x{(kvp.Key == 1 ? ((Models.ELFSectionHeader)verdefSection).sh_offset : ((Models.ELFSectionHeader)verdefSection).sh_offset + ((ulong)entryIndex * ((Models.ELFSectionHeader)verdefSection).sh_entsize)):x6}  Link: {((Models.ELFSectionHeader)verdefSection).sh_link} (.dynstr)  {entryIndex:D4}: Rev: 1  Flags: {flags,-6}   Index: {kvp.Key}  Cnt: 1  名称：{kvp.Value}");
                    entryIndex++;
                }
            }

            // 检查是否超出范围（根据用户提供的示例："Version definition past end of section"）
            if (verdefSection != null)
            {
                var verdefsCount = CalculateVerDefEntryCount((Models.ELFSectionHeader)verdefSection);
                if (parser.VersionDefinitions.Count > verdefsCount)
                {
                    sb.AppendLine("  Version definition past end of section");
                }
            }

            return sb.ToString();
        }
    }
}