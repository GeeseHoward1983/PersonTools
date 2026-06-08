using PersonalTools.PEAnalyzer.Parsers;
using PersonalTools.PEAnalyzer.Models;
using System.IO;
using System.Text;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器图标信息解析辅助模块
    /// 包含图标信息解析的辅助函数
    /// </summary>
    internal static class PEResourceParserIconHelpers
    {
        /// <summary>
        /// 在某资源类型目录下查找指定 ID 的图标数据偏移。
        /// </summary>
        internal static long FindSpecificIconData(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset, uint resourceId)
        {
            return ResourceDirectoryReader.ReadAtOffset(fs, directoryOffset, ResourceDirectoryReader.DirectoryHeaderSize, -1L, () =>
            {
                IMAGERESOURCEDIRECTORY directory = ResourceDirectoryReader.ReadDirectory(reader);
                int totalEntries = directory.NumberOfNamedEntries + directory.NumberOfIdEntries;

                for (int i = 0; i < totalEntries; i++)
                {
                    if (!ResourceDirectoryReader.TryReadEntry(fs, reader, directoryOffset, i, out IMAGERESOURCEDIRECTORYENTRY entry))
                    {
                        break;
                    }

                    if ((entry.NameOrId & 0xFFFF) != (resourceId & 0xFFFF))
                    {
                        continue;
                    }

                    return (entry.OffsetToData & 0x80000000) != 0
                        ? FindIconDataInLanguageDirectory(fs, reader, peInfo, resourceBaseOffset + (entry.OffsetToData & 0x7FFFFFFF), resourceBaseOffset)
                        : GetIconDataFromEntry(fs, reader, peInfo, resourceBaseOffset + entry.OffsetToData);
                }

                return -1L;
            });
        }

        /// <summary>
        /// 在语言目录中查找图标数据（通常取第一个数据条目）。
        /// </summary>
        internal static long FindIconDataInLanguageDirectory(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset)
        {
            return ResourceDirectoryReader.ReadAtOffset(fs, directoryOffset, ResourceDirectoryReader.DirectoryHeaderSize, -1L, () =>
            {
                IMAGERESOURCEDIRECTORY directory = ResourceDirectoryReader.ReadDirectory(reader);
                if (directory.NumberOfNamedEntries + directory.NumberOfIdEntries > 0 &&
                    ResourceDirectoryReader.TryReadEntry(fs, reader, directoryOffset, 0, out IMAGERESOURCEDIRECTORYENTRY entry) &&
                    (entry.OffsetToData & 0x80000000) == 0)
                {
                    return GetIconDataFromEntry(fs, reader, peInfo, resourceBaseOffset + entry.OffsetToData);
                }

                return -1L;
            });
        }

        /// <summary>
        /// 从数据条目获取图标数据偏移（校验偏移与大小有效）。
        /// </summary>
        internal static long GetIconDataFromEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset)
        {
            return ResourceDirectoryReader.ReadAtOffset(fs, dataEntryOffset, 16, -1L, () =>
            {
                IMAGERESOURCEDATAENTRY dataEntry = ResourceDirectoryReader.ReadDataEntry(reader);
                long dataOffset = Utilities.RvaToOffset(dataEntry.OffsetToData, peInfo.SectionHeaders);
                return ResourceDirectoryReader.IsReadableData(dataOffset, dataEntry.Size, fs) ? dataOffset : -1L;
            });
        }

        /// <summary>
        /// 根据资源ID查找图标数据偏移（先定位 RT_ICON(3) 类型目录）。
        /// </summary>
        internal static long FindIconDataByResourceId(FileStream fs, BinaryReader reader, PEInfo peInfo, uint resourceId, long resourceBaseOffset)
        {
            const int RT_ICON_TYPE = 3;

            long resourceOffset = GetResourceDirectoryOffset(fs, peInfo);
            if (resourceOffset < 0)
            {
                return -1;
            }

            return ResourceDirectoryReader.ReadAtOffset(fs, resourceOffset, 0, -1L, () =>
            {
                IMAGERESOURCEDIRECTORY rootDirectory = ResourceDirectoryReader.ReadDirectory(reader);
                int totalEntries = rootDirectory.NumberOfNamedEntries + rootDirectory.NumberOfIdEntries;

                for (int i = 0; i < totalEntries; i++)
                {
                    if (!ResourceDirectoryReader.TryReadEntry(fs, reader, resourceOffset, i, out IMAGERESOURCEDIRECTORYENTRY entry))
                    {
                        break;
                    }

                    if ((entry.NameOrId & 0xFFFF) != RT_ICON_TYPE)
                    {
                        continue;
                    }

                    long nextLevelOffset = resourceOffset + (entry.OffsetToData & 0x7FFFFFFF);
                    if (nextLevelOffset < 0 || nextLevelOffset + ResourceDirectoryReader.DirectoryHeaderSize > fs.Length)
                    {
                        continue;
                    }

                    return FindSpecificIconData(fs, reader, peInfo, nextLevelOffset, resourceBaseOffset, resourceId);
                }

                return -1L;
            });
        }

        /// <summary>
        /// 解析资源数据目录（索引2）的 RVA 为文件偏移；不可用时返回 -1。
        /// </summary>
        private static long GetResourceDirectoryOffset(FileStream fs, PEInfo peInfo)
        {
            if (peInfo.OptionalHeader.DataDirectory.Length <= PEConstants.DirectoryResource ||
                peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryResource].VirtualAddress == 0)
            {
                return -1;
            }

            uint resourceRVA = peInfo.OptionalHeader.DataDirectory[PEConstants.DirectoryResource].VirtualAddress;
            long resourceOffset = Utilities.RvaToOffset(resourceRVA, peInfo.SectionHeaders);
            return resourceOffset == -1 || resourceOffset + ResourceDirectoryReader.DirectoryHeaderSize > fs.Length ? -1 : resourceOffset;
        }

        /// <summary>
        /// 读取资源名称（长度前缀的 Unicode 字符串）。
        /// </summary>
        internal static string ReadResourceName(FileStream fs, BinaryReader reader, long nameOffset)
        {
            return ResourceDirectoryReader.ReadAtOffset(fs, nameOffset, 2, string.Empty, () =>
            {
                ushort nameLength = reader.ReadUInt16();
                if (nameLength == 0 || fs.Position + ((long)nameLength * 2) > fs.Length)
                {
                    return string.Empty;
                }

                byte[] nameBytes = reader.ReadBytes(nameLength * 2);
                return Encoding.Unicode.GetString(nameBytes);
            });
        }
    }
}
