using PersonalTools.Enums;
using PersonalTools.Utils;
using System.Globalization;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFNoteInfo
    {
        internal static string GetFormattedNotesInfo(ELFParser parser)
        {
            StringBuilder sb = new();

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

            if (sb.Length == 0)
            {
                sb.AppendLine("No note segments or sections found.");
            }

            return sb.ToString();
        }

        private static string FormatNoteSection(ELFParser parser, ulong offset, ulong size)
        {
            StringBuilder sb = new();
            sb.AppendLine(CultureInfo.InvariantCulture, $"Displaying notes found at file offset 0x{offset:x8} with length 0x{size:x8}:");
            sb.AppendLine("  Owner             Data size            Description");

            bool isLittleEndian = parser.Header.IsLittleEndian();
            ulong endOffset = Math.Min(offset + size, (ulong)parser.FileData.Length); // 夹紧到文件实际长度

            while (offset + 12 <= endOffset) // 至少能读完 namesz/descsz/type 三个字段
            {
                // Note结构：namesz, descsz, type
                uint namesz = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset, isLittleEndian);
                uint descsz = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset + 4, isLittleEndian);
                uint type = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset + 8, isLittleEndian);

                ulong nameOffset = offset + 12;
                string owner = ELFParserUtils.ExtractStringFromBytes(parser.FileData, (int)nameOffset, (int)namesz);

                ulong descOffset = AlignNoteOffset(nameOffset + namesz);
                // descOffset/descsz 超出文件范围时跳过描述解析（owner 仍有效）：namesz 为 uint 可使 descOffset 超 int.MaxValue，
                // 既避免 (int) 截断错位，也堵住 GetABIVersion/GetBuildID 等对 FileData 的越界读取
                string noteInfo = descOffset + descsz <= (ulong)parser.FileData.Length
                    ? ProcessNoteEntry(parser, type, owner, parser.FileData, (int)descOffset, (int)descsz)
                    : string.Empty;
                if (!string.IsNullOrEmpty(noteInfo))
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  {owner,-18}0x{descsz:x8}           {noteInfo}");
                }

                // 推进到下一个 note（按位宽对齐）；不前进或回绕则停止，避免死循环
                ulong next = AlignNoteOffset(descOffset + descsz);
                if (next <= offset)
                {
                    break;
                }

                offset = next;
            }

            return sb.ToString();
        }

        // ELF note 的 name/desc 一律按 4 字节对齐：Linux/glibc/binutils 及 readelf 对 ELF32 与 ELF64
        // 均按 4 字节(不随 ELFCLASS 变为 8)。此前 64 位按 8 字节对齐会使多 note 段(core dump、
        // build-id 后接其它 note)从第二个 note 起错位。已对齐时返回原值。
        private static ulong AlignNoteOffset(ulong value)
        {
            return (value + 3) & ~3UL;
        }

        private static string ParseNoteSection(ELFParser parser, Models.ELFSectionHeader section)
        {
            return FormatNoteSection(parser, section.sh_offset, section.sh_size);
        }

        private static string GetABIVersion(ELFParser parser, byte[] data, int descOffset, int descSize)
        {
            if (descSize < 16)
            {
                return "";
            }

            // NT_GNU_ABI_TAG desc 为 4 个字：word0 是 OS 描述符(0=Linux 等)，word1-3 才是最低 ABI 版本，
            // 与 readelf 输出 "OS: Linux, ABI: 2.6.32" 对齐；不能把 OS 并入版本号
            bool isLittleEndian = parser.Header.IsLittleEndian();
            uint os = ELFParserUtils.ReadUInt32(data, descOffset, isLittleEndian);
            uint v1 = ELFParserUtils.ReadUInt32(data, descOffset + 4, isLittleEndian);
            uint v2 = ELFParserUtils.ReadUInt32(data, descOffset + 8, isLittleEndian);
            uint v3 = ELFParserUtils.ReadUInt32(data, descOffset + 12, isLittleEndian);
            string osName = os switch
            {
                0 => "Linux",
                1 => "Hurd",
                2 => "Solaris",
                3 => "FreeBSD",
                4 => "NetBSD",
                5 => "Syllable",
                6 => "NaCl",
                _ => $"Unknown ({os})",
            };
            return $"(OS: {osName}, ABI: {v1}.{v2}.{v3})";
        }

        private static string GetBuildID(byte[] data, int descOffset, int descSize)
        {
            // GNU build-id 长度不固定（md5=16 / sha1=20 / uuid 等），硬性要求 20 字节会静默丢弃合法的
            // 16 字节 build-id。仅用一个极小下限做健壮性兜底，实际输出长度由 descSize 决定。
            if (descSize < 4)
            {
                return "";
            }

            // descSize 受文件大小约束但畸形 note 仍可能很大：对 build-id hex 设显示上限，
            // 与 FormatNoteDescriptionData 的 64KB 上限一致，避免为巨型 note 分配约 2×descSize 的字符串。
            const int MaxBuildIdBytes = 64 * 1024;
            int displayCount = Math.Min(descSize, MaxBuildIdBytes);
            string hex = ConvertUtils.ToHexString(data, descOffset, displayCount);
            string ellipsis = displayCount < descSize ? "..." : "";
            return $"NT_GNU_BUILD_ID (unique build ID bitstring)\n    Build ID: {hex}{ellipsis}";
        }

        private static string ProcessNoteEntry(ELFParser parser, uint type, string owner, byte[] data, int descOffset, int descSize)
        {
            string description = GetNoteDescription(type, owner);

            return owner switch
            {
                "GNU" => type switch
                {
                    1 => $"{description} {GetABIVersion(parser, data, descOffset, descSize)}".TrimEnd(),
                    2 => $"{description}",
                    3 => $"{GetBuildID(data, descOffset, descSize)}",
                    4 => $"{description} (gold version)\n    Version: gold {ELFParserUtils.ExtractStringFromBytes(data, descOffset, descSize)}",
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

            // descSize 受文件大小约束但仍可能很大：对 hex-dump 设显示上限。既防 descSize*3 的 int 溢出
            // （descSize>~715M 时溢出为负，StringBuilder(负数) 抛未捕获 ArgumentOutOfRangeException），
            // 也避免为畸形巨型 note 分配约 3×descSize 的字符串。
            const int MaxDisplayBytes = 64 * 1024;
            int displayCount = Math.Min(descSize, MaxDisplayBytes);
            StringBuilder sb = new((displayCount * 3) + 4);
            for (int i = 0; i < displayCount; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(data[descOffset + i].ToString("x2", CultureInfo.InvariantCulture));
            }
            if (displayCount < descSize)
            {
                sb.Append(" …");
            }
            return sb.ToString();
        }

        private static string GetNoteDescription(uint type, string owner)
        {
            return owner switch
            {
                "GNU" => type switch
                {
                    1 => "NT_GNU_ABI_TAG",
                    2 => "NT_GNU_HWCAP",
                    3 => "NT_GNU_BUILD_ID",
                    4 => "NT_GNU_GOLD_VERSION",
                    5 => "NT_GNU_PROPERTY_TYPE_0",
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