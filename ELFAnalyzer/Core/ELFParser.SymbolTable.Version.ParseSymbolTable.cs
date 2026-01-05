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
            
            if (_is64Bit)
            {
                if (_dynamicEntries64 != null)
                {
                    foreach (var entry in _dynamicEntries64)
                    {
                        if (entry.d_tag == (long)DynamicTag.DT_VERSYM)
                        {
                            versymAddr = (long)entry.d_val;
                        }
                    }
                }
            }
            else
            {
                if (_dynamicEntries32 != null)
                {
                    foreach (var entry in _dynamicEntries32)
                    {
                        if (entry.d_tag == (long)DynamicTag.DT_VERSYM)
                        {
                            versymAddr = entry.d_val;
                        }
                    }
                }
            }

            // 查找对应的节头
            if (versymAddr > 0)
            {
                if (_is64Bit)
                {
                    var versymSection = FindSectionByAddress64((ulong)versymAddr);
                    if (versymSection != null)
                    {
                        var data = new byte[versymSection.Value.sh_size];
                        Array.Copy(_fileData, (long)versymSection.Value.sh_offset, data, 0, (int)versymSection.Value.sh_size);
                        
                        int count = (int)(versymSection.Value.sh_size / 2); // 每个版本符号是2字节
                        _versionSymbols64 = new ushort[count];
                        
                        for (int i = 0; i < count; i++)
                        {
                            _versionSymbols64[i] = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? 
                                BitConverter.ToUInt16(data, i * 2) : 
                                (ushort)((data[i * 2 + 1] << 8) | data[i * 2]);
                        }
                    }
                }
                else
                {
                    var versymSection = FindSectionByAddress32((uint)versymAddr);
                    if (versymSection != null)
                    {
                        var data = new byte[versymSection.Value.sh_size];
                        Array.Copy(_fileData, versymSection.Value.sh_offset, data, 0, (int)versymSection.Value.sh_size);
                        
                        int count = (int)(versymSection.Value.sh_size / 2); // 每个版本符号是2字节
                        _versionSymbols32 = new ushort[count];
                        
                        for (int i = 0; i < count; i++)
                        {
                            _versionSymbols32[i] = _header.EI_DATA == (byte)ELFData.ELFDATA2LSB ? 
                                BitConverter.ToUInt16(data, i * 2) : 
                                (ushort)((data[i * 2 + 1] << 8) | data[i * 2]);
                        }
                    }
                }
            }
        }
    }
}