using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static partial class VersionSymbleTable
    {
        private static void ParseVersionSymbolTable(ELFParser parser)
        {
            // 查找版本符号表 (DT_VERSYM)
            long versymAddr = 0;

            if (parser.DynamicEntries != null)
            {
                foreach (ELFDynamic entry in parser.DynamicEntries)
                {
                    if (entry.d_tag == (long)DynamicTag.DT_VERSYM)
                    {
                        versymAddr = (long)entry.d_val;
                    }
                }
            }
            // 查找对应的节头
            if (versymAddr > 0)
            {
                Models.ELFSectionHeader? versymSection = FindSectionByAddress(parser, (ulong)versymAddr);
                if (versymSection != null)
                {
                    byte[] data = parser.CopySectionData(versymSection.Value);

                    int count = (int)(versymSection.Value.sh_size / 2); // 每个版本符号是2字节
                    bool isLittleEndian = parser.Header.IsLittleEndian();
                    parser.VersionSymbols = new ushort[count];

                    for (int i = 0; i < count; i++)
                    {
                        parser.VersionSymbols[i] = ELFParserUtils.ReadUInt16(data, i * 2, isLittleEndian);
                    }
                }
            }
        }
    }
}