using System.Globalization;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static partial class VersionSymbleTable
    {
        private static Models.ELFSectionHeader? GetSection(ELFParser parser, string objSectionName)
        {
            if (parser.SectionHeaders != null && parser.VersionSymbols != null)
            {
                for (int i = 0; i < parser.SectionHeaders.Count; i++)
                {
                    if (SymbleName.GetSectionName(parser, i) == objSectionName)
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

            if (verSymSection != null)
            {
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
            }
            else
            {
                sb.AppendLine("Version symbols section '.gnu.version' not found or empty.");
            }

            return sb.ToString();
        }

        internal static string GetFormattedVersionDependencyInfo(ELFParser parser)
        {
            StringBuilder sb = new();

            // 检查是否存在版本需求表
            Models.ELFSectionHeader? verNeedSection = GetSection(parser, ".gnu.version_r"); ;

            if (verNeedSection != null)
            {
                if (parser.VersionDependencies != null)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"Version needs section '.gnu.version_r' contains {verNeedSection.Value.sh_info} entries:");
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  地址: 0x{verNeedSection.Value.sh_addr:x16}  Offset: 0x{verNeedSection.Value.sh_offset:x8}  Link: {verNeedSection.Value.sh_link} (.dynstr)");

                    // 这里需要实际解析版本需求表的内容
                    ParseAndAppendVersionNeeds(parser, verNeedSection.Value, sb);
                }
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
            if (parser.SectionHeaders == null)
            {
                return;
            }

            //找到版本需求字符串表
            int strTabIdx = (int)section.sh_link;
            if (strTabIdx >= parser.SectionHeaders.Count)
            {
                return;
            }

            byte[] strTabData = parser.GetSectionData(strTabIdx);
            bool isLittleEndian = parser.Header.IsLittleEndian();

            long sectionStart = (long)section.sh_offset;
            long offset = sectionStart;
            int processed = 0;

            // 遍历所有版本需求项（每个项代表一个库）
            while (offset < parser.FileData.Length)
            {
                // 读取版本需求结构
                uint vn_version = ELFParserUtils.ReadUInt16(parser.FileData, (int)offset, isLittleEndian);
                ushort vn_cnt = ELFParserUtils.ReadUInt16(parser.FileData, (int)offset + 2, isLittleEndian);
                uint vn_file = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset + 4, isLittleEndian);
                uint vn_aux = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset + 8, isLittleEndian);
                uint vn_next = ELFParserUtils.ReadUInt32(parser.FileData, (int)offset + 12, isLittleEndian);

                // 获取库名称
                string libName = ELFParserUtils.ExtractStringFromBytes(strTabData, (int)vn_file);

                // 偏移为相对节起始的连续偏移（与 readelf 一致）
                sb.AppendLine(CultureInfo.InvariantCulture, $"  {FormatVersionOffset((int)(offset - sectionStart))}: 版本: {vn_version}  文件: {libName}  计数: {vn_cnt}");

                AppendVerneedAuxEntries(parser, sb, offset + vn_aux, vn_cnt, sectionStart, isLittleEndian);

                processed++;
                if (vn_next == 0)
                {
                    break; // 没有更多版本需求
                }

                offset += vn_next;
            }

            if (processed == 0)
            {
                sb.AppendLine("  No version dependencies found.");
            }
        }

        // 遍历某个版本需求项(库)下的所有 vernaux 依赖并追加输出
        private static void AppendVerneedAuxEntries(ELFParser parser, StringBuilder sb, long auxOffset, ushort vn_cnt, long sectionStart, bool isLittleEndian)
        {
            int auxProcessed = 0;
            while (auxProcessed < vn_cnt && auxOffset < parser.FileData.Length)
            {
                // vernaux: vna_hash(+0) vna_flags(+4) vna_other(+6) vna_name(+8) vna_next(+12)
                ushort vnaFlags = ELFParserUtils.ReadUInt16(parser.FileData, (int)auxOffset + 4, isLittleEndian);
                ushort vnaOther = ELFParserUtils.ReadUInt16(parser.FileData, (int)auxOffset + 6, isLittleEndian);
                uint auxNext = ELFParserUtils.ReadUInt32(parser.FileData, (int)auxOffset + 12, isLittleEndian);

                // vna_other 的低 15 位是版本索引
                ushort verIndex = (ushort)(vnaOther & 0x7fff);
                string actualVersionName = GetVersionInfoByIndex(parser, verIndex);

                sb.AppendLine(CultureInfo.InvariantCulture, $"  {FormatVersionOffset((int)(auxOffset - sectionStart))}: 名称: {actualVersionName}  标志: {GetVerneedFlags(vnaFlags)}  版本: {verIndex}");

                auxProcessed++;
                if (auxNext == 0)
                {
                    break;
                }

                auxOffset += auxNext;
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