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

            // 先找到对应节的索引
            int sectionIndex = -1;
            string actualSectionName = string.Empty;

            if (Parser.SectionHeaders != null)
            {
                for (int i = 0; i < Parser.SectionHeaders.Count; i++)
                {
                    string currentSectionName = SymbleName.GetSectionName(Parser, i) ?? string.Empty;
                    if (currentSectionName == sectionName)
                    {
                        sectionIndex = i;
                        actualSectionName = currentSectionName;
                        break;
                    }
                }

                if (sectionIndex != -1)
                {
                    Models.ELFSectionHeader section = Parser.SectionHeaders[sectionIndex];
                    if (section.sh_type is ((uint)SectionType.SHT_RELA) or ((uint)SectionType.SHT_REL)) // RELA/REL类型节
                    {
                        // 计算条目数
                        int entryCount = (int)(section.sh_size / section.sh_entsize);

                        // 读取数据
                        byte[] data = new byte[section.sh_size];
                        Array.Copy(Parser.FileData, (int)section.sh_offset, data, 0, (int)section.sh_size);

                        // 读取符号表和字符串表
                        Models.ELFSectionHeader symTabSection = Parser.SectionHeaders[(int)section.sh_link];
                        Models.ELFSectionHeader strTabSection = Parser.SectionHeaders[(int)symTabSection.sh_link];
                        byte[] strData = new byte[strTabSection.sh_size];
                        Array.Copy(Parser.FileData, (int)strTabSection.sh_offset, strData, 0, (int)strTabSection.sh_size);

                        byte[] symTabData = new byte[symTabSection.sh_size];
                        Array.Copy(Parser.FileData, (int)symTabSection.sh_offset, symTabData, 0, (int)symTabSection.sh_size);

                        // 读取符号表
                        List<ELFSymbol> symbols = [];
                        int symEntrySize = Parser.Is64Bit ? 24 : 16; // 64位ELF符号表项大小为24字节，32位为16字节
                        int symCount = symTabData.Length / symEntrySize;

                        for (int symIdx = 0; symIdx < symCount; symIdx++)
                        {
                            if (!Parser.Header.IsLittleEndian()) // 如果不是小端序
                            {

                                Array.Reverse(symTabData, symIdx * symEntrySize, 4);
                                if (Parser.Is64Bit)
                                {
                                    Array.Reverse(symTabData, symIdx * symEntrySize + 8, 8);
                                    Array.Reverse(symTabData, symIdx * symEntrySize + 16, 8);
                                    Array.Reverse(symTabData, symIdx * symEntrySize + 6, 2);
                                }
                                else
                                {
                                    Array.Reverse(symTabData, symIdx * symEntrySize + 4, 4);
                                    Array.Reverse(symTabData, symIdx * symEntrySize + 8, 4);
                                    Array.Reverse(symTabData, symIdx * symEntrySize + 14, 2);

                                }
                            }
                            symbols.Add(Parser.Is64Bit ? new ELFSymbol
                            {
                                StName = BitConverter.ToUInt32(symTabData, symIdx * symEntrySize),
                                StValue = BitConverter.ToUInt64(symTabData, symIdx * symEntrySize + 8),
                                StSize = BitConverter.ToUInt64(symTabData, symIdx * symEntrySize + 16),
                                StInfo = symTabData[symIdx * symEntrySize + 4],
                                StOther = symTabData[symIdx * symEntrySize + 5],
                                StShndx = BitConverter.ToUInt16(symTabData, symIdx * symEntrySize + 6)
                            } : new ELFSymbol
                            {
                                StName = BitConverter.ToUInt32(symTabData, symIdx * symEntrySize),
                                StValue = BitConverter.ToUInt32(symTabData, symIdx * symEntrySize + 4),
                                StSize = BitConverter.ToUInt32(symTabData, symIdx * symEntrySize + 8),
                                StInfo = symTabData[symIdx * symEntrySize + 12],
                                StOther = symTabData[symIdx * symEntrySize + 13],
                                StShndx = BitConverter.ToUInt16(symTabData, symIdx * symEntrySize + 14)
                            });
                        }

                        for (int j = 0; j < entryCount; j++)
                        {
                            ulong offset;
                            ulong info;
                            long addend = -1;
                            uint sym;
                            uint type;
                            string symbolName = string.Empty;
                            string symbolValue = "0000000000000000"; // 默认符号值

                            if (!Parser.Is64Bit)
                            {
                                if (sectionName.Contains("rela", StringComparison.CurrentCulture))
                                {
                                    if (!Parser.Header.IsLittleEndian()) // 如果不是小端序
                                    {
                                        Array.Reverse(data, j * 12, 4);
                                        Array.Reverse(data, j * 12 + 4, 4);
                                        Array.Reverse(data, j * 12 + 8, 4);
                                    }
                                    // 读取32位RELA条目
                                    // r_offset (4 bytes), r_info (4 bytes), r_addend (4 bytes)
                                    offset = BitConverter.ToUInt32(data, j * 12);
                                    info = BitConverter.ToUInt32(data, j * 12 + 4);
                                    addend = BitConverter.ToInt32(data, j * 12 + 8);
                                }
                                else
                                {
                                    if (!Parser.Header.IsLittleEndian()) // 如果不是小端序
                                    {
                                        Array.Reverse(data, j * 8, 4);
                                        Array.Reverse(data, j * 8 + 4, 4);
                                    }
                                    // 读取32位REL条目
                                    // r_offset (4 bytes), r_info (4 bytes)
                                    offset = BitConverter.ToUInt32(data, j * 8);
                                    info = BitConverter.ToUInt32(data, j * 8 + 4);
                                }

                                // 解析info字段
                                sym = (uint)(info >> 8); // 符号索引
                                type = (uint)(info & 0xff); // 重定位类型

                                // 读取符号名和符号值
                                if (sym < symbols.Count)
                                {
                                    ELFSymbol symbol = symbols[(int)sym];
                                    symbolName = SymbleName.GetSymbolName(Parser, symbol, SectionType.SHT_DYNSYM);
                                    symbolValue = $"{symbol.StValue:x8}";
                                }
                            }
                            else
                            {
                                if (sectionName.Contains("rela", StringComparison.CurrentCulture))
                                {
                                    if (!Parser.Header.IsLittleEndian()) // 如果不是小端序
                                    {
                                        Array.Reverse(data, j * 24, 8);
                                        Array.Reverse(data, j * 24 + 8, 8);
                                        Array.Reverse(data, j * 24 + 16, 8);
                                    }
                                    // 读取64位RELA条目
                                    // r_offset (8 bytes), r_info (8 bytes), r_addend (8 bytes)
                                    offset = BitConverter.ToUInt64(data, j * 24);
                                    info = BitConverter.ToUInt64(data, j * 24 + 8);
                                    addend = BitConverter.ToInt64(data, j * 24 + 16);
                                }
                                else
                                {
                                    if (!Parser.Header.IsLittleEndian()) // 如果不是小端序
                                    {
                                        Array.Reverse(data, j * 16, 8);
                                        Array.Reverse(data, j * 16 + 8, 8);
                                    }
                                    // 读取64位REL条目
                                    // r_offset (8 bytes), r_info (8 bytes)
                                    offset = BitConverter.ToUInt64(data, j * 16);
                                    info = BitConverter.ToUInt64(data, j * 16 + 8);
                                }

                                // 解析info字段
                                sym = (uint)(info >> 32); // 符号索引
                                type = (uint)(info & 0xffffffff); // 重定位类型

                                // 读取符号名和符号值
                                if (sym < symbols.Count)
                                {
                                    ELFSymbol symbol = symbols[(int)sym];
                                    symbolName = SymbleName.GetSymbolName(Parser, symbol, SectionType.SHT_DYNSYM);
                                    symbolValue = $"{symbol.StValue:x16}";
                                }
                            }

                            // 获取重定位类型名称
                            string typeName = ELFRelocation.GetRelocationTypeName(type, Parser.Header.e_machine);

                            result.Add(new ELFRelocationInfo
                            {
                                Offset = $"{offset:x16}".PadLeft(12),
                                Info = $"{info:x16}".PadLeft(12),
                                Type = typeName ?? "",
                                SymbolValue = symbolValue.PadLeft(16),
                                Symbol = symbolName,
                                Addend = sectionName.Contains("rela", StringComparison.CurrentCulture) ? addend.ToString(CultureInfo.InvariantCulture) : "",
                                SectionName = actualSectionName
                            });
                        }
                    }
                }
            }

            return result;
        }
    }
}