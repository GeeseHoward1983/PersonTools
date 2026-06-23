using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFSymbolNameResolver
    {
        internal static string GetSymbolName(ELFParser parser, ELFSymbol symbol, SectionType sectionType, int symbolIndex)
        {
            uint linkedStrTabIdx32 = parser.LinkedStrTabIdx.GetValueOrDefault(sectionType);
            if (parser.SectionHeaders == null || linkedStrTabIdx32 >= parser.SectionHeaders.Count)
            {
                return string.Empty;
            }

            Models.ELFSectionHeader strSection = parser.SectionHeaders[(int)linkedStrTabIdx32];
            if (strSection.sh_type != (uint)SectionType.SHT_STRTAB)
            {
                return string.Empty;
            }

            byte[] strData = parser.GetSectionData((int)linkedStrTabIdx32);

            int offset = (int)symbol.StName;
            if (offset >= strData.Length)
            {
                return string.Empty;
            }

            string baseName = ELFParserUtils.ExtractStringFromBytes(strData, offset);
            if (baseName.Length == 0)
            {
                return string.Empty;
            }

            // 动态符号表(SHT_DYNSYM)可能带版本：追加 @/@@<版本名>
            return AppendVersionSuffix(parser, baseName, symbolIndex, sectionType);
        }

        // 若该符号有有效外部版本（索引≥2），按是否默认版本追加 "@@版本"/"@版本"，否则原样返回
        private static string AppendVersionSuffix(ELFParser parser, string baseName, int symbolIndex, SectionType sectionType)
        {
            // .gnu.version 表按 .dynsym 索引建立，仅动态符号可据此追加版本后缀；symtab 符号索引语义不同，避免错配后缀
            if (sectionType != SectionType.SHT_DYNSYM)
            {
                return baseName;
            }

            List<ELFSymbol>? symbols = parser.Symbols.GetValueOrDefault(sectionType);
            if (symbols == null || symbolIndex < 0 || symbolIndex >= parser.VersionSymbols.Length)
            {
                return baseName;
            }

            ushort versionIndex = (ushort)(parser.VersionSymbols[symbolIndex] & 0x7fff); // 去除隐藏标志
            if (versionIndex < 2) // 版本索引从2开始是有效的外部版本
            {
                return baseName;
            }

            string versionName = GetVersionNameByVersionIndex(parser, versionIndex);
            if (string.IsNullOrEmpty(versionName))
            {
                return baseName;
            }

            // 判断是否是默认版本符号 (@@ 表示默认版本，否则 @)
            bool isDefaultVersion = IsDefaultVersionSymbol(parser, symbolIndex, symbols);
            return baseName + (isDefaultVersion ? "@@" : "@") + versionName;
        }

        private static string GetVersionNameByVersionIndex(ELFParser parser, ushort versionIndex)
        {
            // 版本索引 → 名称：优先 .gnu.version_d 定义，再 .gnu.version_r 依赖，最后回退 VER_n
            return parser.VersionDefinitions.GetValueOrDefault(versionIndex)
                ?? parser.VersionDependencies.GetValueOrDefault(versionIndex)
                ?? $"VER_{versionIndex}";
        }

        private static bool IsDefaultVersionSymbol(ELFParser parser, int symbolIndex, List<ELFSymbol> symbols)
        {
            // 简化实现：对于动态符号，根据符号绑定类型判断
            // 全局符号且版本号最低的为默认版本
            if (symbolIndex < parser.VersionSymbols.Length && symbolIndex < symbols.Count)
            {
                ELFSymbol symbol = symbols[symbolIndex];
                ushort versionIndex = (ushort)(parser.VersionSymbols[symbolIndex] & 0x7fff);

                // 检查是否是全局符号
                byte binding = (byte)(symbol.StInfo >> 4);
                if (binding is ((byte)SymbolBinding.STB_GLOBAL) or ((byte)SymbolBinding.STB_WEAK))
                {
                    // 简化处理：如果符号版本号是某个特定值，则认为是默认版本
                    return versionIndex == 1; // 版本1通常是默认版本
                }
            }

            return false;
        }

        internal static string GetSectionName(ELFParser parser, int index)
        {
            if (parser.SectionHeaders == null || index >= parser.SectionHeaders.Count)
            {
                return string.Empty;
            }

            byte[] strData = parser.GetSectionData(parser.Header.e_shstrndx);

            Models.ELFSectionHeader section = parser.SectionHeaders[index];
            int offset = (int)section.sh_name;
            return offset >= strData.Length ? string.Empty : ELFParserUtils.ExtractStringFromBytes(strData, offset);
        }
    }
}