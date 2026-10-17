using PersonalTools.ELFAnalyzer.Core;
using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Parsers;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器版本信息解析模块
    /// 专门负责解析PE文件中的版本信息资源
    /// </summary>
    internal static class PEResourceParserVersion
    {
        private static bool CheckDataDirectory(PEInfo peInfo)
        {
            return peInfo.OptionalHeader.DataDirectory.Length > PEConstants.DirectoryResource &&
                   peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryResource].VirtualAddress != 0;
        }

        private static bool ParseVersionInfo2(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            return PEParserUtils.RvaToOffset(peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryResource].VirtualAddress, peInfo.SectionHeaders) switch
            {
                -1 => false,
                _ when PEParserUtils.RvaToOffset(peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryResource].VirtualAddress, peInfo.SectionHeaders) < fs.Length => false,
                _ => ParseResourceDirectoryForVersionInfo(fs, reader, peInfo, PEParserUtils.RvaToOffset(peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryResource].VirtualAddress, peInfo.SectionHeaders))
            };
        }
        /// <summary>
        /// 解析版本信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        internal static void ParseVersionInfo(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            // 扫描会话：与图标解析同款。否则无 RT_VERSION 命中时 ScanTypeEntries 已消耗的条目预算
            // 不会被任何顶层 WalkEntries 复位，泄漏进随后的图标解析导致图标被跳过
            using ResourceDirectoryReader.ScanScope scope = ResourceDirectoryReader.BeginScan();

            // 版本信息通常在资源节中，数据目录索引为#2 (IMAGE_DIRECTORY_ENTRY_RESOURCE)
            peInfo.AdditionalInfo.FileVersion = CheckDataDirectory(peInfo) switch
            {
                true => ParseVersionInfo2(fs, reader, peInfo) switch
                {
                    true => peInfo.AdditionalInfo.FileVersion,
                    _ => $"无效的资源偏移: 0x{peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryResource].VirtualAddress:X8}"
                },
                _ => "文件不包含版本资源"
            };
        }

        /// <summary>
        /// 解析资源目录以查找版本信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="resourceOffset">资源节偏移</param>
        private static bool ParseResourceDirectoryForVersionInfo(FileStream fs, BinaryReader reader, PEInfo peInfo, long resourceOffset)
        {
            // succeeded[0]：跨所有兄弟语言/名称叶子共享的"已成功解析出真实版本"标志。一旦某叶子解析成功，
            // 后续畸形兄弟叶子不得用错误占位串覆盖已得的正确版本（多语言/多叶子 PE 或恶意构造均可触发）。
            bool[] succeeded = [false];

            if (resourceOffset < 0 || resourceOffset + ResourceDirectoryReader.DirectoryHeaderSize > fs.Length)
            {
                return true;
            }

            long originalPosition = fs.Position;
            fs.Position = resourceOffset;

            IMAGE_RESOURCE_DIRECTORY rootDirectory = ResourceDirectoryReader.ReadDirectory(reader);
            int totalEntries = rootDirectory.NumberOfNamedEntries + rootDirectory.NumberOfIdEntries;

            // 查找 RT_VERSION 资源类型 (ID = 16)，命中首个后下钻并结束
            ResourceDirectoryReader.ScanTypeEntries(fs, reader, resourceOffset, totalEntries, 16,
                nextLevelOffset => ParseVersionResource(fs, reader, peInfo, nextLevelOffset, resourceOffset, succeeded),
                stopAtFirst: true);

            fs.Position = originalPosition;

            return true;
        }

        /// <summary>
        /// 解析版本资源（递归遍历目录，叶子节点交给数据项解析）。
        /// </summary>
        private static void ParseVersionResource(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset, bool[] succeeded)

        {
            if (directoryOffset < 0 || directoryOffset + ResourceDirectoryReader.DirectoryHeaderSize > fs.Length)
            {
                return;
            }

            long originalPosition = fs.Position;

            ResourceDirectoryReader.WalkEntries(fs, reader, directoryOffset, resourceBaseOffset,
                subdirectoryOffset => ParseVersionResource(fs, reader, peInfo, subdirectoryOffset, resourceBaseOffset, succeeded),
                dataEntryOffset => ParseVersionDataEntry(fs, reader, peInfo, dataEntryOffset, succeeded));

            fs.Position = originalPosition;
        }

        /// <summary>
        /// 解析资源数据项（定位 VS_VERSIONINFO 并交给结构解析器）。
        /// </summary>
        private static void ParseVersionDataEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset, bool[] succeeded)
        {
            // 已成功解析出真实版本：跳过后续兄弟叶子，避免其错误占位串覆盖已得的正确版本
            if (succeeded[0])
            {
                return;
            }

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
                // 解析成功即置位（后续兄弟叶子将被上方短路跳过）；失败时其内部占位仅在尚无成功结果时有效
                succeeded[0] = PEResourceParserVersionHelpers.ParseVersionInfoStructure(fs, reader, peInfo);
            }
            else
            {
                peInfo.AdditionalInfo.FileVersion = $"无效的数据偏移: 0x{dataEntry.OffsetToData:X8}";
            }

            fs.Position = originalPosition;
        }
    }
}