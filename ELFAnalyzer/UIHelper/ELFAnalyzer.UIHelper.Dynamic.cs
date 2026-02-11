using PersonalTools.ELFAnalyzer.Core;
using ELFModels = PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.Text;
using PersonalTools.ELFAnalyzer.Models;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    public class DynamicHelper
    {
        private static string GetDynamicSectionInfoTableEntryValue(ELFParser _parser, ELFModels.ELFDynamic entry)
        {
            string value = string.Empty;
            ulong strTabAddr = GetStringValueFromDynamicEntries(_parser, DynamicTag.DT_STRTAB);
            ulong strTabSize = GetStringValueFromDynamicEntries(_parser, DynamicTag.DT_STRSZ);

            if (strTabAddr != 0 && strTabSize != 0 && _parser.SectionHeaders != null)
            {
                // Find the section that contains the string table
                ELFModels.ELFSectionHeader? stringTableSection = FindSectionByAddress(_parser, strTabAddr);
                if (stringTableSection != null)
                {
                    value = ReadStringFromSection(_parser, stringTableSection, entry.d_val);
                }
            }
            return value;

        }
        private static string GetDynamicSectionInfoValue(ELFParser _parser, ELFModels.ELFDynamic entry)
        {
            return entry.d_tag switch
            {
                (long)DynamicTag.DT_NEEDED or (long)DynamicTag.DT_SONAME or (long)DynamicTag.DT_RPATH or (long)DynamicTag.DT_RUNPATH => $"{GetDynamicSectionInfoTableEntryValue(_parser, entry)}",
                (long)DynamicTag.DT_FLAGS or (long)DynamicTag.DT_FLAGS_1 => $"{ELFDynamicInfo.GetDynamicFlagDescription((uint)entry.d_val)}",
                _ => entry.d_tag == (long)DynamicTag.DT_PLTREL ? ELFParserUtils.GetTypeName(typeof(DynamicTag), entry.d_val, "") : $"0x{entry.d_val:x}"
            };
        }

        public static List<ELFModels.ELFDynamicSectionInfo> GetDynamicSectionInfoList(ELFParser Parser)
        {
            List<ELFDynamicSectionInfo> result = [];

            if (Parser.DynamicEntries != null)
            {
                foreach (ELFDynamic entry in Parser.DynamicEntries)
                {
                    result.Add(new ELFModels.ELFDynamicSectionInfo
                    {
                        Tag = $"0x{entry.d_tag:x16}",
                        Type = ELFDynamicInfo.GetDynamicTagDescription((ulong)entry.d_tag),
                        Value = GetDynamicSectionInfoValue(Parser, entry)
                    });
                    if (entry.d_tag == (long)DynamicTag.DT_NULL)
                    {
                        break;
                    }
                }
            }

            return result;
        }

        private static ulong GetStringValueFromDynamicEntries(ELFParser _parser, DynamicTag tag)
        {
            if (_parser.DynamicEntries != null)
            {
                ELFDynamic entry = _parser.DynamicEntries.FirstOrDefault(e => e.d_tag == (long)tag);
                if (entry.d_tag != 0)
                {
                    return entry.d_val;
                }
            }
            return 0;
        }

        private static ELFModels.ELFSectionHeader? FindSectionByAddress(ELFParser _parser, ulong address)
        {
            if (_parser.SectionHeaders != null)
            {
                foreach (ELFModels.ELFSectionHeader section in _parser.SectionHeaders)
                {
                    if (section.sh_addr == address)
                    {
                        return section;
                    }
                }
            }
            return null;
        }

        private static string ReadStringFromSection(ELFParser _parser, ELFModels.ELFSectionHeader? section, ulong offset)
        {
            try
            {
                if (section != null && _parser.FileData != null &&
                    offset < (ulong)_parser.FileData.Length &&
                    section.Value.sh_offset + offset < (ulong)_parser.FileData.Length)
                {
                    int start = (int)(section.Value.sh_offset + offset);
                    int end = start;
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
            return $"0x{offset:x}";
        }
    }
}