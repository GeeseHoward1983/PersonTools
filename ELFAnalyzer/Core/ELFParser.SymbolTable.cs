using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MyTool.ELFAnalyzer.Models;

namespace MyTool.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        private void ReadSymbolTables(BinaryReader reader)
        {
            if (_is64Bit)
            {
                for (int i = 0; i < _sectionHeaders64?.Count; i++)
                {
                    var section = _sectionHeaders64[i];
                    if (section.sh_type == (uint)SectionType.SHT_SYMTAB || section.sh_type == (uint)SectionType.SHT_DYNSYM)
                    {
                        reader.BaseStream.Seek((long)section.sh_offset, SeekOrigin.Begin);
                        
                        int symbolCount = (int)(section.sh_size / section.sh_entsize);
                        _symbols64 = new List<ELFSymbol64>(symbolCount);
                        
                        for (int j = 0; j < symbolCount; j++)
                        {
                            var symbol = new ELFSymbol64
                            {
                                st_name = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                                st_info = reader.ReadByte(),
                                st_other = reader.ReadByte(),
                                st_shndx = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt16LE(reader) : ReadUInt16BE(reader),
                                st_value = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader),
                                st_size = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt64LE(reader) : ReadUInt64BE(reader)
                            };
                            _symbols64.Add(symbol);
                        }
                        
                        // 记录符号表关联的字符串表索引
                        _linkedStrTabIdx64 = (int)section.sh_link;
                    }
                }
            }
            else
            {
                for (int i = 0; i < _sectionHeaders32?.Count; i++)
                {
                    var section = _sectionHeaders32[i];
                    if (section.sh_type == (uint)SectionType.SHT_SYMTAB || section.sh_type == (uint)SectionType.SHT_DYNSYM)
                    {
                        reader.BaseStream.Seek(section.sh_offset, SeekOrigin.Begin);
                        
                        int symbolCount = (int)(section.sh_size / section.sh_entsize);
                        _symbols32 = new List<ELFSymbol32>(symbolCount);
                        
                        for (int j = 0; j < symbolCount; j++)
                        {
                            var symbol = new ELFSymbol32
                            {
                                st_name = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                                st_value = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                                st_size = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt32LE(reader) : ReadUInt32BE(reader),
                                st_info = reader.ReadByte(),
                                st_other = reader.ReadByte(),
                                st_shndx = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? ReadUInt16LE(reader) : ReadUInt16BE(reader)
                            };
                            _symbols32.Add(symbol);
                        }
                        
                        // 记录符号表关联的字符串表索引
                        _linkedStrTabIdx32 = (int)section.sh_link;
                    }
                }
            }
        }

        // 添加字段记录符号表关联的字符串表索引
        private int _linkedStrTabIdx32 = -1;
        private int _linkedStrTabIdx64 = -1;

        // 添加版本符号信息存储
        private ushort[]? _versionSymbols32;
        private ushort[]? _versionSymbols64;
        private Dictionary<ushort, string>? _versionDefinitions;
        private Dictionary<ushort, string>? _versionDependencies;

        // 解析版本信息
        private void ReadVersionInformation()
        {
            // 初始化版本定义和依赖字典
            _versionDefinitions = [];
            _versionDependencies = [];
            
            // 解析版本符号表
            ParseVersionSymbolTable();
            
            // 解析版本定义
            ParseVersionDefinitions();
            
            // 解析版本需求
            ParseVersionDependencies();
        }

        private void ParseVersionSymbolTable()
        {
            // 查找版本符号表 (DT_VERSYM)
            long versymAddr = 0;
            
            if (_is64Bit)
            {
                if (_dynamicEntries64 != null)
                {
                    foreach (var entry in _dynamicEntries64)
                    {
                        if (entry.d_tag == (long)DynamicTag.DT_VERSYM)
                        {
                            versymAddr = (long)entry.d_val;
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
                        if (entry.d_tag == (long)DynamicTag.DT_VERSYM)
                        {
                            versymAddr = entry.d_val;
                        }
                    }
                }
            }

            // 查找对应的节头
            if (versymAddr > 0)
            {
                if (_is64Bit)
                {
                    var versymSection = FindSectionByAddress64((ulong)versymAddr);
                    if (versymSection != null)
                    {
                        var data = new byte[versymSection.Value.sh_size];
                        Array.Copy(_fileData, (long)versymSection.Value.sh_offset, data, 0, (int)versymSection.Value.sh_size);
                        
                        int count = (int)(versymSection.Value.sh_size / 2); // 每个版本符号是2字节
                        _versionSymbols64 = new ushort[count];
                        
                        for (int i = 0; i < count; i++)
                        {
                            _versionSymbols64[i] = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? 
                                BitConverter.ToUInt16(data, i * 2) : 
                                (ushort)((data[i * 2 + 1] << 8) | data[i * 2]);
                        }
                    }
                }
                else
                {
                    var versymSection = FindSectionByAddress32((uint)versymAddr);
                    if (versymSection != null)
                    {
                        var data = new byte[versymSection.Value.sh_size];
                        Array.Copy(_fileData, versymSection.Value.sh_offset, data, 0, (int)versymSection.Value.sh_size);
                        
                        int count = (int)(versymSection.Value.sh_size / 2); // 每个版本符号是2字节
                        _versionSymbols32 = new ushort[count];
                        
                        for (int i = 0; i < count; i++)
                        {
                            _versionSymbols32[i] = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? 
                                BitConverter.ToUInt16(data, i * 2) : 
                                (ushort)((data[i * 2 + 1] << 8) | data[i * 2]);
                        }
                    }
                }
            }
        }

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

        public string? GetSymbolName(ELFSymbol32 symbol)
        {
            if (_sectionHeaders32 == null || _linkedStrTabIdx32 < 0 || _linkedStrTabIdx32 >= _sectionHeaders32.Count) 
                return string.Empty;
            
            var strSection = _sectionHeaders32[_linkedStrTabIdx32];
            if (strSection.sh_type != (uint)SectionType.SHT_STRTAB) 
                return string.Empty;
                
            var strData = new byte[strSection.sh_size];
            Array.Copy(_fileData, (int)strSection.sh_offset, strData, 0, (int)strData.Length);
            
            int offset = (int)symbol.st_name;
            if (offset >= strData.Length) return string.Empty;
            
            string baseName = ExtractStringFromBytes(strData, offset) ?? string.Empty;
            
            // 如果符号表是动态符号表(SHT_DYNSYM)，尝试获取版本信息
            if (_symbols32 != null && _versionSymbols32 != null)
            {
                int symbolIndex = _symbols32.IndexOf(symbol);
                if (symbolIndex >= 0 && symbolIndex < _versionSymbols32.Length)
                {
                    ushort versionIndex = (ushort)(_versionSymbols32[symbolIndex] & 0x7fff); // 去除隐藏标志
                    if (versionIndex >= 2) // 版本索引从2开始是有效的外部版本
                    {
                        string? versionName = GetVersionNameByVersionIndex(versionIndex);
                        if (!string.IsNullOrEmpty(versionName))
                        {
                            // 判断是否是全局符号 (@@ 表示默认版本)
                            // 如果是第一个定义的符号则使用 @@，否则使用 @
                            bool isDefaultVersion = IsDefaultVersionSymbol(symbolIndex);
                            return baseName + (isDefaultVersion ? "@@" : "@") + versionName;
                        }
                    }
                }
            }
            
            return baseName;
        }

        public string? GetSymbolName(ELFSymbol64 symbol)
        {
            if (_sectionHeaders64 == null || _linkedStrTabIdx64 < 0 || _linkedStrTabIdx64 >= _sectionHeaders64.Count) 
                return string.Empty;
            
            var strSection = _sectionHeaders64[_linkedStrTabIdx64];
            if (strSection.sh_type != (uint)SectionType.SHT_STRTAB) 
                return string.Empty;
                
            var strData = new byte[strSection.sh_size];
            Array.Copy(_fileData, (int)strSection.sh_offset, strData, 0, (int)strData.Length);
            
            int offset = (int)symbol.st_name;
            if (offset >= strData.Length) return string.Empty;
            
            string baseName = ExtractStringFromBytes(strData, offset) ?? string.Empty;
            
            // 如果符号表是动态符号表(SHT_DYNSYM)，尝试获取版本信息
            if (_symbols64 != null && _versionSymbols64 != null)
            {
                int symbolIndex = _symbols64.IndexOf(symbol);
                if (symbolIndex >= 0 && symbolIndex < _versionSymbols64.Length)
                {
                    ushort versionIndex = (ushort)(_versionSymbols64[symbolIndex] & 0x7fff); // 去除隐藏标志
                    if (versionIndex >= 2) // 版本索引从2开始是有效的外部版本
                    {
                        string? versionName = GetVersionNameByVersionIndex(versionIndex);
                        if (!string.IsNullOrEmpty(versionName))
                        {
                            // 判断是否是全局符号 (@@ 表示默认版本)
                            // 如果是第一个定义的符号则使用 @@，否则使用 @  
                            bool isDefaultVersion = IsDefaultVersionSymbol(symbolIndex);
                            return baseName + (isDefaultVersion ? "@@" : "@") + versionName;
                        }
                    }
                }
            }
            
            return baseName;
        }

        private string? GetVersionNameByVersionIndex(ushort versionIndex)
        {
            // 这里需要根据版本索引查找版本名称
            // 实现简化，实际应用中需要从 .gnu.version_d 或 .gnu.version_n 节中解析
            if (_versionDefinitions != null && _versionDefinitions.ContainsKey(versionIndex))
            {
                return _versionDefinitions[versionIndex];
            }
            
            if (_versionDependencies != null && _versionDependencies.ContainsKey(versionIndex))
            {
                return _versionDependencies[versionIndex];
            }
            
            // 通常版本索引2开始代表外部版本，可以尝试从动态段中查找
            // 简化实现：返回版本索引作为名称
            return $"VER_{versionIndex}";
        }

        private bool IsDefaultVersionSymbol(int symbolIndex)
        {
            // 简化实现：对于动态符号，根据符号绑定类型判断
            // 全局符号且版本号最低的为默认版本
            if (_is64Bit && _symbols64 != null && _versionSymbols64 != null && 
                symbolIndex < _versionSymbols64.Length && symbolIndex < _symbols64.Count)
            {
                var symbol = _symbols64[symbolIndex];
                var versionIndex = (ushort)(_versionSymbols64[symbolIndex] & 0x7fff);
                
                // 检查是否是全局符号
                byte binding = (byte)(symbol.st_info >> 4);
                if (binding == (byte)SymbolBinding.STB_GLOBAL || binding == (byte)SymbolBinding.STB_WEAK)
                {
                    // 简化处理：如果符号版本号是某个特定值，则认为是默认版本
                    return versionIndex == 1; // 版本1通常是默认版本
                }
            }
            else if (!_is64Bit && _symbols32 != null && _versionSymbols32 != null && 
                     symbolIndex < _versionSymbols32.Length && symbolIndex < _symbols32.Count)
            {
                var symbol = _symbols32[symbolIndex];
                var versionIndex = (ushort)(_versionSymbols32[symbolIndex] & 0x7fff);
                
                // 检查是否是全局符号
                byte binding = (byte)(symbol.st_info >> 4);
                if (binding == (byte)SymbolBinding.STB_GLOBAL || binding == (byte)SymbolBinding.STB_WEAK)
                {
                    // 简化处理：如果符号版本号是某个特定值，则认为是默认版本
                    return versionIndex == 1; // 版本1通常是默认版本
                }
            }
            
            return false;
        }

        public string? GetSectionName(int index)
        {
            if (_is64Bit)
            {
                if (_sectionHeaders64 == null || index >= _sectionHeaders64.Count) return string.Empty;
                
                var strSection = _sectionHeaders64[_header.e_shstrndx];
                var strData = new byte[strSection.sh_size];
                Array.Copy(_fileData, (int)strSection.sh_offset, strData, 0, (int)strData.Length);
                
                var section = _sectionHeaders64[index];
                int offset = (int)section.sh_name;
                if (offset >= strData.Length) return string.Empty;
                
                return ExtractStringFromBytes(strData, offset);
            }
            else
            {
                if (_sectionHeaders32 == null || index >= _sectionHeaders32.Count) return string.Empty;
                
                var strSection = _sectionHeaders32[_header.e_shstrndx];
                var strData = new byte[strSection.sh_size];
                Array.Copy(_fileData, (int)strSection.sh_offset, strData, 0, (int)strData.Length);
                
                var section = _sectionHeaders32[index];
                int offset = (int)section.sh_name;
                if (offset >= strData.Length) return string.Empty;
                
                return ExtractStringFromBytes(strData, offset);
            }
        }

        private static string? ExtractStringFromBytes(byte[] data, int startOffset)
        {
            int endOffset = startOffset;
            while (endOffset < data.Length && data[endOffset] != 0)
            {
                endOffset++;
            }

            if (endOffset > startOffset)
            {
                return Encoding.UTF8.GetString(data, startOffset, endOffset - startOffset);
            }
            return string.Empty;
        }

        public string GetFormattedVersionSymbolInfo()
        {
            var sb = new StringBuilder();
            
            // 首先检查是否存在版本符号表
            ELFSectionHeader32? verSymSection32 = null;
            ELFSectionHeader64? verSymSection64 = null;
            
            if (_is64Bit)
            {
                if (_sectionHeaders64 != null)
                {
                    foreach (var section in _sectionHeaders64)
                    {
                        string sectionName = GetSectionName(_sectionHeaders64.IndexOf(section)) ?? string.Empty;
                        if (sectionName == ".gnu.version" || sectionName == ".gnu.version_r")
                        {
                            if (sectionName == ".gnu.version" && _versionSymbols64 != null)
                            {
                                verSymSection64 = section;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                if (_sectionHeaders32 != null)
                {
                    foreach (var section in _sectionHeaders32)
                    {
                        string sectionName = GetSectionName(_sectionHeaders32.IndexOf(section)) ?? string.Empty;
                        if (sectionName == ".gnu.version" || sectionName == ".gnu.version_r")
                        {
                            if (sectionName == ".gnu.version" && _versionSymbols32 != null)
                            {
                                verSymSection32 = section;
                                break;
                            }
                        }
                    }
                }
            }

            if ((_is64Bit && verSymSection64 != null) || (!_is64Bit && verSymSection32 != null))
            {
                if (_is64Bit)
                {
                    if (verSymSection64 != null && _versionSymbols64 != null && _symbols64 != null)
                    {
                        int entryCount = _versionSymbols64.Length;
                        sb.AppendLine($"Version symbols section '.gnu.version' contains {entryCount} entries:");
                        sb.AppendLine($"  Addr: 0x{verSymSection64.Value.sh_addr:x16}  Offset: 0x{verSymSection64.Value.sh_offset:x6}  Link: {verSymSection64.Value.sh_link} (.dynsym)");
                        
                        // 每行显示4个版本符号
                        for (int i = 0; i < _versionSymbols64.Length; i += 4)
                        {
                            sb.Append($" {i:x3}:");
                            for (int j = 0; j < 4 && (i + j) < _versionSymbols64.Length; j++)
                            {
                                ushort versionIndex = (ushort)(_versionSymbols64[i + j] & 0x7fff);
                                string versionInfo = GetVersionInfoByIndex(versionIndex);
                                sb.Append($" {versionIndex:D3} ({versionInfo})");
                            }
                            sb.AppendLine();
                        }
                    }
                }
                else
                {
                    if (verSymSection32 != null && _versionSymbols32 != null && _symbols32 != null)
                    {
                        int entryCount = _versionSymbols32.Length;
                        sb.AppendLine($"Version symbols section '.gnu.version' contains {entryCount} entries:");
                        sb.AppendLine($"  地址: 0x{verSymSection32.Value.sh_addr:x8}  Offset: 0x{verSymSection32.Value.sh_offset:x6}  Link: {verSymSection32.Value.sh_link} (.dynsym)");
                        
                        // 每行显示4个版本符号
                        for (int i = 0; i < _versionSymbols32.Length; i += 4)
                        {
                            sb.Append($" {i:x3}:");
                            for (int j = 0; j < 4 && (i + j) < _versionSymbols32.Length; j++)
                            {
                                ushort versionIndex = (ushort)(_versionSymbols32[i + j] & 0x7fff);
                                string versionInfo = GetVersionInfoByIndex(versionIndex);
                                sb.Append($" {versionIndex:D3} ({versionInfo})");
                            }
                            sb.AppendLine();
                        }
                    }
                }
            }
            else
            {
                sb.AppendLine("Version symbols section '.gnu.version' not found or empty.");
            }
            
            return sb.ToString();
        }

        public string GetFormattedVersionDependencyInfo()
        {
            var sb = new StringBuilder();
            
            // 检查是否存在版本需求表
            ELFSectionHeader32? verNeedSection32 = null;
            ELFSectionHeader64? verNeedSection64 = null;
            
            if (_is64Bit)
            {
                if (_sectionHeaders64 != null)
                {
                    foreach (var section in _sectionHeaders64)
                    {
                        string sectionName = GetSectionName(_sectionHeaders64.IndexOf(section)) ?? string.Empty;
                        if (sectionName == ".gnu.version_r")
                        {
                            verNeedSection64 = section;
                            break;
                        }
                    }
                }
            }
            else
            {
                if (_sectionHeaders32 != null)
                {
                    foreach (var section in _sectionHeaders32)
                    {
                        string sectionName = GetSectionName(_sectionHeaders32.IndexOf(section)) ?? string.Empty;
                        if (sectionName == ".gnu.version_r")
                        {
                            verNeedSection32 = section;
                            break;
                        }
                    }
                }
            }

            if ((_is64Bit && verNeedSection64 != null) || (!_is64Bit && verNeedSection32 != null))
            {
                if (_is64Bit)
                {
                    if (verNeedSection64 != null && _versionDependencies != null)
                    {
                        int entryCount = _versionDependencies.Count;
                        sb.AppendLine($"Version needs section '.gnu.version_r' contains {entryCount} entries:");
                        sb.AppendLine($"  地址: 0x{verNeedSection64.Value.sh_addr:x16}  Offset: 0x{verNeedSection64.Value.sh_offset:x6}  Link: {verNeedSection64.Value.sh_link} (.dynstr)");
                        
                        // 这里需要实际解析版本需求表的内容
                        ParseAndAppendVersionNeeds64(verNeedSection64.Value, sb);
                    }
                }
                else
                {
                    if (verNeedSection32 != null && _versionDependencies != null)
                    {
                        int entryCount = _versionDependencies.Count;
                        sb.AppendLine($"Version needs section '.gnu.version_r' contains {entryCount} entries:");
                        sb.AppendLine($"  地址: 0x{verNeedSection32.Value.sh_addr:x8}  Offset: 0x{verNeedSection32.Value.sh_offset:x6}  Link: {verNeedSection32.Value.sh_link} (.dynstr)");
                        
                        // 这里需要实际解析版本需求表的内容
                        ParseAndAppendVersionNeeds32(verNeedSection32.Value, sb);
                    }
                }
            }
            else
            {
                sb.AppendLine("Version needs section '.gnu.version_r' not found or empty.");
            }
            
            return sb.ToString();
        }
        
        private string GetVersionInfoByIndex(ushort versionIndex)
        {
            if (versionIndex == 0) return "*本地*";
            if (versionIndex == 1) return "*全局*";
            
            if (_versionDefinitions != null && _versionDefinitions.ContainsKey(versionIndex))
            {
                return _versionDefinitions[versionIndex];
            }
            
            if (_versionDependencies != null && _versionDependencies.ContainsKey(versionIndex))
            {
                return _versionDependencies[versionIndex];
            }
            
            return $"VER_{versionIndex}";
        }
        
        private void ParseAndAppendVersionNeeds64(ELFSectionHeader64 section, StringBuilder sb)
        {
            if (_sectionHeaders64 == null) return;
            
            // 找到版本需求字符串表
            int strTabIdx = (int)section.sh_link;
            if (strTabIdx >= _sectionHeaders64.Count) return;
            
            var strTabSection = _sectionHeaders64[strTabIdx];
            var strTabData = new byte[strTabSection.sh_size];
            Array.Copy(_fileData, (long)strTabSection.sh_offset, strTabData, 0, (int)strTabSection.sh_size);
            
            long offset = (long)section.sh_offset;
            int processed = 0;
            
            // 遍历所有版本需求项（每个项代表一个库）
            while (offset < _fileData.Length)
            {
                // 读取版本需求结构
                var vn_version = BitConverter.ToUInt16(_fileData, (int)offset);
                var vn_cnt = BitConverter.ToUInt16(_fileData, (int)offset + 2);
                var vn_file = BitConverter.ToUInt32(_fileData, (int)offset + 4);
                var vn_aux = BitConverter.ToUInt32(_fileData, (int)offset + 8);
                var vn_next = BitConverter.ToUInt32(_fileData, (int)offset + 12);

                // 获取库名称
                string libName = ExtractStringFromBytes(strTabData, (int)vn_file) ?? "unknown";

                // 计算该库的版本依赖数量
                int versionCount = vn_cnt;
                
                sb.AppendLine($"  000000: 版本: {vn_version}  文件: {libName}  计数: {versionCount}");
                
                long auxOffset = offset + vn_aux;
                int auxProcessed = 0;
                
                // 遍历该库的所有版本依赖
                while (auxProcessed < vn_cnt && auxOffset < _fileData.Length)
                {
                    var nameOffset = BitConverter.ToUInt32(_fileData, (int)auxOffset + 8);
                    var flags = BitConverter.ToUInt16(_fileData, (int)auxOffset + 6);
                    var auxNext = BitConverter.ToUInt32(_fileData, (int)auxOffset + 12);
                    string versionName = ExtractStringFromBytes(strTabData, (int)nameOffset) ?? "unknown";
                    
                    // 使用版本索引作为键来获取版本信息
                    ushort verIndex = (ushort)(flags & 0x7fff); // 去除隐藏标志
                    string actualVersionName = GetVersionInfoByIndex(verIndex);
                    
                    sb.AppendLine($"  0x{0x10 * (auxProcessed + 1):x4}: 名称: {actualVersionName}  标志: {((flags & 0x1) != 0x00 ? "BASE" : "none")}  版本: {verIndex}");
                    
                    auxProcessed++;
                    if (auxNext == 0) break;
                    auxOffset += auxNext;
                }
                
                processed++;
                if (vn_next == 0) break; // 没有更多版本需求
                offset += vn_next;
            }
            
            if (processed == 0)
            {
                sb.AppendLine("  No version dependencies found.");
            }
        }
        
        private void ParseAndAppendVersionNeeds32(ELFSectionHeader32 section, StringBuilder sb)
        {
            if (_sectionHeaders32 == null) return;
            
            // 找到版本需求字符串表
            int strTabIdx = (int)section.sh_link;
            if (strTabIdx >= _sectionHeaders32.Count) return;
            
            var strTabSection = _sectionHeaders32[strTabIdx];
            var strTabData = new byte[strTabSection.sh_size];
            Array.Copy(_fileData, strTabSection.sh_offset, strTabData, 0, (int)strTabSection.sh_size);
            
            long offset = section.sh_offset;
            int processed = 0;
            
            // 遍历所有版本需求项（每个项代表一个库）
            while (offset < _fileData.Length)
            {
                // 读取版本需求结构
                var vn_version = BitConverter.ToUInt16(_fileData, (int)offset);
                var vn_cnt = BitConverter.ToUInt16(_fileData, (int)offset + 2);
                var vn_file = BitConverter.ToUInt32(_fileData, (int)offset + 4);
                var vn_aux = BitConverter.ToUInt32(_fileData, (int)offset + 8);
                var vn_next = BitConverter.ToUInt32(_fileData, (int)offset + 12);

                // 获取库名称
                string libName = ExtractStringFromBytes(strTabData, (int)vn_file) ?? "unknown";

                // 计算该库的版本依赖数量
                int versionCount = vn_cnt;
                
                sb.AppendLine($"  000000: Version: {vn_version}  File: {libName}  Count: {versionCount}");
                
                long auxOffset = offset + vn_aux;
                int auxProcessed = 0;
                
                // 遍历该库的所有版本依赖
                while (auxProcessed < vn_cnt && auxOffset < _fileData.Length)
                {
                    var nameOffset = BitConverter.ToUInt32(_fileData, (int)auxOffset + 8);
                    var flags = BitConverter.ToUInt16(_fileData, (int)auxOffset + 6);
                    var auxNext = BitConverter.ToUInt32(_fileData, (int)auxOffset + 12);
                    string versionName = ExtractStringFromBytes(strTabData, (int)nameOffset) ?? "unknown";
                    
                    // 使用版本索引作为键来获取版本信息
                    ushort verIndex = (ushort)(flags & 0x7fff); // 去除隐藏标志
                    string actualVersionName = GetVersionInfoByIndex(verIndex);
                    
                    sb.AppendLine($"  0x{0x10 * (auxProcessed + 1):x4}: Name: {actualVersionName}  Flags: {((flags & 0x1) != 0x00 ? "BASE" : "none")}  Version: {verIndex}");
                    
                    auxProcessed++;
                    if (auxNext == 0) break;
                    auxOffset += auxNext;
                }
                
                processed++;
                if (vn_next == 0) break; // 没有更多版本需求
                offset += vn_next;
            }
            
            if (processed == 0)
            {
                sb.AppendLine("  No version dependencies found.");
            }
        }

        public static string? GetSymbolType(byte stInfo)
        {
            byte type = (byte)(stInfo & 0x0F);
            if (Enum.IsDefined(typeof(SymbolType), type))
            {
                return Enum.GetName(typeof(SymbolType), type)?.Replace("STT_", "");
            }
            return "UNKNOWN";
        }

        public static string? GetSymbolBinding(byte stInfo)
        {
            byte binding = (byte)(stInfo >> 4);
            if (Enum.IsDefined(typeof(SymbolBinding), binding))
            {
                return Enum.GetName(typeof(SymbolBinding), binding)?.Replace("STB_", "");
            }
            return "UNKNOWN";
        }

        public static string? GetSymbolVisibility(byte stOther)
        {
            byte visibility = (byte)(stOther & 0x03);
            if (Enum.IsDefined(typeof(SymbolVisibility), visibility))
            {
                return Enum.GetName(typeof(SymbolVisibility), visibility)?.Replace("STV_", "");
            }
            return "UNKNOWN";
        }
    }
}