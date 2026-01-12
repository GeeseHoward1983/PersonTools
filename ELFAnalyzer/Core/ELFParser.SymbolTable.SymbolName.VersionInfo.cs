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
            var sb = new StringBuilder();

            // 首先检查是否存在版本符号表
            Models.ELFSectionHeader? verSymSection = GetSection(parser, ".gnu.version");

            if ((verSymSection != null))
            {
                if (parser.VersionSymbols != null && parser.Symbols != null)
                {
                    sb.AppendLine($"Version symbols section '.gnu.version' contains {parser.VersionSymbols.Length} entries:");
                    sb.AppendLine($"  地址: 0x{verSymSection.Value.sh_addr:x16}  Offset: 0x{verSymSection.Value.sh_offset:x6}  Link: {verSymSection.Value.sh_link} (.dynsym)");

                    // 每行显示4个版本符号
                    for (int i = 0; i < parser.VersionSymbols.Length; i++)
                    {
                        sb.Append($" {i:x3}:");
                        ushort versionIndex = (ushort)(parser.VersionSymbols[i] & 0x7fff);
                        string versionInfo = GetVersionInfoByIndex(parser, versionIndex);
                        sb.Append($" {versionIndex:D3} ({versionInfo})");
                        if ((i & 0x3) == 0x3)
                            sb.AppendLine();
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
            var sb = new StringBuilder();

            // 检查是否存在版本需求表
            Models.ELFSectionHeader? verNeedSection = GetSection(parser, ".gnu.version_r"); ;

            if ((verNeedSection != null))
            {
                if (verNeedSection != null && parser.VersionDependencies != null)
                {
                    sb.AppendLine($"Version needs section '.gnu.version_r' contains {parser.VersionDependencies.Count} entries:");
                    sb.AppendLine($"  地址: 0x{verNeedSection.Value.sh_addr:x16}  Offset: 0x{verNeedSection.Value.sh_offset:x6}  Link: {verNeedSection.Value.sh_link} (.dynstr)");

                    // 这里需要实际解析版本需求表的内容
                    ParseAndAppendVersionNeeds(parser, verNeedSection.Value, sb);
                }
            }
            else
            {
                sb.AppendLine("Version needs section '.gnu.version_r' not found or empty.");
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
            if (parser.SectionHeaders == null) return;

            //找到版本需求字符串表
            int strTabIdx = (int)section.sh_link;
            if (strTabIdx >= parser.SectionHeaders.Count) return;

            var strTabSection = parser.SectionHeaders[strTabIdx];
            var strTabData = new byte[strTabSection.sh_size];
            Array.Copy(parser.FileData, (long)strTabSection.sh_offset, strTabData, 0, (int)strTabSection.sh_size);

            long offset = (long)section.sh_offset;
            int processed = 0;

            // 遍历所有版本需求项（每个项代表一个库）
            while (offset < parser.FileData.Length)
            {
                // 读取版本需求结构
                var vn_version = BitConverter.ToUInt16(parser.FileData, (int)offset);
                var vn_cnt = BitConverter.ToUInt16(parser.FileData, (int)offset + 2);
                var vn_file = BitConverter.ToUInt32(parser.FileData, (int)offset + 4);
                var vn_aux = BitConverter.ToUInt32(parser.FileData, (int)offset + 8);
                var vn_next = BitConverter.ToUInt32(parser.FileData, (int)offset + 12);

                // 获取库名称
                string libName = ELFParserUtils.ExtractStringFromBytes(strTabData, (int)vn_file);

                // 计算该库的版本依赖数量
                int versionCount = vn_cnt;

                sb.AppendLine($"  000000: 版本: {vn_version}  文件: {libName}  计数: {versionCount}");

                long auxOffset = offset + vn_aux;
                int auxProcessed = 0;

                // 遍历该库的所有版本依赖
                while (auxProcessed < vn_cnt && auxOffset < parser.FileData.Length)
                {
                    var nameOffset = BitConverter.ToUInt32(parser.FileData, (int)auxOffset + 8);
                    var flags = BitConverter.ToUInt16(parser.FileData, (int)auxOffset + 6);
                    var auxNext = BitConverter.ToUInt32(parser.FileData, (int)auxOffset + 12);
                    _ = ELFParserUtils.ExtractStringFromBytes(strTabData, (int)nameOffset);

                    // 使用版本索引作为键来获取版本信息
                    ushort verIndex = (ushort)(flags & 0x7fff); // 去除隐藏标志
                    string actualVersionName = GetVersionInfoByIndex(parser, verIndex);

                    sb.AppendLine($"  0x{0x10 * (auxProcessed + 1):x4}: 名称: {actualVersionName}  标志: {((flags & 0x1) != 0x00 ? "BASE" : "none")}  版本: {verIndex}");

                    auxProcessed++;
                    if (auxNext == 0) break;
                    auxOffset += auxNext;
                }

                processed++;
                if (vn_next == 0) break; // 没有更多版本需求
                offset += vn_next;
            }

            if (processed == 0)
            {
                sb.AppendLine("  No version dependencies found.");
            }
        }
    }
}