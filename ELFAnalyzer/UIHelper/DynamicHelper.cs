using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.Enums;
using PersonalTools.ELFAnalyzer.Models;

namespace PersonalTools.ELFAnalyzer.UIHelper
{
    internal static class DynamicHelper
    {
        private static string GetDynamicSectionInfoTableEntryValue(ELFParser _parser, ELFDynamic entry)
        {
            string value = string.Empty;
            ulong strTabAddr = GetStringValueFromDynamicEntries(_parser, DynamicTag.DT_STRTAB);
            ulong strTabSize = GetStringValueFromDynamicEntries(_parser, DynamicTag.DT_STRSZ);

            if (strTabAddr != 0 && strTabSize != 0 && _parser.SectionHeaders != null)
            {
                // Find the section that contains the string table
                ELFSectionHeader? stringTableSection = ELFParserUtils.FindSectionByAddress(_parser, strTabAddr);
                if (stringTableSection != null)
                {
                    value = ReadStringFromSection(_parser, stringTableSection, entry.d_val);
                }
            }
            return value;

        }
        private static string GetDynamicSectionInfoValue(ELFParser _parser, ELFDynamic entry)
        {
            return entry.d_tag switch
            {
                (long)DynamicTag.DT_NEEDED or (long)DynamicTag.DT_SONAME or (long)DynamicTag.DT_RPATH or (long)DynamicTag.DT_RUNPATH => $"{GetDynamicSectionInfoTableEntryValue(_parser, entry)}",
                (long)DynamicTag.DT_FLAGS or (long)DynamicTag.DT_FLAGS_1 => $"{ELFDynamicInfo.GetDynamicFlagDescription((uint)entry.d_val)}",
                _ => entry.d_tag == (long)DynamicTag.DT_PLTREL ? ELFParserUtils.GetTypeName(typeof(DynamicTag), entry.d_val, "") : $"0x{entry.d_val:x}"
            };
        }

        internal static List<ELFDynamicSectionInfo> GetDynamicSectionInfoList(ELFParser Parser)
        {
            List<ELFDynamicSectionInfo> result = [];

            if (Parser.DynamicEntries != null)
            {
                foreach (ELFDynamic entry in Parser.DynamicEntries)
                {
                    result.Add(new ELFDynamicSectionInfo
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

        private static string ReadStringFromSection(ELFParser _parser, ELFSectionHeader? section, ulong offset)
        {
            // 用减法式校验避免 sh_offset + offset 两个 ulong 相加回绕越过 < Length 检查
            // （offset 来自不可信 d_val，可被构造为巨值令和回绕到小值绕过边界）
            if (section != null && _parser.FileData != null
                && section.Value.sh_offset < (ulong)_parser.FileData.Length
                && offset < (ulong)_parser.FileData.Length - section.Value.sh_offset)
            {
                int start = (int)(section.Value.sh_offset + offset);
                string value = ELFParserUtils.ExtractStringFromBytes(_parser.FileData, start);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            return $"0x{offset:x}";
        }
    }
}