using System.Globalization;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class VersionSymbleTable
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
        public static string GetFormattedVersionSymbolInfo(ELFParser parser)
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

        public static string GetFormattedVersionDependencyInfo(ELFParser parser)
        {
            StringBuilder sb = new();

            // 检查是否存在版本需求表
            Models.ELFSectionHeader? verNeedSection = GetSection(parser, ".gnu.version_r"); ;

            if (verNeedSection != null)
            {
                if (parser.VersionDependencies != null)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"Version needs section '.gnu.version_r' contains {parser.VersionDependencies.Count} entries:");
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  地址: 0x{verNeedSection.Value.sh_addr:x16}  Offset: 0x{verNeedSection.Value.sh_offset:x6}  Link: {verNeedSection.Value.sh_link} (.dynstr)");

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

            Models.ELFSectionHeader strTabSection = parser.SectionHeaders[strTabIdx];
            byte[] strTabData = new byte[strTabSection.sh_size];
            Array.Copy(parser.FileData, (long)strTabSection.sh_offset, strTabData, 0, (int)strTabSection.sh_size);

            long offset = (long)section.sh_offset;
            int processed = 0;

            // 遍历所有版本需求项（每个项代表一个库）
            while (offset < parser.FileData.Length)
            {
                // 读取版本需求结构
                if (!parser.Header.IsLittleEndian()) // 如果不是小端序
                {
                    Array.Reverse(parser.FileData, (int)offset, 2);
                    Array.Reverse(parser.FileData, (int)offset + 2, 2);
                    Array.Reverse(parser.FileData, (int)offset + 4, 4);
                    Array.Reverse(parser.FileData, (int)offset + 8, 4);
                    Array.Reverse(parser.FileData, (int)offset + 12, 4);
                }

                uint vn_version = BitConverter.ToUInt16(parser.FileData, (int)offset);
                ushort vn_cnt = BitConverter.ToUInt16(parser.FileData, (int)offset + 2);
                uint vn_file = BitConverter.ToUInt32(parser.FileData, (int)offset + 4);
                uint vn_aux = BitConverter.ToUInt32(parser.FileData, (int)offset + 8);
                uint vn_next = BitConverter.ToUInt32(parser.FileData, (int)offset + 12);

                // 获取库名称
                string libName = ELFParserUtils.ExtractStringFromBytes(strTabData, (int)vn_file);

                // 计算该库的版本依赖数量
                int versionCount = vn_cnt;

                sb.AppendLine(CultureInfo.InvariantCulture, $"  000000: 版本: {vn_version}  文件: {libName}  计数: {versionCount}");

                long auxOffset = offset + vn_aux;
                int auxProcessed = 0;

                // 遍历该库的所有版本依赖
                while (auxProcessed < vn_cnt && auxOffset < parser.FileData.Length)
                {
                    if (!parser.Header.IsLittleEndian()) // 如果不是小端序
                    {
                        Array.Reverse(parser.FileData, (int)auxOffset + 8, 4);
                        Array.Reverse(parser.FileData, (int)auxOffset + 6, 2);
                        Array.Reverse(parser.FileData, (int)auxOffset + 12, 4);
                    }
                    uint nameOffset = BitConverter.ToUInt32(parser.FileData, (int)auxOffset + 8);
                    ushort flags = BitConverter.ToUInt16(parser.FileData, (int)auxOffset + 6);
                    uint auxNext = BitConverter.ToUInt32(parser.FileData, (int)auxOffset + 12);
                    _ = ELFParserUtils.ExtractStringFromBytes(strTabData, (int)nameOffset);

                    // 使用版本索引作为键来获取版本信息
                    ushort verIndex = (ushort)(flags & 0x7fff); // 去除隐藏标志
                    string actualVersionName = GetVersionInfoByIndex(parser, verIndex);

                    sb.AppendLine(CultureInfo.InvariantCulture, $"  0x{0x10 * (auxProcessed + 1):x4}: 名称: {actualVersionName}  标志: {((flags & 0x1) != 0x00 ? "BASE" : "none")}  版本: {verIndex}");

                    auxProcessed++;
                    if (auxNext == 0)
                    {
                        break;
                    }

                    auxOffset += auxNext;
                }

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
    }
}