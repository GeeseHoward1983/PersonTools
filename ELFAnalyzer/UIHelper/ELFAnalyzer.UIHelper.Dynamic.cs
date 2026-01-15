using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.Text;

namespace PersonalTools.ELFAnalyzer
{
    public partial class ELFAnalyzer
    {
        private string GetDynamicSectionInfoTableEntryValue(ELFDynamic entry)
        {
            string value = string.Empty;
            var strTabAddr = GetStringValueFromDynamicEntries(DynamicTag.DT_STRTAB);
            var strTabSize = GetStringValueFromDynamicEntries(DynamicTag.DT_STRSZ);

            if (strTabAddr != 0 && strTabSize != 0 && _parser.SectionHeaders != null)
            {
                // Find the section that contains the string table
                var stringTableSection = FindSectionByAddress(strTabAddr);
                if (stringTableSection != null)
                {
                    value = ReadStringFromSection(stringTableSection, entry.d_val) ?? $"0x{entry.d_val:x}";
                }
            }
            return value;

        }
        private string GetDynamicSectionInfoValue(ELFDynamic entry)
        {
            return (entry.d_tag) switch
            {
                (long)DynamicTag.DT_NEEDED => $"{GetDynamicSectionInfoTableEntryValue(entry)}",
                (long)DynamicTag.DT_SONAME => $"{GetDynamicSectionInfoTableEntryValue(entry)}",
                (long)DynamicTag.DT_RPATH => $"{GetDynamicSectionInfoTableEntryValue(entry)}",
                (long)DynamicTag.DT_RUNPATH => $"{GetDynamicSectionInfoTableEntryValue(entry)}",
                (long)DynamicTag.DT_FLAGS => $"{ELFDynamicInfo.GetDynamicFlagDescription((uint)entry.d_val)}",
                (long)DynamicTag.DT_FLAGS_1 => $"{ELFDynamicInfo.GetDynamicFlag1Description((uint)entry.d_val)}",
                _ => entry.d_tag == (long)DynamicTag.DT_PLTREL ? ELFParserUtils.GetTypeName(typeof(DynamicTag), entry.d_val, "") : $"0x{entry.d_val:x}"
            };
        }

        public List<ELFDynamicSectionInfo> GetDynamicSectionInfoList()
        {
            var result = new List<ELFDynamicSectionInfo>();

            if (_parser.DynamicEntries != null)
            {
                foreach (var entry in _parser.DynamicEntries)
                {
                    result.Add(new ELFDynamicSectionInfo
                    {
                        Tag = $"0x{entry.d_tag:x16}",
                        Type = ELFDynamicInfo.GetDynamicTagDescription((ulong)entry.d_tag),
                        Value = GetDynamicSectionInfoValue(entry)
                    });
                    if(entry.d_tag == (long)DynamicTag.DT_NULL)
                    {
                        break;
                    }
                }
            }

            return result;
        }
                
        private ulong GetStringValueFromDynamicEntries(DynamicTag tag)
        {
            if (_parser.DynamicEntries != null)
            {
                var entry = _parser.DynamicEntries.FirstOrDefault(e => e.d_tag == (long)tag);
                if (entry.d_tag != 0)
                {
                    return entry.d_val;
                }
            }
            return 0;
        }
                
        private Models.ELFSectionHeader? FindSectionByAddress(ulong address)
        {
            if (_parser.SectionHeaders != null)
            {
                foreach (var section in _parser.SectionHeaders)
                {
                    if (section.sh_addr == address)
                    {
                        return section;
                    }
                }
            }
            return null;
        }
                
        private string? ReadStringFromSection(Models.ELFSectionHeader? section, ulong offset)
        {
            try
            {
                if (section != null && _parser.FileData != null && 
                    offset < (ulong)_parser.FileData.Length && 
                    section.Value.sh_offset + offset < (ulong)_parser.FileData.Length)
                {
                    var start = (int)(section.Value.sh_offset + offset);
                    var end = start;
                    while (end < _parser.FileData.Length && _parser.FileData[end] != 0)
                    {
                        end++;
                    }
                    
                    if (end > start)
                    {
                        return Encoding.UTF8.GetString(_parser.FileData, start, end - start);
                    }
                }
            }
            catch
            {
                // If there's an error reading the string, return null
            }
            return null;
        }
    }
}