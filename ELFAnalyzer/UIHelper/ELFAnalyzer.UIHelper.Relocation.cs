using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.Text;

namespace PersonalTools.ELFAnalyzer
{
    public partial class ELFAnalyzer
    {
        public List<ELFRelocationInfo> GetRelocationInfoForSpecificSection(string sectionName)
        {
            var result = new List<ELFRelocationInfo>();
            
            // 先找到对应节的索引
            int sectionIndex = -1;
            string actualSectionName = string.Empty;
            
            if (_parser.SectionHeaders != null)
            {
                for (int i = 0; i < _parser.SectionHeaders.Count; i++)
                {
                    string currentSectionName = _parser.GetSectionName(i) ?? string.Empty;
                    if (currentSectionName == sectionName)
                    {
                        sectionIndex = i;
                        actualSectionName = currentSectionName;
                        break;
                    }
                }
                
                if (sectionIndex != -1)
                {
                    var section = _parser.SectionHeaders[sectionIndex];
                    if (section.sh_type == (uint)SectionType.SHT_RELA) // RELA类型节
                    {
                        // 计算条目数
                        int entryCount = (int)(section.sh_size / section.sh_entsize);
                        
                        // 读取数据
                        var data = new byte[section.sh_size];
                        Array.Copy(_parser.FileData, (int)section.sh_offset, data, 0, (int)section.sh_size);
                        
                        // 读取符号表和字符串表
                        var symTabSection = _parser.SectionHeaders[(int)section.sh_link];
                        var strTabSection = _parser.SectionHeaders[(int)symTabSection.sh_link];
                        var strData = new byte[strTabSection.sh_size];
                        Array.Copy(_parser.FileData, (int)strTabSection.sh_offset, strData, 0, (int)strTabSection.sh_size);
                        
                        var symTabData = new byte[symTabSection.sh_size];
                        Array.Copy(_parser.FileData, (int)symTabSection.sh_offset, symTabData, 0, (int)symTabSection.sh_size);

                        // 读取符号表
                        var symbols = new List<ELFSymbol>();
                        int symEntrySize = _parser._is64Bit ? 24 : 16; // 64位ELF符号表项大小为24字节，32位为16字节
                        int symCount = symTabData.Length / symEntrySize;
                        
                        for (int symIdx = 0; symIdx < symCount; symIdx++)
                        {
                            ELFSymbol symbol;
                            if (!_parser._is64Bit)
                            {
                                // 32位符号
                                uint st_name = BitConverter.ToUInt32(symTabData, symIdx * symEntrySize);
                                uint st_value = BitConverter.ToUInt32(symTabData, symIdx * symEntrySize + 4);
                                uint st_size = BitConverter.ToUInt32(symTabData, symIdx * symEntrySize + 8);
                                byte st_info = symTabData[symIdx * symEntrySize + 12];
                                byte st_other = symTabData[symIdx * symEntrySize + 13];
                                ushort st_shndx = BitConverter.ToUInt16(symTabData, symIdx * symEntrySize + 14);
                                
                                symbol = new ELFSymbol
                                {
                                    st_name = st_name,
                                    st_value = st_value,
                                    st_size = st_size,
                                    st_info = st_info,
                                    st_other = st_other,
                                    st_shndx = st_shndx
                                };
                            }
                            else
                            {
                                // 64位符号
                                uint st_name = BitConverter.ToUInt32(symTabData, symIdx * symEntrySize);
                                byte st_info = symTabData[symIdx * symEntrySize + 4];
                                byte st_other = symTabData[symIdx * symEntrySize + 5];
                                ushort st_shndx = BitConverter.ToUInt16(symTabData, symIdx * symEntrySize + 6);
                                ulong st_value = BitConverter.ToUInt64(symTabData, symIdx * symEntrySize + 8);
                                ulong st_size = BitConverter.ToUInt64(symTabData, symIdx * symEntrySize + 16);
                                
                                symbol = new ELFSymbol
                                {
                                    st_name = st_name,
                                    st_value = st_value,
                                    st_size = st_size,
                                    st_info = st_info,
                                    st_other = st_other,
                                    st_shndx = st_shndx
                                };
                            }
                            symbols.Add(symbol);
                        }

                        for (int j = 0; j < entryCount; j++)
                        {
                            ulong offset;
                            ulong info;
                            long addend;
                            uint sym;
                            uint type;
                            string symbolName = string.Empty;
                            string symbolValue = "0000000000000000"; // 默认符号值
                            
                            if (!_parser._is64Bit)
                            {
                                // 读取32位RELA条目
                                // r_offset (4 bytes), r_info (4 bytes), r_addend (4 bytes)
                                offset = BitConverter.ToUInt32(data, j * 12);
                                info = BitConverter.ToUInt32(data, j * 12 + 4);
                                addend = BitConverter.ToInt32(data, j * 12 + 8);

                                // 解析info字段
                                sym = (uint)(info >> 8); // 符号索引
                                type = (uint)(info & 0xff); // 重定位类型

                                // 读取符号名和符号值
                                if (sym < symbols.Count)
                                {
                                    var symbol = symbols[(int)sym];
                                    symbolName = _parser.GetSymbolName(symbol, SectionType.SHT_DYNSYM) ?? "Unknown";
                                    symbolValue = $"{symbol.st_value:x8}";
                                }
                            }
                            else
                            {
                                // 读取64位RELA条目
                                // r_offset (8 bytes), r_info (8 bytes), r_addend (8 bytes)
                                offset = BitConverter.ToUInt64(data, j * 24);
                                info = BitConverter.ToUInt64(data, j * 24 + 8);
                                addend = BitConverter.ToInt64(data, j * 24 + 16);

                                // 解析info字段
                                sym = (uint)(info >> 32); // 符号索引
                                type = (uint)(info & 0xffffffff); // 重定位类型

                                // 读取符号名和符号值
                                if (sym < symbols.Count)
                                {
                                    var symbol = symbols[(int)sym];
                                    symbolName = _parser.GetSymbolName(symbol, SectionType.SHT_DYNSYM) ?? "Unknown";
                                    symbolValue = $"{symbol.st_value:x16}";
                                }
                            }
                            
                            // 获取重定位类型名称
                            string? typeName = ELFParser.GetRelocationTypeName(type, _parser.Header.e_machine);

                            result.Add(new ELFRelocationInfo
                            {
                                Offset = $"{offset:x16}".PadLeft(12),
                                Info = $"{info:x16}".PadLeft(12),
                                Type = typeName ?? "",
                                SymbolValue = symbolValue.PadLeft(16),
                                Symbol = symbolName,
                                Addend = addend.ToString(),
                                SectionName = actualSectionName
                            });
                        }
                    }
                }
            }
            
            return result;
        }

        private static string? ReadStringFromBytes(byte[] data, int offset)
        {
            if (offset < 0 || offset >= data.Length) return null;
            
            int start = offset;
            while (start < data.Length && data[start] == 0) start++; // 跳过开头的空字符
            
            if (start >= data.Length) return null;
            
            int end = start;
            while (end < data.Length && data[end] != 0) end++;
            
            if (end <= start) return null;
            
            try
            {
                return Encoding.UTF8.GetString(data, start, end - start);
            }
            catch
            {
                return null;
            }
        }
    }
}