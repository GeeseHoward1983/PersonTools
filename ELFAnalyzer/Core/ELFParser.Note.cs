using PersonalTools.Enums;
using System.Globalization;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFNoteInfo
    {
        internal static string GetFormattedNotesInfo(ELFParser parser)
        {
            StringBuilder sb = new();

            if (sb.Length == 0)
            {
                // 检查节头中的Note节
                if (parser.SectionHeaders != null)
                {
                    for (int i = 0; i < parser.SectionHeaders.Count; i++)
                    {
                        if (parser.SectionHeaders[i].sh_type == (uint)SectionType.SHT_NOTE)
                        {
                            string noteInfo = ParseNoteSection(parser, parser.SectionHeaders[i]);
                            if (!string.IsNullOrEmpty(noteInfo))
                            {
                                sb.AppendLine(noteInfo);
                            }
                        }
                    }
                }
            }

            if (sb.Length == 0)
            {
                sb.AppendLine("No note segments or sections found.");
            }

            return sb.ToString();
        }

        private static string GetNoteDescription(ELFParser parser, ulong offset, ulong size)
        {
            StringBuilder sb = new();
            sb.AppendLine(CultureInfo.InvariantCulture, $"Displaying notes found at file offset 0x{offset:x8} with length 0x{size:x8}:");
            sb.AppendLine("  Owner             Data size            Description");

            bool isLittleEndian = parser.Header.IsLittleEndian();
            bool is64Bit = parser.Is64Bit;
            ulong endOffset = offset + size;

            while (offset < endOffset)
            {
                // Note结构：namesz, descsz, type
                uint namesz = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset, isLittleEndian);
                uint descsz = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset + 4, isLittleEndian);
                uint type = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset + 8, isLittleEndian);

                ulong nameOffset = offset + 12;
                string owner = ELFParserUtils.ExtractStringFromBytes(parser.FileData, (int)nameOffset, (int)namesz);

                ulong descOffset = AlignNoteOffset(nameOffset + namesz, is64Bit);
                string noteInfo = ProcessNoteEntry(parser, type, owner, parser.FileData, (int)descOffset, (int)descsz);
                if (!string.IsNullOrEmpty(noteInfo))
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  {owner,-18}0x{descsz:x8}           {noteInfo}");
                }

                // 推进到下一个 note（按位宽对齐）
                offset = AlignNoteOffset(descOffset + descsz, is64Bit);
            }

            return sb.ToString();
        }

        // note 偏移按位宽对齐（64位→8字节边界，32位→4字节边界）；已对齐时返回原值
        private static ulong AlignNoteOffset(ulong value, bool is64Bit)
        {
            return is64Bit ? (value + 7) & ~7UL : (value + 3) & ~3UL;
        }

        private static string ParseNoteSection(ELFParser parser, Models.ELFSectionHeader section)
        {
            return GetNoteDescription(parser, section.sh_offset, section.sh_size);
        }

        private static string GetABIVersion(ELFParser parser, byte[] data, int descOffset, int descSize)
        {
            if (descSize < 16)
            {
                return "";
            }

            bool isLittleEndian = parser.Header.IsLittleEndian();
            uint v0 = ELFParserUtils.ReadUInt32(data, descOffset, isLittleEndian);
            uint v1 = ELFParserUtils.ReadUInt32(data, descOffset + 4, isLittleEndian);
            uint v2 = ELFParserUtils.ReadUInt32(data, descOffset + 8, isLittleEndian);
            uint v3 = ELFParserUtils.ReadUInt32(data, descOffset + 12, isLittleEndian);
            return $"(ABI version: {v0}.{v1}.{v2}.{v3})";
        }

        private static string GetBuildID(byte[] data, int descOffset, int descSize)
        {
            return descSize >= 20 ? $"(NT_GNU_BUILD_ID (unique build ID bitstring)\n    Build ID: {Utils.ToHexString(data, descOffset, descSize)}" : "";
        }

        private static string ProcessNoteEntry(ELFParser parser, uint type, string owner, byte[] data, int descOffset, int descSize)
        {
            string description = GetNoteDescription(type, owner);

            return owner switch
            {
                "GNU" => type switch
                {
                    1 => $"{description} {GetABIVersion(parser, data, descOffset, descSize)}",
                    2 => $"{description}",
                    3 => $"{GetBuildID(data, descOffset, descSize)}",
                    4 => $"{description} (gold version)\n    Version: gold {ELFParserUtils.ExtractStringFromBytes(data, descOffset)}",
                    5 => $"{description}",
                    _ => $"{description}"
                },
                "Android" => type switch
                {
                    1 => $"{description} (version)\n   description data: {FormatNoteDescriptionData(data, descOffset, descSize)}",
                    _ => $"Unknown Android note type {type}"
                },
                _ => $"{description} (type: {type})"
            };
        }

        // 以空格分隔的小写十六进制输出 note 描述数据（与 readelf "description data:" 一致）
        private static string FormatNoteDescriptionData(byte[] data, int descOffset, int descSize)
        {
            if (descOffset < 0 || descSize <= 0 || descOffset + descSize > data.Length)
            {
                return string.Empty;
            }

            StringBuilder sb = new(descSize * 3);
            for (int i = 0; i < descSize; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(data[descOffset + i].ToString("x2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        private static string GetNoteDescription(uint type, string owner)
        {
            return owner switch
            {
                "GNU" => type switch
                {
                    0 => "NT_GNU_ABI_TAG",
                    1 => "NT_GNU_HWCAP",
                    2 => "NT_GNU_BUILD_ID",
                    3 => "NT_GNU_GOLD_VERSION",
                    4 => "NT_GNU_BUILD_ATTRIBUTE",
                    _ => $"Unknown GNU note type {type}"
                },
                "Android" => type switch
                {
                    1 => "NT_VERSION",
                    _ => $"Unknown Android note type {type}"
                },
                "CC" => "Compiler Info",
                _ => $"type 0x{type:x}"
            };
        }
    }
}