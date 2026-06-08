using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器直接图标资源解析模块
    /// 专门负责解析PE文件中的直接图标资源（非 GROUP_ICON 中的 RT_ICON）
    /// </summary>
    internal static class PEResourceParserIconDirect
    {
        /// <summary>
        /// 解析直接的 RT_ICON 资源（递归遍历目录，叶子节点交给图标数据处理）。
        /// </summary>
        public static void ParseDirectIconResource(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset)
        {
            ResourceDirectoryReader.RunAtOffset(fs, directoryOffset, ResourceDirectoryReader.DirectoryHeaderSize, "直接图标资源解析错误", () =>
                ResourceDirectoryReader.WalkEntries(fs, reader, directoryOffset, resourceBaseOffset,
                    subdirectoryOffset => ParseDirectIconResource(fs, reader, peInfo, subdirectoryOffset, resourceBaseOffset),
                    dataEntryOffset => PEResourceParserIconData.ReadAndProcessIconDataEntry(fs, reader, peInfo, dataEntryOffset)));
        }
    }
}
