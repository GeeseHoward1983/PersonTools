using PersonalTools.ELFAnalyzer.Models;
using System.IO;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        private void ParseVersionDefinitions()
        {
            // 查找版本定义 (DT_VERDEF)
            long verdefAddr = 0;
            long verdefNum = 0;
            
            if (_is64Bit)
            {
                if (_dynamicEntries64 != null)
                {
                    foreach (var entry in _dynamicEntries64)
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
            }
            else
            {
                if (_dynamicEntries32 != null)
                {
                    foreach (var entry in _dynamicEntries32)
                    {
                        if (entry.d_tag == (long)DynamicTag.DT_VERDEF)
                        {
                            verdefAddr = entry.d_val;
                        }
                        else if (entry.d_tag == (long)DynamicTag.DT_VERDEFNUM)
                        {
                            verdefNum = entry.d_val;
                        }
                    }
                }
            }

            if (verdefAddr > 0 && verdefNum > 0)
            {
                _versionDefinitions = [];
                
                if (_is64Bit)
                {
                    var verdefSection = FindSectionByAddress64((ulong)verdefAddr);
                    if (verdefSection != null)
                    {
                        ParseVerDefEntries64(verdefSection.Value, (int)verdefNum);
                    }
                }
                else
                {
                    var verdefSection = FindSectionByAddress32((uint)verdefAddr);
                    if (verdefSection != null)
                    {
                        ParseVerDefEntries32(verdefSection.Value, (int)verdefNum);
                    }
                }
            }
        }

        private void ParseVerDefEntries64(ELFSectionHeader64 section, int count)
        {
            if (_sectionHeaders64 == null || _versionDefinitions == null) return;
            
            // 找到版本定义字符串表
            int strTabIdx = (int)section.sh_link;
            if (strTabIdx >= _sectionHeaders64.Count) return;
            
            var strTabSection = _sectionHeaders64[strTabIdx];
            var strTabData = new byte[strTabSection.sh_size];
            Array.Copy(_fileData, (long)strTabSection.sh_offset, strTabData, 0, (int)strTabSection.sh_size);
            
            long offset = (long)section.sh_offset;
            int processed = 0;
            
            while (processed < count && offset < _fileData.Length)
            {
                // 读取版本定义结构
                _ = BitConverter.ToUInt16(_fileData, (int)offset);
                _ = BitConverter.ToUInt16(_fileData, (int)offset + 2);
                var vd_ndx = BitConverter.ToUInt16(_fileData, (int)offset + 4);
                _ = BitConverter.ToUInt16(_fileData, (int)offset + 6);
                _ = BitConverter.ToUInt32(_fileData, (int)offset + 8);
                var vd_aux = BitConverter.ToUInt32(_fileData, (int)offset + 12);
                var vd_next = BitConverter.ToUInt32(_fileData, (int)offset + 16);
                
                // 获取版本名称
                long nameOffset = offset + vd_aux;
                var nameOffsetInStrTab = BitConverter.ToUInt32(_fileData, (int)nameOffset + 8);
                string versionName = ExtractStringFromBytes(strTabData, (int)nameOffsetInStrTab) ?? "unknown";
                
                // 存储版本定义
                ushort index = (ushort)(vd_ndx & 0x7fff); // 去除隐藏标志
                if (!_versionDefinitions.ContainsKey(index))
                {
                    _versionDefinitions.Add(index, versionName);
                }
                
                offset += vd_next; // 移动到下一个版本定义
                processed++;
                
                if (vd_next == 0) break; // 没有更多版本定义
            }
        }

        private void ParseVerDefEntries32(ELFSectionHeader32 section, int count)
        {
            if (_sectionHeaders32 == null || _versionDefinitions == null) return;
            
            // 找到版本定义字符串表
            int strTabIdx = (int)section.sh_link;
            if (strTabIdx >= _sectionHeaders32.Count) return;
            
            var strTabSection = _sectionHeaders32[strTabIdx];
            var strTabData = new byte[strTabSection.sh_size];
            Array.Copy(_fileData, strTabSection.sh_offset, strTabData, 0, (int)strTabSection.sh_size);
            
            long offset = section.sh_offset;
            int processed = 0;
            
            while (processed < count && offset < _fileData.Length)
            {
                // 读取版本定义结构
                _ = BitConverter.ToUInt16(_fileData, (int)offset);
                _ = BitConverter.ToUInt16(_fileData, (int)offset + 2);
                var vd_ndx = BitConverter.ToUInt16(_fileData, (int)offset + 4);
                _ = BitConverter.ToUInt16(_fileData, (int)offset + 6);
                _ = BitConverter.ToUInt32(_fileData, (int)offset + 8);
                var vd_aux = BitConverter.ToUInt32(_fileData, (int)offset + 12);
                var vd_next = BitConverter.ToUInt32(_fileData, (int)offset + 16);
                
                // 获取版本名称
                long nameOffset = offset + vd_aux;
                var nameOffsetInStrTab = BitConverter.ToUInt32(_fileData, (int)nameOffset + 8);
                string versionName = ExtractStringFromBytes(strTabData, (int)nameOffsetInStrTab) ?? "unknown";
                
                // 存储版本定义
                ushort index = (ushort)(vd_ndx & 0x7fff); // 去除隐藏标志
                if (!_versionDefinitions.ContainsKey(index))
                {
                    _versionDefinitions.Add(index, versionName);
                }
                
                offset += vd_next; // 移动到下一个版本定义
                processed++;
                
                if (vd_next == 0) break; // 没有更多版本定义
            }
        }
    }
}