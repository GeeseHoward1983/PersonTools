using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class SymbolTable
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

                reader.BaseStream.Seek((long)section.sh_offset, SeekOrigin.Begin);

                int symbolCount = (int)(section.sh_size / section.sh_entsize);
                List<ELFSymbol> symbols = new(symbolCount);
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