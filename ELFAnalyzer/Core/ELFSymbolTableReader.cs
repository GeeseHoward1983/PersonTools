using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFSymbolTableReader
    {
        internal static void ReadSymbolTables(ELFParser parser, BinaryReader reader, bool isLittleEndian)
        {
            for (int i = 0; i < parser.SectionHeaders?.Count; i++)
            {
                Models.ELFSectionHeader section = parser.SectionHeaders[i];
                if (section.sh_type is not (((uint)SectionType.SHT_SYMTAB) or ((uint)SectionType.SHT_DYNSYM)))
                {
                    continue;
                }

                if (section.sh_entsize == 0)
                {
                    continue; // 畸形文件 sh_entsize 为 0 时跳过，避免除零
                }

                // 安全：sh_offset/sh_size 来自不可信节头，校验整段落在文件内，
                // 避免畸形大小触发超大 List 预分配(OOM) 或读取越界(EndOfStream)
                if (!ELFParserUtils.IsRangeWithin(section.sh_offset, section.sh_size, (ulong)parser.FileData.Length))
                {
                    continue;
                }

                reader.BaseStream.Seek((long)section.sh_offset, SeekOrigin.Begin);

                // 安全：sh_entsize 不可信，实际按固定大小读取（Elf64_Sym=24 / Elf32_Sym=16）。
                // 按固定项大小算条目数，并夹到节内可读字节，避免伪造 sh_entsize=1 令 count 暴增、
                // 越节读到 EOF 抛 EndOfStream 中止整份分析。
                int entrySize = parser.Is64Bit ? ELFConstants.SymbolEntrySize64 : ELFConstants.SymbolEntrySize32;
                int symbolCount = (int)(section.sh_size / (ulong)entrySize);
                List<ELFSymbol> symbols = new(Math.Min(symbolCount, ELFConstants.MaxPreallocCount)); // 容量按需增长，不按不可信值一次性预分配
                for (int j = 0; j < symbolCount; j++)
                {
                    symbols.Add(ReadSymbol(reader, parser.Is64Bit, isLittleEndian));
                }

                parser.Symbols.Add((SectionType)section.sh_type, symbols);
                // 记录符号表关联的字符串表索引
                parser.LinkedStrTabIdx.Add((SectionType)section.sh_type, section.sh_link);
            }
        }

        // 读取单个符号记录（Elf32_Sym 与 Elf64_Sym 字段顺序不同）
        private static ELFSymbol ReadSymbol(BinaryReader reader, bool is64Bit, bool isLittleEndian)
        {
            ELFSymbol symbol = new()
            {
                StName = ELFParserUtils.ReadUInt32(reader, isLittleEndian)
            };
            if (is64Bit)
            {
                symbol.StInfo = reader.ReadByte();
                symbol.StOther = reader.ReadByte();
                symbol.StShndx = ELFParserUtils.ReadUInt16(reader, isLittleEndian);
                symbol.StValue = ELFParserUtils.ReadUInt64(reader, isLittleEndian);
                symbol.StSize = ELFParserUtils.ReadUInt64(reader, isLittleEndian);
            }
            else
            {
                symbol.StValue = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
                symbol.StSize = ELFParserUtils.ReadUInt32(reader, isLittleEndian);
                symbol.StInfo = reader.ReadByte();
                symbol.StOther = reader.ReadByte();
                symbol.StShndx = ELFParserUtils.ReadUInt16(reader, isLittleEndian);
            }

            return symbol;
        }
    }
}