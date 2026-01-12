using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        private void ParseVersionDefinitions()
        {
            // 查找版本定义 (DT_VERDEF)
            long verdefAddr = 0;
            long verdefNum = 0;

            if (_dynamicEntries != null)
            {
                foreach (var entry in _dynamicEntries)
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
                _versionDefinitions = [];

                var verdefSection = FindSectionByAddress((ulong)verdefAddr);
                if (verdefSection != null)
                {
                    ParseVerDefEntries(verdefSection.Value, (int)verdefNum);
                }
            }
            else
            {
                // 如果动态段中没有找到版本定义信息，则直接查找SHT_GNU_VERDEF类型的节
                FindAndParseVersionDefinitionSection();
            }
        }

        private void FindAndParseVersionDefinitionSection()
        {
            if (_sectionHeaders == null || _versionDefinitions != null) return;

            // 确保_versionDefinitions字典存在
            if (_versionDefinitions == null)
            {
                _versionDefinitions = [];
            }

            // 遍历所有节头查找SHT_GNU_VERDEF类型的节（即.gnu.version_d）
            for (int i = 0; i < _sectionHeaders.Count; i++)
            {
                var section = _sectionHeaders[i];
                if (section.sh_type == (uint)SectionType.SHT_GNU_verdef)
                {
                    ParseVerDefEntries(section, CalculateVerDefEntryCount(section));
                }
            }
        }

        private static int CalculateVerDefEntryCount(Models.ELFSectionHeader section)
        {
            if (section.sh_entsize == 0) return 0;
            return (int)(section.sh_size / section.sh_entsize);
        }

        private void ParseVerDefEntries(Models.ELFSectionHeader section, int count)
        {
            if (_sectionHeaders == null || _versionDefinitions == null) return;
            
            // 找到版本定义字符串表
            int strTabIdx = (int)section.sh_link;
            if (strTabIdx >= _sectionHeaders.Count) return;
            
            var strTabSection = _sectionHeaders[strTabIdx];
            var strTabData = new byte[strTabSection.sh_size];
            Array.Copy(_fileData, (long)strTabSection.sh_offset, strTabData, 0, (int)strTabSection.sh_size);
            
            ulong offset = section.sh_offset;
            int processed = 0;
            
            while (processed < count && offset < (ulong)_fileData.Length)
            {          
                var vd_version = BitConverter.ToUInt16(_fileData, (int)offset);
                var vd_flags = BitConverter.ToUInt16(_fileData, (int)offset + 2);
                var vd_ndx = BitConverter.ToUInt16(_fileData, (int)offset + 4);
                var vd_cnt = BitConverter.ToUInt16(_fileData, (int)offset + 6);
                // Skip vd_hash at offset+8

                var vd_aux = _is64Bit ? BitConverter.ToUInt64(_fileData, (int)offset + 8) : BitConverter.ToUInt32(_fileData, (int)offset + 12);  // 64位的vd_aux和vd_next
                var vd_next = _is64Bit ? BitConverter.ToUInt64(_fileData, (int)offset + 16) : BitConverter.ToUInt32(_fileData, (int)offset + 16);

                // 获取版本名称
                // vernaux 结构紧跟在 verdef 结构之后
                ulong nameOffset = offset + vd_aux;
                var nameOffsetInStrTab = BitConverter.ToUInt32(_fileData, (int)nameOffset + 8); // vernaux.vna_name
                string versionName = ELFParserUtils.ExtractStringFromBytes(strTabData, (int)nameOffsetInStrTab);
                
                // 存储版本定义
                ushort index = (ushort)(vd_ndx & 0x7fff); // 去除隐藏标志
                if (!_versionDefinitions.ContainsKey(index))
                {
                    _versionDefinitions.Add(index, versionName);
                }
                
                // 如果没有更多版本定义或者偏移量为0，则退出循环
                if (vd_next == 0 || offset + vd_next >= (ulong)_fileData.Length)
                {
                    break;
                }
                
                offset += vd_next; // 移动到下一个版本定义
                processed++;
            }
        }

        public string GetFormattedVersionDefinitionInfo()
        {
            if (_versionDefinitions == null || _versionDefinitions.Count == 0)
            {
                return "未找到版本定义信息";
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Version definition section '.gnu.version_d' contains 1 entries:");

            // 获取.gnu.version_d节的信息
            var verdefSection = _sectionHeaders?.Find(sh => sh.sh_type == (uint)SectionType.SHT_GNU_verdef);
            if (verdefSection != null)
            {
                int entryIndex = 0;
                foreach (var kvp in _versionDefinitions.OrderBy(k => k.Key))
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
                if (_versionDefinitions.Count > verdefsCount)
                {
                    sb.AppendLine("  Version definition past end of section");
                }
            }

            return sb.ToString();
        }
    }
}