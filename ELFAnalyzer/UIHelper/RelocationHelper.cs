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
            if (section.sh_entsize == 0)
            {
                return result; // 防除零：畸形节 sh_entsize 为 0
            }

            byte[] data = Parser.GetSectionData(sectionIndex);
            List<ELFSymbol> symbols = ReadRelocationSymbols(Parser, (int)section.sh_link);

            // 按真实 sh_type 判定 RELA/REL（条目大小不同），而非依赖节名是否含 "rela"
            bool isRela = section.sh_type == (uint)SectionType.SHT_RELA;
            int entrySize = Parser.Is64Bit ? (isRela ? 24 : 16) : (isRela ? 12 : 8);

            // entryCount 同时受声明条目数与实际可读字节约束，避免越界/截断节(data 为空或偏短)时越界读取
            int entryCount = (int)Math.Min(section.sh_size / section.sh_entsize, (ulong)(data.Length / entrySize));
            for (int j = 0; j < entryCount; j++)
            {
                ReadRelocationEntry(Parser, data, j, isRela, out ulong offset, out ulong info, out long addend);
                SplitRelocInfo(Parser.Is64Bit, info, out uint sym, out uint type);
                (string symbolName, string symbolValue) = ResolveRelocSymbol(Parser, symbols, sym);

                string typeName = ELFRelocation.GetRelocationTypeName(type, Parser.Header.e_machine);
                result.Add(new ELFRelocationInfo
                {
                    Offset = Parser.Is64Bit ? $"{offset:x16}" : $"{offset:x8}",
                    Info = Parser.Is64Bit ? $"{info:x16}" : $"{info:x8}",
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
                string currentSectionName = ELFSymbolNameResolver.GetSectionName(Parser, i) ?? string.Empty;
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

        // 读取重定位节关联的符号表（处理 32/64 位与字节序）。结果按 symTabIndex 缓存于 Parser，
        // 多个重定位/GOT 节共享同一符号表时避免整表重复解析（此前仅缓存了字节、仍重复 new ELFSymbol）
        private static List<ELFSymbol> ReadRelocationSymbols(ELFParser Parser, int symTabIndex)
        {
            return Parser.GetOrParseSymbols(symTabIndex, () =>
            {
                byte[] symTabData = Parser.GetSectionData(symTabIndex);
                List<ELFSymbol> symbols = [];
                bool little = Parser.Header.IsLittleEndian();
                int symEntrySize = Parser.Is64Bit ? 24 : 16; // 64位符号表项24字节，32位16字节
                int symCount = symTabData.Length / symEntrySize;

                for (int symIdx = 0; symIdx < symCount; symIdx++)
                {
                    int b = symIdx * symEntrySize;
                    symbols.Add(Parser.Is64Bit ? ReadSymbol64(symTabData, b, little) : ReadSymbol32(symTabData, b, little));
                }
                return symbols;
            });
        }

        private static ELFSymbol ReadSymbol64(byte[] d, int b, bool little) => new()
        {
            StName = ELFParserUtils.ReadUInt32(d, b, little),
            StInfo = d[b + 4],
            StOther = d[b + 5],
            StShndx = ELFParserUtils.ReadUInt16(d, b + 6, little),
            StValue = ELFParserUtils.ReadUInt64(d, b + 8, little),
            StSize = ELFParserUtils.ReadUInt64(d, b + 16, little)
        };

        private static ELFSymbol ReadSymbol32(byte[] d, int b, bool little) => new()
        {
            StName = ELFParserUtils.ReadUInt32(d, b, little),
            StValue = ELFParserUtils.ReadUInt32(d, b + 4, little),
            StSize = ELFParserUtils.ReadUInt32(d, b + 8, little),
            StInfo = d[b + 12],
            StOther = d[b + 13],
            StShndx = ELFParserUtils.ReadUInt16(d, b + 14, little)
        };

        // 读取一个重定位条目（32/64 位 × REL/RELA × 字节序），不修改源缓冲
        private static void ReadRelocationEntry(ELFParser Parser, byte[] data, int j, bool isRela, out ulong offset, out ulong info, out long addend)
        {
            addend = -1;
            bool little = Parser.Header.IsLittleEndian();
            if (Parser.Is64Bit)
            {
                int b = j * (isRela ? 24 : 16);
                offset = ELFParserUtils.ReadUInt64(data, b, little);
                info = ELFParserUtils.ReadUInt64(data, b + 8, little);
                if (isRela)
                {
                    addend = ELFParserUtils.ReadInt64(data, b + 16, little);
                }
            }
            else
            {
                int b = j * (isRela ? 12 : 8);
                offset = ELFParserUtils.ReadUInt32(data, b, little);
                info = ELFParserUtils.ReadUInt32(data, b + 4, little);
                if (isRela)
                {
                    addend = ELFParserUtils.ReadInt32(data, b + 8, little);
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
                // 回退值宽度与正常值一致：64 位 16 位十六进制，32 位 8 位
                return (string.Empty, Parser.Is64Bit ? "0000000000000000" : "00000000");
            }
            ELFSymbol symbol = symbols[(int)sym];
            string name = ResolveRelocSymbolName(Parser, symbol, (int)sym);
            string value = Parser.Is64Bit ? $"{symbol.StValue:x16}" : $"{symbol.StValue:x8}";
            return (name, value);
        }

        // 解析重定位符号名：SECTION 类符号 st_name 通常为 0，回退显示其所属节名（与符号表一致）
        private static string ResolveRelocSymbolName(ELFParser Parser, ELFSymbol symbol, int index)
        {
            string name = ELFSymbolNameResolver.GetSymbolName(Parser, symbol, SectionType.SHT_DYNSYM, index);
            if (string.IsNullOrEmpty(name)
                && (byte)(symbol.StInfo & ELFConstants.ST_TYPE_MASK) == (byte)SymbolType.STT_SECTION
                && symbol.StShndx > 0 && symbol.StShndx < ELFConstants.SHN_LORESERVE)
            {
                name = ELFSymbolNameResolver.GetSectionName(Parser, symbol.StShndx);
            }
            return name;
        }
    }
}