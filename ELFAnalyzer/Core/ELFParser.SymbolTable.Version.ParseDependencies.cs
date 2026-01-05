using PersonalTools.ELFAnalyzer.Models;
using System.IO;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        private void ParseVersionDependencies()
        {
            // 查找版本依赖 (DT_VERNEED)
            long verneedAddr = 0;
            long verneedNum = 0;
            
            if (_is64Bit)
            {
                if (_dynamicEntries64 != null)
                {
                    foreach (var entry in _dynamicEntries64)
                    {
                        if (entry.d_tag == (long)DynamicTag.DT_VERNEED)
                        {
                            verneedAddr = (long)entry.d_val;
                        }
                        else if (entry.d_tag == (long)DynamicTag.DT_VERNEEDNUM)
                        {
                            verneedNum = (long)entry.d_val;
                        }
                    }
                }
            }
            else
            {
                if (_dynamicEntries32 != null)
                {
                    foreach (var entry in _dynamicEntries32)
                    {
                        if (entry.d_tag == (long)DynamicTag.DT_VERNEED)
                        {
                            verneedAddr = entry.d_val;
                        }
                        else if (entry.d_tag == (long)DynamicTag.DT_VERNEEDNUM)
                        {
                            verneedNum = entry.d_val;
                        }
                    }
                }
            }

            if (verneedAddr > 0 && verneedNum > 0)
            {
                _versionDependencies = [];
                
                if (_is64Bit)
                {
                    var verneedSection = FindSectionByAddress64((ulong)verneedAddr);
                    if (verneedSection != null)
                    {
                        ParseVerNeedEntries64(verneedSection.Value, (int)verneedNum);
                    }
                }
                else
                {
                    var verneedSection = FindSectionByAddress32((uint)verneedAddr);
                    if (verneedSection != null)
                    {
                        ParseVerNeedEntries32(verneedSection.Value, (int)verneedNum);
                    }
                }
            }
        }

        private ELFSectionHeader64? FindSectionByAddress64(ulong address)
        {
            if (_sectionHeaders64 == null) return null;
            
            foreach (var section in _sectionHeaders64)
            {
                if (section.sh_addr == address)
                {
                    return section;
                }
            }
            return null;
        }

        private ELFSectionHeader32? FindSectionByAddress32(uint address)
        {
            if (_sectionHeaders32 == null) return null;
            
            foreach (var section in _sectionHeaders32)
            {
                if (section.sh_addr == address)
                {
                    return section;
                }
            }
            return null;
        }

        private void ParseVerNeedEntries64(ELFSectionHeader64 section, int count)
        {
            if (_sectionHeaders64 == null || _versionDependencies == null) return;
            
            // 找到版本需求字符串表
            int strTabIdx = (int)section.sh_link;
            if (strTabIdx >= _sectionHeaders64.Count) return;
            
            var strTabSection = _sectionHeaders64[strTabIdx];
            var strTabData = new byte[strTabSection.sh_size];
            Array.Copy(_fileData, (long)strTabSection.sh_offset, strTabData, 0, (int)strTabSection.sh_size);
            
            long offset = (long)section.sh_offset;
            int processed = 0;
            
            while (processed < count && offset < _fileData.Length)
            {
                // 读取版本需求结构
                _ = BitConverter.ToUInt16(_fileData, (int)offset);
                var vn_cnt = BitConverter.ToUInt16(_fileData, (int)offset + 2);
                var vn_file = BitConverter.ToUInt32(_fileData, (int)offset + 4);
                var vn_aux = BitConverter.ToUInt32(_fileData, (int)offset + 8);
                var vn_next = BitConverter.ToUInt32(_fileData, (int)offset + 12);

                // 获取库名称
                _ = ExtractStringFromBytes(strTabData, (int)vn_file) ?? "unknown";

                long auxOffset = offset + vn_aux;
                int auxProcessed = 0;
                
                // 遍历辅助条目
                while (auxProcessed < vn_cnt && auxOffset < _fileData.Length)
                {
                    var nameOffset = BitConverter.ToUInt32(_fileData, (int)auxOffset + 8);
                    var flags = BitConverter.ToUInt16(_fileData, (int)auxOffset + 6);
                    var auxNext = BitConverter.ToUInt32(_fileData, (int)auxOffset + 12);
                    string versionName = ExtractStringFromBytes(strTabData, (int)nameOffset) ?? "unknown";
                    
                    // 使用版本索引作为键，而不是顺序
                    ushort verIndex = (ushort)(flags & 0x7fff); // 去除隐藏标志
                    if (!_versionDependencies.ContainsKey(verIndex))
                    {
                        _versionDependencies.Add(verIndex, versionName);
                    }
                    
                    auxProcessed++;
                    if (auxNext == 0) break;
                    auxOffset += auxNext;
                }
                
                offset += vn_next; // 移动到下一个版本需求
                processed++;
                
                if (vn_next == 0) break; // 没有更多版本需求
            }
        }

        private void ParseVerNeedEntries32(ELFSectionHeader32 section, int count)
        {
            if (_sectionHeaders32 == null || _versionDependencies == null) return;
            
            // 找到版本需求字符串表
            int strTabIdx = (int)section.sh_link;
            if (strTabIdx >= _sectionHeaders32.Count) return;
            
            var strTabSection = _sectionHeaders32[strTabIdx];
            var strTabData = new byte[strTabSection.sh_size];
            Array.Copy(_fileData, strTabSection.sh_offset, strTabData, 0, (int)strTabSection.sh_size);
            
            long offset = section.sh_offset;
            int processed = 0;
            
            while (processed < count && offset < _fileData.Length)
            {
                // 读取版本需求结构
                _ = BitConverter.ToUInt16(_fileData, (int)offset);
                var vn_cnt = BitConverter.ToUInt16(_fileData, (int)offset + 2);
                var vn_file = BitConverter.ToUInt32(_fileData, (int)offset + 4);
                var vn_aux = BitConverter.ToUInt32(_fileData, (int)offset + 8);
                var vn_next = BitConverter.ToUInt32(_fileData, (int)offset + 12);

                // 获取库名称
                _ = ExtractStringFromBytes(strTabData, (int)vn_file) ?? "unknown";

                long auxOffset = offset + vn_aux;
                int auxProcessed = 0;
                
                // 遍历辅助条目
                while (auxProcessed < vn_cnt && auxOffset < _fileData.Length)
                {
                    var nameOffset = BitConverter.ToUInt32(_fileData, (int)auxOffset + 8);
                    var flags = BitConverter.ToUInt16(_fileData, (int)auxOffset + 6);
                    var auxNext = BitConverter.ToUInt32(_fileData, (int)auxOffset + 12);
                    string versionName = ExtractStringFromBytes(strTabData, (int)nameOffset) ?? "unknown";
                    
                    // 使用版本索引作为键，而不是顺序
                    ushort verIndex = (ushort)(flags & 0x7fff); // 去除隐藏标志
                    if (!_versionDependencies.ContainsKey(verIndex))
                    {
                        _versionDependencies.Add(verIndex, versionName);
                    }
                    
                    auxProcessed++;
                    if (auxNext == 0) break;
                    auxOffset += auxNext;
                }
                
                offset += vn_next; // 移动到下一个版本需求
                processed++;
                
                if (vn_next == 0) break; // 没有更多版本需求
            }
        }
    }
}