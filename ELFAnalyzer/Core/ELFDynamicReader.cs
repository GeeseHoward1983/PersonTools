using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;
using System.IO;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFDynamicReader
    {
        internal static void ReadDynamicEntries(ELFParser parser, BinaryReader reader, bool isLittleEndian)
        {
            // Find the dynamic section
            int dynamicSectionIndex = -1;
            for (int i = 0; i < parser.SectionHeaders?.Count; i++)
            {
                if (parser.SectionHeaders[i].sh_type == (uint)SectionType.SHT_DYNAMIC)
                {
                    dynamicSectionIndex = i;
                    break;
                }
            }

            if (dynamicSectionIndex != -1 && parser.SectionHeaders != null)
            {
                Models.ELFSectionHeader dynSection = parser.SectionHeaders[dynamicSectionIndex];
                if (dynSection.sh_entsize == 0)
                {
                    return; // 畸形文件 sh_entsize 为 0 时跳过，避免除零
                }

                // 安全：校验整段落在文件内，避免畸形大小导致超大预分配(OOM)/读取越界
                if (!ELFParserUtils.IsRangeWithin(dynSection.sh_offset, dynSection.sh_size, (ulong)parser.FileData.Length))
                {
                    return;
                }

                reader.BaseStream.Seek((long)dynSection.sh_offset, SeekOrigin.Begin);

                // 安全：sh_entsize 不可信，实际按固定大小读取（64 位 Elf64_Dyn=16 / 32 位 Elf32_Dyn=8）。
                // 按固定项大小算条目数，避免伪造 sh_entsize 令 count 暴增、越窗读到 EOF。
                int entrySize = parser.Is64Bit ? ELFConstants.DynamicEntrySize64 : ELFConstants.DynamicEntrySize32;
                int entryCount = (int)(dynSection.sh_size / (ulong)entrySize);
                parser.DynamicEntries = new List<ELFDynamic>(Math.Min(entryCount, ELFConstants.MaxPreallocCount));

                for (int i = 0; i < entryCount; i++)
                {
                    ELFDynamic entry = new()
                    {
                        d_tag = parser.Is64Bit ? ELFParserUtils.ReadInt64(reader, isLittleEndian) : ELFParserUtils.ReadInt32(reader, isLittleEndian),
                        d_val = parser.Is64Bit ? ELFParserUtils.ReadUInt64(reader, isLittleEndian) : ELFParserUtils.ReadUInt32(reader, isLittleEndian)
                    };
                    parser.DynamicEntries.Add(entry);
                }
            }
        }
    }
}