using PersonalTools.PEAnalyzer.Parsers;
using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器命名图标资源解析模块
    /// 专门负责解析PE文件中的命名图标资源
    /// </summary>
    internal static class PEResourceParserIconNamed
    {
        /// <summary>
        /// 解析资源目录以查找命名图标资源。
        /// </summary>
        public static void ParseResourceDirectoryForNamedIcons(FileStream fs, BinaryReader reader, PEInfo peInfo, long resourceOffset)
        {
            try
            {
                if (resourceOffset < 0 || resourceOffset + ResourceDirectoryReader.DirectoryHeaderSize > fs.Length)
                {
                    return;
                }

                long originalPosition = fs.Position;
                fs.Position = resourceOffset;

                IMAGERESOURCEDIRECTORY rootDirectory = ResourceDirectoryReader.ReadDirectory(reader);

                // 命名条目位于目录前部，仅遍历这部分
                for (int i = 0; i < rootDirectory.NumberOfNamedEntries; i++)
                {
                    if (!ResourceDirectoryReader.TryReadEntry(fs, reader, resourceOffset, i, out IMAGERESOURCEDIRECTORYENTRY entry))
                    {
                        break;
                    }

                    if ((entry.NameOrId & 0x80000000) == 0)
                    {
                        continue;
                    }

                    long nextLevelOffset = resourceOffset + (entry.OffsetToData & 0x7FFFFFFF);
                    if (nextLevelOffset >= 0 && nextLevelOffset + ResourceDirectoryReader.DirectoryHeaderSize <= fs.Length)
                    {
                        ParseNamedResourceDirectory(fs, reader, peInfo, nextLevelOffset, resourceOffset, entry.NameOrId & 0x7FFFFFFF);
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                Console.WriteLine($"解析资源目录以查找命名图标错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析命名资源目录：仅当无名（已下钻）或资源名暗示为图标时，递归遍历其条目。
        /// </summary>
        private static void ParseNamedResourceDirectory(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset, uint nameOffset)
        {
            try
            {
                if (directoryOffset < 0 || directoryOffset + ResourceDirectoryReader.DirectoryHeaderSize > fs.Length)
                {
                    return;
                }

                long originalPosition = fs.Position;

                if (ShouldProcessNamedResource(fs, reader, resourceBaseOffset, nameOffset))
                {
                    ResourceDirectoryReader.WalkEntries(fs, reader, directoryOffset, resourceBaseOffset,
                        subdirectoryOffset => ParseNamedResourceDirectory(fs, reader, peInfo, subdirectoryOffset, resourceBaseOffset, 0),
                        dataEntryOffset => PEResourceParserIconData.ReadAndProcessIconDataEntry(fs, reader, peInfo, dataEntryOffset));
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                Console.WriteLine($"解析命名资源目录错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 判断该命名资源是否应继续解析：无名条目（nameOffset==0，表示已进入子目录）始终处理；
        /// 否则仅当资源名包含 icon/.ico/app 等关键字时才处理。
        /// </summary>
        private static bool ShouldProcessNamedResource(FileStream fs, BinaryReader reader, long resourceBaseOffset, uint nameOffset)
        {
            if (nameOffset == 0)
            {
                return true;
            }

            long nameStringOffset = resourceBaseOffset + nameOffset;
            if (nameStringOffset < 0 || nameStringOffset + 2 > fs.Length)
            {
                return false;
            }

            string resourceName = PEResourceParserIconHelpers.ReadResourceName(fs, reader, nameStringOffset);
            return !string.IsNullOrEmpty(resourceName) &&
                (resourceName.Contains("icon", StringComparison.OrdinalIgnoreCase) ||
                 resourceName.Contains(".ico", StringComparison.OrdinalIgnoreCase) ||
                 resourceName.Contains("app", StringComparison.OrdinalIgnoreCase));
        }
    }
}
