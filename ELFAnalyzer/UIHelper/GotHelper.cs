using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using System.Globalization;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class GotHelper
    {
        // 解析指定 GOT 节(.got 或 .got.plt)，每个槽位通过动态重定位(r_offset==槽位地址)关联符号与类型
        internal static List<ELFGotInfo> GetGotInfoList(ELFParser Parser, string sectionName)
        {
            List<ELFGotInfo> result = [];
            if (Parser.SectionHeaders == null)
            {
                return result;
            }

            Dictionary<ulong, ELFRelocationInfo> relocByOffset = BuildRelocationMap(Parser);

            AppendGotSection(Parser, sectionName, relocByOffset, result);

            return result;
        }

        // 收集所有动态重定位，按 r_offset(目标 GOT 槽位地址) 建索引
        private static Dictionary<ulong, ELFRelocationInfo> BuildRelocationMap(ELFParser Parser)
        {
            Dictionary<ulong, ELFRelocationInfo> map = [];
            string[] relocSections = [".rela.plt", ".rel.plt", ".rela.dyn", ".rel.dyn"];
            foreach (string name in relocSections)
            {
                foreach (ELFRelocationInfo reloc in RelocationHelper.GetRelocationInfoForSpecificSection(Parser, name))
                {
                    if (ulong.TryParse(reloc.Offset.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ulong off))
                    {
                        map[off] = reloc;
                    }
                }
            }
            return map;
        }

        private static void AppendGotSection(ELFParser Parser, string sectionName,
            Dictionary<ulong, ELFRelocationInfo> relocByOffset, List<ELFGotInfo> result)
        {
            int sectionIndex = FindSectionIndex(Parser, sectionName);
            if (sectionIndex < 0)
            {
                return;
            }

            Models.ELFSectionHeader section = Parser.SectionHeaders[sectionIndex];
            bool isLittleEndian = Parser.Header.IsLittleEndian();

            int entrySize = Parser.Is64Bit ? 8 : 4;
            if (section.sh_entsize is > 0 and <= 8)
            {
                entrySize = (int)section.sh_entsize;
            }

            // 安全：槽位数取自不可信 sh_size，夹紧到文件实际可承载的条目数，避免超大节构造海量对象(OOM)
            long maxReadable = section.sh_offset >= (ulong)Parser.FileData.Length
                ? 0
                : (Parser.FileData.Length - (long)section.sh_offset) / entrySize;
            int count = (int)Math.Min(section.sh_size / (ulong)entrySize, (ulong)Math.Max(maxReadable, 0));
            for (int i = 0; i < count; i++)
            {
                ulong slotAddr = section.sh_addr + (ulong)((long)i * entrySize);
                long fileOffset = (long)section.sh_offset + (long)i * entrySize;

                string value = ReadSlotValue(Parser, fileOffset, entrySize, isLittleEndian);
                (string type, string symbol) = ResolveSlot(relocByOffset, slotAddr, sectionName, i);

                result.Add(new ELFGotInfo
                {
                    Number = $"{i}",
                    Offset = entrySize >= 8 ? $"0x{slotAddr:x16}" : $"0x{slotAddr:x8}",
                    Value = value,
                    Type = type,
                    Symbol = symbol,
                    SectionName = sectionName
                });
            }
        }

        private static string ReadSlotValue(ELFParser Parser, long fileOffset, int entrySize, bool isLittleEndian)
        {
            // 按实际读取宽度校验边界，而非不可信 entrySize：entrySize 可被夹为 1..7（<8），
            // 但下方对 <8 一律 ReadUInt32 固定读 4 字节；若仅按 entrySize(如 3) 校验，
            // 槽位落在文件末尾时 ReadUInt32 会越界抛异常崩溃。
            int readWidth = entrySize >= 8 ? 8 : 4;
            if (fileOffset < 0 || fileOffset + readWidth > Parser.FileData.Length)
            {
                return string.Empty;
            }

            if (entrySize >= 8)
            {
                ulong stored = ELFParserUtils.ReadUInt64(Parser.FileData, (int)fileOffset, isLittleEndian);
                return $"0x{stored:x16}";
            }

            uint stored32 = ELFParserUtils.ReadUInt32(Parser.FileData, (int)fileOffset, isLittleEndian);
            return $"0x{stored32:x8}";
        }

        private static (string type, string symbol) ResolveSlot(
            Dictionary<ulong, ELFRelocationInfo> relocByOffset, ulong slotAddr, string sectionName, int index)
        {
            ELFRelocationInfo? reloc = relocByOffset.GetValueOrDefault(slotAddr);
            if (reloc != null)
            {
                return (reloc.Type, reloc.Symbol);
            }

            // .got.plt 前 3 个为保留槽：GOT[0]=.dynamic 地址，GOT[1]/[2]=运行时链接器使用
            if (sectionName == ".got.plt" && index < 3)
            {
                return ("保留", string.Empty);
            }

            return (string.Empty, string.Empty);
        }

        private static int FindSectionIndex(ELFParser Parser, string sectionName)
        {
            if (Parser.SectionHeaders == null)
            {
                return -1;
            }
            for (int i = 0; i < Parser.SectionHeaders.Count; i++)
            {
                if (ELFSymbolNameResolver.GetSectionName(Parser, i) == sectionName)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
