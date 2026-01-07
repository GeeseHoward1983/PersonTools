using PersonalTools.ELFAnalyzer.Models;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        public string? GetSymbolName(ELFSymbol symbol, SectionType sectionType)
        {
            var linkedStrTabIdx32 = _linkedStrTabIdx.GetValueOrDefault(sectionType);
            if (_sectionHeaders == null || linkedStrTabIdx32 >= _sectionHeaders.Count) 
                return string.Empty;
            
            var strSection = _sectionHeaders[(int)linkedStrTabIdx32];
            if (strSection.sh_type != (uint)SectionType.SHT_STRTAB) 
                return string.Empty;
                
            var strData = new byte[strSection.sh_size];
            Array.Copy(_fileData, (int)strSection.sh_offset, strData, 0, (int)strData.Length);
            
            int offset = (int)symbol.st_name;
            if (offset >= strData.Length) return string.Empty;
            
            string baseName = ExtractStringFromBytes(strData, offset) ?? string.Empty;
            if(baseName.Length == 0) return string.Empty;
            // 如果符号表是动态符号表(SHT_DYNSYM)，尝试获取版本信息
            var symbols = Symbols.GetValueOrDefault(sectionType);
            if (symbols != null && _versionSymbols != null)
            {
                int symbolIndex = symbols.IndexOf(symbol);
                if (symbolIndex >= 0 && symbolIndex < _versionSymbols.Length)
                {
                    ushort versionIndex = (ushort)(_versionSymbols[symbolIndex] & 0x7fff); // 去除隐藏标志
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
            if (_symbols != null && _versionSymbols != null && 
                symbolIndex < _versionSymbols.Length && symbolIndex < _symbols.Count)
            {
                var symbol = _symbols[symbolIndex];
                var versionIndex = (ushort)(_versionSymbols[symbolIndex] & 0x7fff);
                
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
            if (_sectionHeaders == null || index >= _sectionHeaders.Count) return string.Empty;

            var strSection = _sectionHeaders[_header.e_shstrndx];
            var strData = new byte[strSection.sh_size];
            Array.Copy(_fileData, (int)strSection.sh_offset, strData, 0, (int)strData.Length);

            var section = _sectionHeaders[index];
            int offset = (int)section.sh_name;
            if (offset >= strData.Length) return string.Empty;

            return ExtractStringFromBytes(strData, offset);
        }
    }
}