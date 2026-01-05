using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.ELFAnalyzer.Models;
using System.Text;

namespace PersonalTools.ELFAnalyzer
{
    public partial class ELFAnalyzer
    {
        public List<ELFDynamicSectionInfo> GetDynamicSectionInfoList()
        {
            var result = new List<ELFDynamicSectionInfo>();

            if (_parser.DynamicEntries32 != null && _parser.DynamicEntries32.Count > 0)
            {
                foreach (var entry in _parser.DynamicEntries32)
                {
                    var tag = ELFParser.GetDynamicTagDescription(entry.d_tag);
                    string value = string.Empty;

                    // Handle entries that refer to string table
                    if (IsStringTableEntry(entry.d_tag))
                    {
                        // Find the string table to resolve the name
                        var strTabAddr = GetStringValueFromDynamicEntries(DynamicTag.DT_STRTAB);
                        var strTabSize = GetStringValueFromDynamicEntries(DynamicTag.DT_STRSZ);

                        if (strTabAddr != 0 && strTabSize != 0 && _parser.SectionHeaders32 != null)
                        {
                            // Find the section that contains the string table
                            var stringTableSection = FindSectionByAddress(strTabAddr);
                            if (stringTableSection != null)
                            {
                                value = ReadStringFromSection(stringTableSection, (uint)entry.d_val) ?? $"0x{entry.d_val:x}";
                            }
                        }
                    }
                    else if (entry.d_tag == (long)DynamicTag.DT_FLAGS)
                    {
                        value = ELFParser.GetDynamicFlagDescription(entry.d_val);
                    }
                    else
                    {
                        value = $"0x{entry.d_val:x}";
                    }

                    result.Add(new ELFDynamicSectionInfo
                    {
                        Tag = $"0x{entry.d_tag:x16}",
                        Type = tag,
                        Value = value
                    });
                }
            }
            else if (_parser.DynamicEntries64 != null && _parser.DynamicEntries64.Count > 0)
            {
                foreach (var entry in _parser.DynamicEntries64)
                {
                    var tag = ELFParser.GetDynamicTagDescription(entry.d_tag);
                    string value = string.Empty;

                    // Handle entries that refer to string table
                    if (IsStringTableEntry(entry.d_tag))
                    {
                        // Find the string table to resolve the name
                        var strTabAddr = GetStringValueFromDynamicEntries64(DynamicTag.DT_STRTAB);
                        var strTabSize = GetStringValueFromDynamicEntries64(DynamicTag.DT_STRSZ);

                        if (strTabAddr != 0 && strTabSize != 0 && _parser.SectionHeaders64 != null)
                        {
                            // Find the section that contains the string table
                            var stringTableSection = FindSectionByAddress64(strTabAddr);
                            if (stringTableSection != null)
                            {
                                value = ReadStringFromSection64(stringTableSection, (ulong)entry.d_val) ?? $"0x{entry.d_val:x}";
                            }
                        }
                    }
                    else if (entry.d_tag == (long)DynamicTag.DT_FLAGS)
                    {
                        value = ELFParser.GetDynamicFlagDescription((uint)entry.d_val);
                    }
                    else
                    {
                        value = $"0x{entry.d_val:x}";
                    }

                    result.Add(new ELFDynamicSectionInfo
                    {
                        Tag = $"0x{entry.d_tag:x16}",
                        Type = tag,
                        Value = value
                    });
                    if(entry.d_tag == 0)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        private static bool IsStringTableEntry(long tag)
        {
            return tag == (long)DynamicTag.DT_NEEDED || 
                   tag == (long)DynamicTag.DT_SONAME || 
                   tag == (long)DynamicTag.DT_RPATH || 
                   tag == (long)DynamicTag.DT_RUNPATH;
        }
        
        private uint GetStringValueFromDynamicEntries(DynamicTag tag)
        {
            if (_parser.DynamicEntries32 != null)
            {
                var entry = _parser.DynamicEntries32.FirstOrDefault(e => e.d_tag == (long)tag);
                if (entry.d_tag != 0)
                {
                    return entry.d_val;
                }
            }
            return 0;
        }
        
        private ulong GetStringValueFromDynamicEntries64(DynamicTag tag)
        {
            if (_parser.DynamicEntries64 != null)
            {
                var entry = _parser.DynamicEntries64.FirstOrDefault(e => e.d_tag == (long)tag);
                if (entry.d_tag != 0)
                {
                    return (ulong)entry.d_val;
                }
            }
            return 0;
        }
        
        private ELFSectionHeader32? FindSectionByAddress(ulong address)
        {
            if (_parser.SectionHeaders32 != null)
            {
                foreach (var section in _parser.SectionHeaders32)
                {
                    if (section.sh_addr == address)
                    {
                        return section;
                    }
                }
            }
            return null;
        }
        
        private ELFSectionHeader64? FindSectionByAddress64(ulong address)
        {
            if (_parser.SectionHeaders64 != null)
            {
                foreach (var section in _parser.SectionHeaders64)
                {
                    if (section.sh_addr == address)
                    {
                        return section;
                    }
                }
            }
            return null;
        }
        
        private string? ReadStringFromSection(ELFSectionHeader32? section, uint offset)
        {
            try
            {
                if (section != null && _parser.FileData != null && offset < _parser.FileData.Length && section.Value.sh_offset + offset < _parser.FileData.Length)
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
        
        private string? ReadStringFromSection64(ELFSectionHeader64? section, ulong offset)
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