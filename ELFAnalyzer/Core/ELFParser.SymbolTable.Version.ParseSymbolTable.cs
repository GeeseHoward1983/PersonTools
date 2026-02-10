using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class VersionSymbleTable
    {
        private static void ParseVersionSymbolTable(ELFParser parser)
        {
            // 查找版本符号表 (DT_VERSYM)
            long versymAddr = 0;

            if (parser.DynamicEntries != null)
            {
                foreach (var entry in parser.DynamicEntries)
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
                var versymSection = FindSectionByAddress(parser, (ulong)versymAddr);
                if (versymSection != null)
                {
                    var data = new byte[versymSection.Value.sh_size];
                    Array.Copy(parser.FileData, (long)versymSection.Value.sh_offset, data, 0, (int)versymSection.Value.sh_size);

                    int count = (int)(versymSection.Value.sh_size / 2); // 每个版本符号是2字节
                    parser.VersionSymbols = new ushort[count];

                    for (int i = 0; i < count; i++)
                    {
                        if (!parser.Header.IsLittleEndian()) // 如果不是小端序
                        {
                            Array.Reverse(parser.FileData, (int)i * 2, 2);
                        }

                        BitConverter.ToUInt16(data, i * 2);
                    }
                }
            }
        }
    }
}