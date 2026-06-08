using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.Globalization;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class RelocationHelper
    {
        internal static List<ELFRelocationInfo> GetRelocationInfoForSpecificSection(ELFParser Parser, string sectionName)
        {
            List<ELFRelocationInfo> result = [];

            int sectionIndex = FindRelocationSection(Parser, sectionName, out string actualSectionName);
            if (sectionIndex == -1)
            {
                return result;
            }

            Models.ELFSectionHeader section = Parser.SectionHeaders![sectionIndex];
            int entryCount = (int)(section.sh_size / section.sh_entsize);
            byte[] data = Parser.CopySectionData(in section);

            Models.ELFSectionHeader symTabSection = Parser.SectionHeaders[(int)section.sh_link];
            List<ELFSymbol> symbols = ReadRelocationSymbols(Parser, symTabSection);

            bool isRela = sectionName.Contains("rela", StringComparison.CurrentCulture);
            for (int j = 0; j < entryCount; j++)
            {
                ReadRelocationEntry(Parser, data, j, isRela, out ulong offset, out ulong info, out long addend);
                SplitRelocInfo(Parser.Is64Bit, info, out uint sym, out uint type);
                (string symbolName, string symbolValue) = ResolveRelocSymbol(Parser, symbols, sym);

                string typeName = ELFRelocation.GetRelocationTypeName(type, Parser.Header.e_machine);
                result.Add(new ELFRelocationInfo
                {
                    Offset = $"{offset:x16}".PadLeft(12),
                    Info = $"{info:x16}".PadLeft(12),
                    Type = typeName ?? "",
                    SymbolValue = symbolValue.PadLeft(16),
                    Symbol = symbolName,
                    Addend = isRela ? addend.ToString(CultureInfo.InvariantCulture) : "",
                    SectionName = actualSectionName
                });
            }

            return result;
        }

        // 按名称查找 RELA/REL 重定位节，返回其索引（找不到或类型不符返回 -1）
        private static int FindRelocationSection(ELFParser Parser, string sectionName, out string actualSectionName)
        {
            actualSectionName = string.Empty;
            if (Parser.SectionHeaders == null)
            {
                return -1;
            }

            for (int i = 0; i < Parser.SectionHeaders.Count; i++)
            {
                string currentSectionName = SymbleName.GetSectionName(Parser, i) ?? string.Empty;
                if (currentSectionName != sectionName)
                {
                    continue;
                }

                Models.ELFSectionHeader section = Parser.SectionHeaders[i];
                if (section.sh_type is ((uint)SectionType.SHT_RELA) or ((uint)SectionType.SHT_REL))
                {
                    actualSectionName = currentSectionName;
                    return i;
                }
                return -1;
            }
            return -1;
        }

        // 读取重定位节关联的符号表（处理 32/64 位与字节序）
        private static List<ELFSymbol> ReadRelocationSymbols(ELFParser Parser, Models.ELFSectionHeader symTabSection)
        {
            byte[] symTabData = Parser.CopySectionData(in symTabSection);
            List<ELFSymbol> symbols = [];
            int symEntrySize = Parser.Is64Bit ? 24 : 16; // 64位符号表项24字节，32位16字节
            int symCount = symTabData.Length / symEntrySize;

            for (int symIdx = 0; symIdx < symCount; symIdx++)
            {
                int b = symIdx * symEntrySize;
                if (!Parser.Header.IsLittleEndian())
                {
                    ReverseSymbolFields(symTabData, b, Parser.Is64Bit);
                }
                symbols.Add(Parser.Is64Bit ? ReadSymbol64(symTabData, b) : ReadSymbol32(symTabData, b));
            }
            return symbols;
        }

        private static void ReverseSymbolFields(byte[] d, int b, bool is64)
        {
            Array.Reverse(d, b, 4); // st_name
            if (is64)
            {
                Array.Reverse(d, b + 8, 8);  // st_value
                Array.Reverse(d, b + 16, 8); // st_size
                Array.Reverse(d, b + 6, 2);  // st_shndx
            }
            else
            {
                Array.Reverse(d, b + 4, 4);  // st_value
                Array.Reverse(d, b + 8, 4);  // st_size
                Array.Reverse(d, b + 14, 2); // st_shndx
            }
        }

        private static ELFSymbol ReadSymbol64(byte[] d, int b) => new()
        {
            StName = BitConverter.ToUInt32(d, b),
            StValue = BitConverter.ToUInt64(d, b + 8),
            StSize = BitConverter.ToUInt64(d, b + 16),
            StInfo = d[b + 4],
            StOther = d[b + 5],
            StShndx = BitConverter.ToUInt16(d, b + 6)
        };

        private static ELFSymbol ReadSymbol32(byte[] d, int b) => new()
        {
            StName = BitConverter.ToUInt32(d, b),
            StValue = BitConverter.ToUInt32(d, b + 4),
            StSize = BitConverter.ToUInt32(d, b + 8),
            StInfo = d[b + 12],
            StOther = d[b + 13],
            StShndx = BitConverter.ToUInt16(d, b + 14)
        };

        // 读取一个重定位条目（32/64 位 × REL/RELA × 字节序）
        private static void ReadRelocationEntry(ELFParser Parser, byte[] data, int j, bool isRela, out ulong offset, out ulong info, out long addend)
        {
            addend = -1;
            bool little = Parser.Header.IsLittleEndian();
            if (Parser.Is64Bit)
            {
                int b = j * (isRela ? 24 : 16);
                if (!little)
                {
                    Array.Reverse(data, b, 8);
                    Array.Reverse(data, b + 8, 8);
                    if (isRela)
                    {
                        Array.Reverse(data, b + 16, 8);
                    }
                }
                offset = BitConverter.ToUInt64(data, b);
                info = BitConverter.ToUInt64(data, b + 8);
                if (isRela)
                {
                    addend = BitConverter.ToInt64(data, b + 16);
                }
            }
            else
            {
                int b = j * (isRela ? 12 : 8);
                if (!little)
                {
                    Array.Reverse(data, b, 4);
                    Array.Reverse(data, b + 4, 4);
                    if (isRela)
                    {
                        Array.Reverse(data, b + 8, 4);
                    }
                }
                offset = BitConverter.ToUInt32(data, b);
                info = BitConverter.ToUInt32(data, b + 4);
                if (isRela)
                {
                    addend = BitConverter.ToInt32(data, b + 8);
                }
            }
        }

        // 拆分 r_info 为符号索引与重定位类型（位数不同分割位不同）
        private static void SplitRelocInfo(bool is64, ulong info, out uint sym, out uint type)
        {
            if (is64)
            {
                sym = (uint)(info >> 32);
                type = (uint)(info & 0xffffffff);
            }
            else
            {
                sym = (uint)(info >> 8);
                type = (uint)(info & 0xff);
            }
        }

        // 解析符号名与符号值（越界返回默认零值）
        private static (string name, string value) ResolveRelocSymbol(ELFParser Parser, List<ELFSymbol> symbols, uint sym)
        {
            if (sym >= symbols.Count)
            {
                return (string.Empty, "0000000000000000");
            }
            ELFSymbol symbol = symbols[(int)sym];
            string name = ResolveRelocSymbolName(Parser, symbol, (int)sym);
            string value = Parser.Is64Bit ? $"{symbol.StValue:x16}" : $"{symbol.StValue:x8}";
            return (name, value);
        }

        // 解析重定位符号名：SECTION 类符号 st_name 通常为 0，回退显示其所属节名（与符号表一致）
        private static string ResolveRelocSymbolName(ELFParser Parser, ELFSymbol symbol, int index)
        {
            string name = SymbleName.GetSymbolName(Parser, symbol, SectionType.SHT_DYNSYM, index);
            if (string.IsNullOrEmpty(name)
                && (byte)(symbol.StInfo & 0x0F) == (byte)SymbolType.STT_SECTION
                && symbol.StShndx > 0 && symbol.StShndx < 0xFF00)
            {
                name = SymbleName.GetSectionName(Parser, symbol.StShndx);
            }
            return name;
        }
    }
}