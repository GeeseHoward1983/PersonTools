using PersonalTools.ELFAnalyzer.Models;
using System.IO;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        public string? GetSymbolName(ELFSymbol32 symbol, SectionType sectionType)
        {
            var linkedStrTabIdx32 = _linkedStrTabIdx32.GetValueOrDefault(sectionType);
            if (_sectionHeaders32 == null || linkedStrTabIdx32 >= _sectionHeaders32.Count) 
                return string.Empty;
            
            var strSection = _sectionHeaders32[(int)linkedStrTabIdx32];
            if (strSection.sh_type != (uint)SectionType.SHT_STRTAB) 
                return string.Empty;
                
            var strData = new byte[strSection.sh_size];
            Array.Copy(_fileData, (int)strSection.sh_offset, strData, 0, (int)strData.Length);
            
            int offset = (int)symbol.st_name;
            if (offset >= strData.Length) return string.Empty;
            
            string baseName = ExtractStringFromBytes(strData, offset) ?? string.Empty;
            if(baseName.Length == 0) return string.Empty;
            // 如果符号表是动态符号表(SHT_DYNSYM)，尝试获取版本信息
            var symbols32 = Symbols32.GetValueOrDefault(sectionType);
            if (symbols32 != null && _versionSymbols32 != null)
            {
                int symbolIndex = symbols32.IndexOf(symbol);
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

        public string? GetSymbolName(ELFSymbol64 symbol, SectionType sectionType)
        {
            var linkedStrTabIdx64 = _linkedStrTabIdx64.GetValueOrDefault(sectionType);

            if (_sectionHeaders64 == null || linkedStrTabIdx64 < 0 || linkedStrTabIdx64 >= _sectionHeaders64.Count) 
                return string.Empty;
            
            var strSection = _sectionHeaders64[(int)linkedStrTabIdx64];
            if (strSection.sh_type != (uint)SectionType.SHT_STRTAB) 
                return string.Empty;
                
            var strData = new byte[strSection.sh_size];
            Array.Copy(_fileData, (int)strSection.sh_offset, strData, 0, (int)strData.Length);
            
            int offset = (int)symbol.st_name;
            if (offset >= strData.Length) return string.Empty;
            
            string baseName = ExtractStringFromBytes(strData, offset) ?? string.Empty;
            
            if(baseName.Length == 0) return string.Empty;
            // 如果符号表是动态符号表(SHT_DYNSYM)，尝试获取版本信息
            var symbols64 = Symbols64.GetValueOrDefault(sectionType);
            if (symbols64 != null && _versionSymbols64 != null)
            {
                int symbolIndex = symbols64.IndexOf(symbol);
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
    }
}