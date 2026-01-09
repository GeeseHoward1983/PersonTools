using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    public partial class ELFParser
    {
        public string GetFormattedVersionSymbolInfo()
        {
            var sb = new StringBuilder();

            // 首先检查是否存在版本符号表
            Models.ELFSectionHeader? verSymSection = null;

            if (_sectionHeaders != null)
            {
                foreach (var section in _sectionHeaders)
                {
                    string sectionName = GetSectionName(_sectionHeaders.IndexOf(section)) ?? string.Empty;
                    if (sectionName == ".gnu.version" || sectionName == ".gnu.version_r")
                    {
                        if (sectionName == ".gnu.version" && _versionSymbols != null)
                        {
                            verSymSection = section;
                            break;
                        }
                    }
                }
            }

            if ((verSymSection != null))
            {
                if (verSymSection != null && _versionSymbols != null && _symbols != null)
                {
                    int entryCount = _versionSymbols.Length;
                    sb.AppendLine($"Version symbols section '.gnu.version' contains {entryCount} entries:");
                    sb.AppendLine($"  地址: 0x{verSymSection.Value.sh_addr:x16}  Offset: 0x{verSymSection.Value.sh_offset:x6}  Link: {verSymSection.Value.sh_link} (.dynsym)");

                    // 每行显示4个版本符号
                    for (int i = 0; i < _versionSymbols.Length; i += 4)
                    {
                        sb.Append($" {i:x3}:");
                        for (int j = 0; j < 4 && (i + j) < _versionSymbols.Length; j++)
                        {
                            ushort versionIndex = (ushort)(_versionSymbols[i + j] & 0x7fff);
                            string versionInfo = GetVersionInfoByIndex(versionIndex);
                            sb.Append($" {versionIndex:D3} ({versionInfo})");
                        }
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

        public string GetFormattedVersionDependencyInfo()
        {
            var sb = new StringBuilder();

            // 检查是否存在版本需求表
            Models.ELFSectionHeader? verNeedSection = null;

            if (_sectionHeaders != null)
            {
                foreach (var section in _sectionHeaders)
                {
                    string sectionName = GetSectionName(_sectionHeaders.IndexOf(section)) ?? string.Empty;
                    if (sectionName == ".gnu.version_r")
                    {
                        verNeedSection = section;
                        break;
                    }
                }
            }

            if ((verNeedSection != null))
            {
                if (verNeedSection != null && _versionDependencies != null)
                {
                    int entryCount = _versionDependencies.Count;
                    sb.AppendLine($"Version needs section '.gnu.version_r' contains {entryCount} entries:");
                    sb.AppendLine($"  地址: 0x{verNeedSection.Value.sh_addr:x16}  Offset: 0x{verNeedSection.Value.sh_offset:x6}  Link: {verNeedSection.Value.sh_link} (.dynstr)");

                    // 这里需要实际解析版本需求表的内容
                    ParseAndAppendVersionNeeds(verNeedSection.Value, sb);
                }
            }
            else
            {
                sb.AppendLine("Version needs section '.gnu.version_r' not found or empty.");
            }

            return sb.ToString();
        }

        private string GetVersionInfoByIndex(ushort versionIndex)
        {
            if (versionIndex == 0) return "*本地*";
            if (versionIndex == 1) return "*全局*";

            if (_versionDefinitions != null && _versionDefinitions.ContainsKey(versionIndex))
            {
                return _versionDefinitions[versionIndex];
            }

            if (_versionDependencies != null && _versionDependencies.ContainsKey(versionIndex))
            {
                return _versionDependencies[versionIndex];
            }

            return $"VER_{versionIndex}";
        }

        private void ParseAndAppendVersionNeeds(Models.ELFSectionHeader section, StringBuilder sb)
        {
            if (_sectionHeaders == null) return;

            // 找到版本需求字符串表
            int strTabIdx = (int)section.sh_link;
            if (strTabIdx >= _sectionHeaders.Count) return;

            var strTabSection = _sectionHeaders[strTabIdx];
            var strTabData = new byte[strTabSection.sh_size];
            Array.Copy(_fileData, (long)strTabSection.sh_offset, strTabData, 0, (int)strTabSection.sh_size);

            long offset = (long)section.sh_offset;
            int processed = 0;

            // 遍历所有版本需求项（每个项代表一个库）
            while (offset < _fileData.Length)
            {
                // 读取版本需求结构
                var vn_version = BitConverter.ToUInt16(_fileData, (int)offset);
                var vn_cnt = BitConverter.ToUInt16(_fileData, (int)offset + 2);
                var vn_file = BitConverter.ToUInt32(_fileData, (int)offset + 4);
                var vn_aux = BitConverter.ToUInt32(_fileData, (int)offset + 8);
                var vn_next = BitConverter.ToUInt32(_fileData, (int)offset + 12);

                // 获取库名称
                string libName = ELFParserUtils.ExtractStringFromBytes(strTabData, (int)vn_file);

                // 计算该库的版本依赖数量
                int versionCount = vn_cnt;

                sb.AppendLine($"  000000: 版本: {vn_version}  文件: {libName}  计数: {versionCount}");

                long auxOffset = offset + vn_aux;
                int auxProcessed = 0;

                // 遍历该库的所有版本依赖
                while (auxProcessed < vn_cnt && auxOffset < _fileData.Length)
                {
                    var nameOffset = BitConverter.ToUInt32(_fileData, (int)auxOffset + 8);
                    var flags = BitConverter.ToUInt16(_fileData, (int)auxOffset + 6);
                    var auxNext = BitConverter.ToUInt32(_fileData, (int)auxOffset + 12);
                    _ = ELFParserUtils.ExtractStringFromBytes(strTabData, (int)nameOffset);

                    // 使用版本索引作为键来获取版本信息
                    ushort verIndex = (ushort)(flags & 0x7fff); // 去除隐藏标志
                    string actualVersionName = GetVersionInfoByIndex(verIndex);

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