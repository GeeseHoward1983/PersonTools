using PersonalTools.Enums;
using System.IO;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    public class ELFNoteInfo
    {
        public static string GetFormattedNotesInfo(ELFParser parser)
        {
            var sb = new StringBuilder();

            if(sb.Length == 0) 
            // 检查节头中的Note节
            if (parser.SectionHeaders != null)
            {
                for (int i = 0; i < parser.SectionHeaders.Count; i++)
                {
                    if (parser.SectionHeaders[i].sh_type == (uint)SectionType.SHT_NOTE)
                    {
                        var noteInfo = ParseNoteSection(parser, parser.SectionHeaders[i]);
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

        private static string GetNoteDescription(ELFParser parser, ulong offset, ulong size)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Displaying notes found at file offset 0x{offset:x8} with length 0x{size:x8}:");
            sb.AppendLine("  Owner             Data size            Description");
            ulong endOffset = offset + size;

            while (offset < endOffset)
            {
                // Note结构：namesz, descsz, type
                bool isLittleEndian = parser.Header.EI_DATA == (byte)ELFData.ELFDATA2LSB;
                uint namesz = ELFParserUtils.ReadUInt32(new BinaryReader(new MemoryStream(parser.FileData, (int)offset, Math.Min((int)(endOffset - offset), parser.FileData.Length - (int)offset))), isLittleEndian);
                uint descsz = ELFParserUtils.ReadUInt32(new BinaryReader(new MemoryStream(parser.FileData, (int)offset + 4, Math.Min((int)(endOffset - offset - 4), parser.FileData.Length - (int)offset - 4))), isLittleEndian);
                uint type = ELFParserUtils.ReadUInt32(new BinaryReader(new MemoryStream(parser.FileData, (int)offset + 8, Math.Min((int)(endOffset - offset - 8), parser.FileData.Length - (int)offset - 8))), isLittleEndian);

                ulong nameOffset = offset + 12;
                string owner = ELFParserUtils.ExtractStringFromBytes(parser.FileData, (int)nameOffset, (int)namesz);

                ulong descOffset = nameOffset + namesz;
                // 修复对齐方式 - 确保对齐到4字节边界
                if (parser.Is64Bit)
                {
                    if (descOffset % 8 != 0) descOffset = (descOffset + 7) & ~7UL; // 对齐 (64位)
                }
                else
                {
                    if (descOffset % 4 != 0) descOffset = (descOffset + 3) & ~3UL; // 对齐
                }
                string noteInfo = ProcessNoteEntry(type, owner, parser.FileData, (int)descOffset, (int)descsz);
                if (!string.IsNullOrEmpty(noteInfo))
                {
                    sb.AppendLine($"  {owner,-18}0x{descsz:x8}           {noteInfo}");
                }

                // 计算下一个note的偏移
                ulong nextOffset = descOffset + descsz;
                if (parser.Is64Bit)
                {
                    if (nextOffset % 8 != 0) nextOffset = (nextOffset + 7) & ~7UL; // 对齐 (64位)
                }
                else
                {
                    if (nextOffset % 4 != 0) nextOffset = (nextOffset + 3) & ~3UL; // 对齐
                }
                offset = nextOffset;
            }

            return sb.ToString();

        }

        private static string ParseNoteSection(ELFParser parser, Models.ELFSectionHeader section)
        {
            return GetNoteDescription(parser, section.sh_offset, section.sh_size);
        }

        private static string GetABIVersion(byte[] data, int descOffset, int descSize)
        {
            return descSize >= 16 ? $"(ABI version: {BitConverter.ToUInt32(data, descOffset)}.{BitConverter.ToUInt32(data, descOffset + 4)}.{BitConverter.ToUInt32(data, descOffset + 8)}.{BitConverter.ToUInt32(data, descOffset + 12)})" : "";
        }

        private static string GetBuildID(byte[] data, int descOffset, int descSize)
        {
            return descSize >= 20 ? $"(NT_GNU_BUILD_ID (unique build ID bitstring)\n    Build ID: {Utils.ToHexString(data, descOffset, descSize)}" : "";
        }

        private static string ProcessNoteEntry(uint type, string owner, byte[] data, int descOffset, int descSize)
        {
            string description = GetNoteDescription(type, owner);

            return owner switch
            {
                "GNU" => type switch
                {
                    1 => $"{description} {GetABIVersion(data, descOffset, descSize)}",
                    2 => $"{description}",
                    3 => $"{GetBuildID(data, descOffset, descSize)}",
                    4 => $"{description} (gold version)\n    Version: gold {ELFParserUtils.ExtractStringFromBytes(data, descOffset)}",
                    5 => $"{description}",
                    _ => $"{description}"
                },
                "Android" => type switch
                {
                    1 => $"{description} (版本)",
                    _ => $"Unknown GNU note type {type}"
                },
                _ => $"{description} (type: {type})"
            };
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
                    _ => $"Unknown GNU note type {type}"
                },
                "CC" => "Compiler Info",
                _ => $"type 0x{type:x}"
            };
        }
    }
}