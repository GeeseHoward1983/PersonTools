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

            if (ParseResourceDirectoryForVersionInfoCheck(fs.Length, resourceOffset))
            {
                ParseResourceDirectoryForVersionInfoCore(fs, reader, peInfo, resourceOffset);
            }
            return true;
        }

        private static void ParseResourceDirectoryForVersionInfoCore(FileStream fs, BinaryReader reader, PEInfo peInfo, long resourceOffset)
        {
            bool[] succeeded = [false];
            ParseResourceDirectoryForVersionInfoCore2(fs, reader, peInfo, resourceOffset, succeeded);
        }

        private static void ParseResourceDirectoryForVersionInfoCore2(FileStream fs, BinaryReader reader, PEInfo peInfo, long resourceOffset, bool[] succeeded)
        {
            long originalPosition = fs.Position;
            ParseResourceDirectoryForVersionInfoCore3(fs, reader, peInfo, resourceOffset, succeeded);
            fs.Position = originalPosition;
        }

        private static void ParseResourceDirectoryForVersionInfoCore3(FileStream fs, BinaryReader reader, PEInfo peInfo, long resourceOffset, bool[] succeeded)
        {
            fs.Position = resourceOffset;
            ParseResourceDirectoryForVersionInfoCore4(fs, reader, peInfo, resourceOffset, succeeded);
        }

        private static void ParseResourceDirectoryForVersionInfoCore4(FileStream fs, BinaryReader reader, PEInfo peInfo, long resourceOffset, bool[] succeeded)
        {
            IMAGE_RESOURCE_DIRECTORY rootDirectory = ResourceDirectoryReader.ReadDirectory(reader);
            ParseResourceDirectoryForVersionInfoCore5(rootDirectory.NumberOfNamedEntries + rootDirectory.NumberOfIdEntries, fs, reader, peInfo, resourceOffset, succeeded);
        }

        private static void ParseResourceDirectoryForVersionInfoCore5(int totalEntries, FileStream fs, BinaryReader reader, PEInfo peInfo, long resourceOffset, bool[] succeeded)
        {
            // 查找 RT_VERSION 资源类型 (ID = 16)，命中首个后下钻并结束
            ResourceDirectoryReader.ScanTypeEntries(fs, reader, resourceOffset, totalEntries, 16,
                nextLevelOffset => ParseVersionResource(fs, reader, peInfo, nextLevelOffset, resourceOffset, succeeded),
                stopAtFirst: true);
        }

        private static bool ParseResourceDirectoryForVersionInfoCheck(long fsLength, long resourceOffset)
        {
            return resourceOffset >= 0 && resourceOffset + ResourceDirectoryReader.DirectoryHeaderSize <= fsLength;
        }

        /// <summary>
        /// 解析版本资源（递归遍历目录，叶子节点交给数据项解析）。
        /// </summary>
        private static void ParseVersionResource(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset, bool[] succeeded)

        {
            if (ParseVersionResourceCheck(fs.Length, directoryOffset))
            {
                ParseVersionResource2(fs, reader, peInfo, directoryOffset, resourceBaseOffset, succeeded);
            }
        }

        private static void ParseVersionResource2(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset, bool[] succeeded)

        {
            long originalPosition = fs.Position;
            ParseVersionResourceCore(fs, reader, peInfo, directoryOffset, resourceBaseOffset, succeeded);
            fs.Position = originalPosition;
        }

        private static void ParseVersionResourceCore(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset, bool[] succeeded)
        {
                ResourceDirectoryReader.WalkEntries(fs, reader, directoryOffset, resourceBaseOffset,
                    subdirectoryOffset => ParseVersionResource(fs, reader, peInfo, subdirectoryOffset, resourceBaseOffset, succeeded),
                    dataEntryOffset => ParseVersionDataEntry(fs, reader, peInfo, dataEntryOffset, succeeded));
        }

        private static bool ParseVersionResourceCheck(long fsLength, long directoryOffset)
        {
            return directoryOffset >= 0 && directoryOffset + ResourceDirectoryReader.DirectoryHeaderSize <= fsLength;
        }


        /// <summary>
        /// 解析资源数据项（定位 VS_VERSIONINFO 并交给结构解析器）。
        /// </summary>
        private static void ParseVersionDataEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset, bool[] succeeded)
        {
            // 已成功解析出真实版本：跳过后续兄弟叶子，避免其错误占位串覆盖已得的正确版本
            if (ParseVersionDataEntryCheck(fs.Length, dataEntryOffset, succeeded))
            {
                ParseVersionDataEntry2(fs, reader, peInfo, dataEntryOffset, succeeded);
            }
            else
                peInfo.AdditionalInfo.FileVersion = "资源数据条目不完整";
        }

        private static void ParseVersionDataEntry2(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset, bool[] succeeded)
        {
            // 已成功解析出真实版本：跳过后续兄弟叶子，避免其错误占位串覆盖已得的正确版本
            long originalPosition = fs.Position;
            ParseVersionDataEntry3(fs, reader, peInfo, dataEntryOffset, succeeded);
            fs.Position = originalPosition;
        }

        private static void ParseVersionDataEntry3(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset, bool[] succeeded)
        {
            fs.Position = dataEntryOffset;
            ParseVersionDataEntryCore(fs, reader, peInfo, succeeded);
        }

        private static bool ParseVersionDataEntryCheck(long fsLength, long dataEntryOffset, bool[] succeeded)
        {
            return succeeded[0] || (dataEntryOffset >= 0 && dataEntryOffset + 16 <= fsLength);
        }

        private static void ParseVersionDataEntryCore(FileStream fs, BinaryReader reader, PEInfo peInfo, bool[] succeeded)
        {
            IMAGE_RESOURCE_DATA_ENTRY dataEntry = ResourceDirectoryReader.ReadDataEntry(reader);
            ParseVersionDataEntryCore2(dataEntry, fs, reader, peInfo, succeeded);
        }

        private static void ParseVersionDataEntryCore2(IMAGE_RESOURCE_DATA_ENTRY dataEntry, FileStream fs, BinaryReader reader, PEInfo peInfo, bool[] succeeded)
        {
            long dataOffset = PEParserUtils.RvaToOffset(dataEntry.OffsetToData, peInfo.SectionHeaders, dataEntry.Size);
            ParseVersionDataEntryCore3(dataOffset, dataEntry, fs, reader, peInfo, succeeded);
        }

        private static void ParseVersionDataEntryCore3(long dataOffset, IMAGE_RESOURCE_DATA_ENTRY dataEntry, FileStream fs, BinaryReader reader, PEInfo peInfo, bool[] succeeded)
        {
            if (ResourceDirectoryReader.IsReadableData(dataOffset, dataEntry.Size, fs))
            {
                ParseVersionDataEntryCore4(dataOffset, fs, reader, peInfo, succeeded);
            }
            else
            {
                peInfo.AdditionalInfo.FileVersion = $"无效的数据偏移: 0x{dataEntry.OffsetToData:X8}";
            }
        }

        private static void ParseVersionDataEntryCore4(long dataOffset, FileStream fs, BinaryReader reader, PEInfo peInfo, bool[] succeeded)
        {
            fs.Position = dataOffset;
            succeeded[0] = PEResourceParserVersionHelpers.ParseVersionInfoStructure(fs, reader, peInfo);
        }
    }
}