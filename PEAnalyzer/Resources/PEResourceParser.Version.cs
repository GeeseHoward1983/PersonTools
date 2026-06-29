using PersonalTools.PEAnalyzer.Parsers;
using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器版本信息解析模块
    /// 专门负责解析PE文件中的版本信息资源
    /// </summary>
    internal static class PEResourceParserVersion
    {
        /// <summary>
        /// 解析版本信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        internal static void ParseVersionInfo(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            try
            {
                // 版本信息通常在资源节中，数据目录索引为#2 (IMAGE_DIRECTORY_ENTRY_RESOURCE)

                if (peInfo.OptionalHeader.DataDirectory.Length > PEConstants.DirectoryResource &&
                    peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryResource].VirtualAddress != 0)
                {
                    uint resourceRVA = peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryResource].VirtualAddress;
                    long resourceOffset = PEParserUtils.RvaToOffset(resourceRVA, peInfo.SectionHeaders);

                    if (resourceOffset != -1 && resourceOffset < fs.Length)
                    {
                        // 解析资源目录以找到版本信息
                        ParseResourceDirectoryForVersionInfo(fs, reader, peInfo, resourceOffset);
                    }
                    else
                    {
                        peInfo.AdditionalInfo.FileVersion = $"无效的资源偏移: 0x{resourceRVA:X8}";
                    }
                }
                else
                {
                    peInfo.AdditionalInfo.FileVersion = "文件不包含版本资源";
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                // 解析版本信息时出现异常，记录日志但不中断程序执行；展示字段置简短占位，避免异常细节暴露到 UI
                PersonalTools.Utils.AppLogger.Log($"解析版本信息错误: {ex.Message}");
                peInfo.AdditionalInfo.FileVersion = "版本信息不完整";
            }
        }

        /// <summary>
        /// 解析资源目录以查找版本信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="resourceOffset">资源节偏移</param>
        private static void ParseResourceDirectoryForVersionInfo(FileStream fs, BinaryReader reader, PEInfo peInfo, long resourceOffset)
        {
            try
            {
                if (resourceOffset < 0 || resourceOffset + ResourceDirectoryReader.DirectoryHeaderSize > fs.Length)
                {
                    return;
                }

                long originalPosition = fs.Position;
                fs.Position = resourceOffset;

                IMAGE_RESOURCE_DIRECTORY rootDirectory = ResourceDirectoryReader.ReadDirectory(reader);
                int totalEntries = rootDirectory.NumberOfNamedEntries + rootDirectory.NumberOfIdEntries;

                // 查找 RT_VERSION 资源类型 (ID = 16)，命中首个后下钻并结束
                ResourceDirectoryReader.ScanTypeEntries(fs, reader, resourceOffset, totalEntries, 16,
                    nextLevelOffset => ParseVersionResource(fs, reader, peInfo, nextLevelOffset, resourceOffset),
                    stopAtFirst: true);

                fs.Position = originalPosition;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                PersonalTools.Utils.AppLogger.Log($"资源目录解析错误: {ex.Message}");
                peInfo.AdditionalInfo.FileVersion = "版本信息不完整";
            }
        }

        /// <summary>
        /// 解析版本资源（递归遍历目录，叶子节点交给数据项解析）。
        /// </summary>
        private static void ParseVersionResource(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset)
        {
            try
            {
                if (directoryOffset < 0 || directoryOffset + ResourceDirectoryReader.DirectoryHeaderSize > fs.Length)
                {
                    return;
                }

                long originalPosition = fs.Position;

                ResourceDirectoryReader.WalkEntries(fs, reader, directoryOffset, resourceBaseOffset,
                    subdirectoryOffset => ParseVersionResource(fs, reader, peInfo, subdirectoryOffset, resourceBaseOffset),
                    dataEntryOffset => ParseVersionDataEntry(fs, reader, peInfo, dataEntryOffset));

                fs.Position = originalPosition;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                PersonalTools.Utils.AppLogger.Log($"版本资源解析错误: {ex.Message}");
                peInfo.AdditionalInfo.FileVersion = "版本信息不完整";
            }
        }

        /// <summary>
        /// 解析资源数据项（定位 VS_VERSIONINFO 并交给结构解析器）。
        /// </summary>
        private static void ParseVersionDataEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset)
        {
            try
            {
                if (dataEntryOffset < 0 || dataEntryOffset + 16 > fs.Length)
                {
                    peInfo.AdditionalInfo.FileVersion = "资源数据条目不完整";
                    return;
                }

                long originalPosition = fs.Position;
                fs.Position = dataEntryOffset;

                IMAGE_RESOURCE_DATA_ENTRY dataEntry = ResourceDirectoryReader.ReadDataEntry(reader);

                // OffsetToData 为 RVA；传 Size 作为 requiredLength，确保整个版本数据块落在同一节内，
                // 否则 Size 跨节时仍被判可读，VS_VERSIONINFO 会从错误节区起始解析致结果错乱。
                long dataOffset = PEParserUtils.RvaToOffset(dataEntry.OffsetToData, peInfo.SectionHeaders, dataEntry.Size);
                if (ResourceDirectoryReader.IsReadableData(dataOffset, dataEntry.Size, fs))
                {
                    fs.Position = dataOffset;
                    PEResourceParserVersionHelpers.ParseVersionInfoStructure(fs, reader, peInfo);
                }
                else
                {
                    peInfo.AdditionalInfo.FileVersion = $"无效的数据偏移: 0x{dataEntry.OffsetToData:X8}";
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException)
            {
                PersonalTools.Utils.AppLogger.Log($"数据项解析错误: {ex.Message}");
                peInfo.AdditionalInfo.FileVersion = "版本信息不完整";
            }
        }
    }
}