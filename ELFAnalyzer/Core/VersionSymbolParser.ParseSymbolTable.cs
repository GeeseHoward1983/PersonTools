using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static partial class VersionSymbolParser
    {
        private static void ParseVersionSymbolTable(ELFParser parser)
        {
            // 查找版本符号表 (DT_VERSYM)
            long versymAddr = FindDynamicValue(parser, DynamicTag.DT_VERSYM);

            // 查找对应的节头
            if (versymAddr > 0)
            {
                Models.ELFSectionHeader? versymSection = ELFParserUtils.FindSectionByAddress(parser, (ulong)versymAddr);
                if (versymSection != null)
                {
                    byte[] data = parser.CopySectionData(versymSection.Value);

                    // 按实际读到的字节数计算条目，避免越界/截断节(CopySectionData 返回空)时 sh_size 仍驱动越界读取
                    int count = data.Length / 2; // 每个版本符号是2字节
                    bool isLittleEndian = parser.Header.IsLittleEndian();
                    parser.VersionSymbols = new ushort[count];

                    for (int i = 0; i < count; i++)
                    {
                        parser.VersionSymbols[i] = ELFParserUtils.ReadUInt16(data, i * 2, isLittleEndian);
                    }
                }
            }
        }

        // 扫描动态段取指定标签的值（同标签多次出现取最后一次，与原逐项覆盖一致）；未找到返回 defaultValue
        private static long FindDynamicValue(ELFParser parser, DynamicTag tag, long defaultValue = 0)
        {
            long value = defaultValue;
            if (parser.DynamicEntries != null)
            {
                foreach (ELFDynamic entry in parser.DynamicEntries)
                {
                    if (entry.d_tag == (long)tag)
                    {
                        value = (long)entry.d_val;
                    }
                }
            }

            return value;
        }
    }
}