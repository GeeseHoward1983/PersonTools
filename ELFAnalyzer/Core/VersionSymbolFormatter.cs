using PersonalTools.Enums;
using System.Globalization;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class VersionSymbolFormatter
    {
        private static Models.ELFSectionHeader? GetSection(ELFParser parser, string objSectionName)
        {
            if (parser.SectionHeaders != null && parser.VersionSymbols != null)
            {
                for (int i = 0; i < parser.SectionHeaders.Count; i++)
                {
                    if (SymbolName.GetSectionName(parser, i) == objSectionName)
                    {
                        return parser.SectionHeaders[i];
                    }
                }
            }
            return null;

        }
        internal static string GetFormattedVersionSymbolInfo(ELFParser parser)
        {
            StringBuilder sb = new();

            // 首先检查是否存在版本符号表
            Models.ELFSectionHeader? verSymSection = GetSection(parser, ".gnu.version");
            if (verSymSection == null)
            {
                sb.AppendLine("Version symbols section '.gnu.version' not found or empty.");
                return sb.ToString();
            }

            if (parser.VersionSymbols != null && parser.Symbols != null)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"Version symbols section '.gnu.version' contains {parser.VersionSymbols.Length} entries:");
                sb.AppendLine(CultureInfo.InvariantCulture, $"  地址: 0x{verSymSection.Value.sh_addr:x16}  Offset: 0x{verSymSection.Value.sh_offset:x6}  Link: {verSymSection.Value.sh_link} (.dynsym)");

                // 每行显示4个版本符号
                for (int i = 0; i < parser.VersionSymbols.Length; i++)
                {
                    sb.Append(CultureInfo.InvariantCulture, $" {i:x3}:");
                    ushort versionIndex = (ushort)(parser.VersionSymbols[i] & 0x7fff);
                    string versionInfo = GetVersionInfoByIndex(parser, versionIndex);
                    sb.Append(CultureInfo.InvariantCulture, $" {versionIndex:D3} ({versionInfo})");
                    if ((i & 0x3) == 0x3)
                    {
                        sb.AppendLine();
                    }
                }
            }

            return sb.ToString();
        }

        internal static string GetFormattedVersionDependencyInfo(ELFParser parser)
        {
            StringBuilder sb = new();

            // 检查是否存在版本需求表
            Models.ELFSectionHeader? verNeedSection = GetSection(parser, ".gnu.version_r");

            if (verNeedSection != null && parser.VersionDependencies != null)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"Version needs section '.gnu.version_r' contains {verNeedSection.Value.sh_info} entries:");
                sb.AppendLine(CultureInfo.InvariantCulture, $"  地址: 0x{verNeedSection.Value.sh_addr:x16}  Offset: 0x{verNeedSection.Value.sh_offset:x8}  Link: {verNeedSection.Value.sh_link} (.dynstr)");

                // 这里需要实际解析版本需求表的内容
                ParseAndAppendVersionNeeds(parser, verNeedSection.Value, sb);
            }

            return sb.ToString();
        }

        internal static string GetFormattedVersionDefinitionInfo(ELFParser parser)
        {
            if (parser.VersionDefinitions == null || parser.VersionDefinitions.Count == 0)
            {
                return "";
            }

            // 获取.gnu.version_d节的信息
            Models.ELFSectionHeader? verdefSection = parser.SectionHeaders?.Find(sh => sh.sh_type == (uint)SectionType.SHT_GNU_verdef);
            if (verdefSection == null)
            {
                return "";
            }

            Models.ELFSectionHeader vd = verdefSection.Value;
            StringBuilder sb = new();
            sb.AppendLine(CultureInfo.InvariantCulture, $"Version definition section '.gnu.version_d' contains {vd.sh_info} entries:");

            int entryIndex = 0;
            foreach (KeyValuePair<ushort, string> kvp in parser.VersionDefinitions.OrderBy(k => k.Key))
            {
                bool isBase = kvp.Key == 1;
                string flags = isBase ? "BASE" : "";
                ulong entryDelta = isBase ? 0UL : (ulong)entryIndex * vd.sh_entsize;
                ulong addr = vd.sh_addr + entryDelta;
                ulong fileOffset = vd.sh_offset + entryDelta;
                sb.AppendLine(CultureInfo.InvariantCulture, $"  地址：0x{addr:x8}  Offset: 0x{fileOffset:x6}  Link: {vd.sh_link} (.dynstr)  {entryIndex:D4}: Rev: 1  Flags: {flags,-6}   Index: {kvp.Key}  Cnt: 1  名称：{kvp.Value}");
                entryIndex++;
            }

            // 检查是否超出范围（参考 readelf："Version definition past end of section"）
            if (parser.VersionDefinitions.Count > VersionSymbolParser.CalculateVerDefEntryCount(vd))
            {
                sb.AppendLine("  Version definition past end of section");
            }

            return sb.ToString();
        }

        private static string GetVersionInfoByIndex(ELFParser parser, ushort versionIndex)
        {
            return versionIndex switch
            {
                0 => "*本地*",
                1 => "*全局*",
                _ => parser.VersionDefinitions.GetValueOrDefault(versionIndex) ?? parser.VersionDependencies.GetValueOrDefault(versionIndex) ?? $"VER_{versionIndex}"
            };
        }

        private static void ParseAndAppendVersionNeeds(ELFParser parser, Models.ELFSectionHeader section, StringBuilder sb)
        {
            if (!ELFParserUtils.TryGetLinkedStringTable(parser, section, out byte[] strTabData, out bool isLittleEndian))
            {
                return;
            }

            long sectionStart = (long)section.sh_offset;

            // 遍历骨架与 readelf 一致，不限项数；填表与格式化共用 WalkVerneed（见 ParseDependencies.cs）
            int processed = VersionSymbolParser.WalkVerneed(parser, sectionStart, maxCount: -1, isLittleEndian,
                onVerneed: (verneedOffset, vn_cnt) =>
                {
                    uint vn_version = ELFParserUtils.ReadUInt16(parser.FileData, (int)verneedOffset, isLittleEndian);
                    uint vn_file = ELFParserUtils.ReadUInt32(parser.FileData, (int)verneedOffset + 4, isLittleEndian);
                    string libName = ELFParserUtils.ExtractStringFromBytes(strTabData, (int)vn_file);

                    // 偏移为相对节起始的连续偏移（与 readelf 一致）
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  {FormatVersionOffset((int)(verneedOffset - sectionStart))}: 版本: {vn_version}  文件: {libName}  计数: {vn_cnt}");
                },
                onVernaux: auxOffset =>
                {
                    // vernaux: vna_hash(+0) vna_flags(+4) vna_other(+6) vna_name(+8) vna_next(+12)
                    ushort vnaFlags = ELFParserUtils.ReadUInt16(parser.FileData, (int)auxOffset + 4, isLittleEndian);
                    ushort vnaOther = ELFParserUtils.ReadUInt16(parser.FileData, (int)auxOffset + 6, isLittleEndian);

                    // vna_other 的低 15 位是版本索引
                    ushort verIndex = (ushort)(vnaOther & 0x7fff);
                    string actualVersionName = GetVersionInfoByIndex(parser, verIndex);

                    sb.AppendLine(CultureInfo.InvariantCulture, $"  {FormatVersionOffset((int)(auxOffset - sectionStart))}: 名称: {actualVersionName}  标志: {GetVerneedFlags(vnaFlags)}  版本: {verIndex}");
                });

            if (processed == 0)
            {
                sb.AppendLine("  No version dependencies found.");
            }
        }

        // 节内偏移格式：0 → "000000"，否则 → "0x00NN"（与 readelf %#06x 一致）
        private static string FormatVersionOffset(int offset)
        {
            return offset == 0 ? "000000" : $"0x{offset:x4}";
        }

        // 解析 vna_flags（VER_FLG_BASE=0x1, VER_FLG_WEAK=0x2）
        private static string GetVerneedFlags(ushort flags)
        {
            if (flags == 0)
            {
                return "none";
            }

            List<string> parts = [];
            if ((flags & 0x1) != 0)
            {
                parts.Add("BASE");
            }
            if ((flags & 0x2) != 0)
            {
                parts.Add("WEAK");
            }
            ushort unknown = (ushort)(flags & ~0x3);
            if (unknown != 0)
            {
                parts.Add($"<unknown: {unknown:x}>");
            }
            return string.Join(" | ", parts);
        }
    }
}