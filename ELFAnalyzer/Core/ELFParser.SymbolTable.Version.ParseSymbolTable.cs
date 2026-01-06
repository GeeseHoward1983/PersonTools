using PersonalTools.ELFAnalyzer.Models;
using System.IO;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        private void ParseVersionSymbolTable()
        {
            // 查找版本符号表 (DT_VERSYM)
            long versymAddr = 0;

            if (_dynamicEntries != null)
            {
                foreach (var entry in _dynamicEntries)
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
                var versymSection = FindSectionByAddress((ulong)versymAddr);
                if (versymSection != null)
                {
                    var data = new byte[versymSection.Value.sh_size];
                    Array.Copy(_fileData, (long)versymSection.Value.sh_offset, data, 0, (int)versymSection.Value.sh_size);

                    int count = (int)(versymSection.Value.sh_size / 2); // 每个版本符号是2字节
                    _versionSymbols = new ushort[count];

                    for (int i = 0; i < count; i++)
                    {
                        _versionSymbols[i] = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ?
                            BitConverter.ToUInt16(data, i * 2) :
                            (ushort)((data[i * 2 + 1] << 8) | data[i * 2]);
                    }
                }
            }
        }
    }
}