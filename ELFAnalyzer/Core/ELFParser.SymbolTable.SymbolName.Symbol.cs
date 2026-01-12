using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    public class SymbleName
    {
        public static string GetSymbolName(ELFParser parser, ELFSymbol symbol, SectionType sectionType)
        {
            var linkedStrTabIdx32 = parser.LinkedStrTabIdx.GetValueOrDefault(sectionType);
            if (parser.SectionHeaders == null || linkedStrTabIdx32 >= parser.SectionHeaders.Count) 
                return string.Empty;
            
            var strSection = parser.SectionHeaders[(int)linkedStrTabIdx32];
            if (strSection.sh_type != (uint)SectionType.SHT_STRTAB) 
                return string.Empty;
                
            var strData = new byte[strSection.sh_size];
            Array.Copy(parser.FileData, (int)strSection.sh_offset, strData, 0, (int)strData.Length);
            
            int offset = (int)symbol.st_name;
            if (offset >= strData.Length) return string.Empty;
            
            string baseName = ELFParserUtils.ExtractStringFromBytes(strData, offset);
            if(baseName.Length == 0) return string.Empty;
            // 如果符号表是动态符号表(SHT_DYNSYM)，尝试获取版本信息
            var symbols = parser.Symbols.GetValueOrDefault(sectionType);
            if (symbols != null && parser.VersionSymbols != null)
            {
                int symbolIndex = symbols.IndexOf(symbol);
                if (symbolIndex >= 0 && symbolIndex < parser.VersionSymbols.Length)
                {
                    ushort versionIndex = (ushort)(parser.VersionSymbols[symbolIndex] & 0x7fff); // 去除隐藏标志
                    if (versionIndex >= 2) // 版本索引从2开始是有效的外部版本
                    {
                        string versionName = GetVersionNameByVersionIndex(parser, versionIndex);
                        if (!string.IsNullOrEmpty(versionName))
                        {
                            // 判断是否是全局符号 (@@ 表示默认版本)
                            // 如果是第一个定义的符号则使用 @@，否则使用 @
                            bool isDefaultVersion = IsDefaultVersionSymbol(parser, symbolIndex, symbols);
                            return baseName + (isDefaultVersion ? "@@" : "@") + versionName;
                        }
                    }
                }
            }
            
            return baseName;
        }

        private static string GetVersionNameByVersionIndex(ELFParser parser, ushort versionIndex)
        {
            // 这里需要根据版本索引查找版本名称
            // 实现简化，实际应用中需要从 .gnu.version_d 或 .gnu.version_n 节中解析
            if (parser.VersionDefinitions != null && parser.VersionDefinitions.ContainsKey(versionIndex))
            {
                return parser.VersionDefinitions[versionIndex];
            }
            
            if (parser.VersionDependencies != null && parser.VersionDependencies.ContainsKey(versionIndex))
            {
                return parser.VersionDependencies[versionIndex];
            }
            
            // 通常版本索引2开始代表外部版本，可以尝试从动态段中查找
            // 简化实现：返回版本索引作为名称
            return $"VER_{versionIndex}";
        }

        private static bool IsDefaultVersionSymbol(ELFParser parser, int symbolIndex, List<ELFSymbol> symbols)
        {
            // 简化实现：对于动态符号，根据符号绑定类型判断
            // 全局符号且版本号最低的为默认版本
            if (parser.Symbols != null && parser.VersionSymbols != null && 
                symbolIndex < parser.VersionSymbols.Length && symbolIndex < parser.Symbols.Count)
            {
                var symbol = symbols[symbolIndex];
                var versionIndex = (ushort)(parser.VersionSymbols[symbolIndex] & 0x7fff);
                
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

        public static string GetSectionName(ELFParser parser,int index)
        {
            if (parser.SectionHeaders == null || index >= parser.SectionHeaders.Count) return string.Empty;

            var strSection = parser.SectionHeaders[parser.Header.e_shstrndx];
            var strData = new byte[strSection.sh_size];
            Array.Copy(parser.FileData, (int)strSection.sh_offset, strData, 0, (int)strData.Length);

            var section = parser.SectionHeaders[index];
            int offset = (int)section.sh_name;
            if (offset >= strData.Length) return string.Empty;

            return ELFParserUtils.ExtractStringFromBytes(strData, offset);
        }
    }
}