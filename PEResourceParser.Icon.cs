using System;
using System.IO;
using System.Text;

namespace MyTool
{
    /// <summary>
    /// PE资源解析器图标信息解析模块
    /// 专门负责解析PE文件中的图标资源
    /// </summary>
    public static class PEResourceParserIcon
    {
        /// <summary>
        /// 解析图标信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        public static void ParseIconInfo(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            try
            {
                // 图标信息在资源目录中，数据目录索引为#2 (IMAGE_DIRECTORY_ENTRY_RESOURCE)
                const int RESOURCE_DIRECTORY_INDEX = 2; // IMAGE_DIRECTORY_ENTRY_RESOURCE

                if (peInfo.OptionalHeader.DataDirectory.Length > RESOURCE_DIRECTORY_INDEX &&
                    peInfo.OptionalHeader.DataDirectory[RESOURCE_DIRECTORY_INDEX].VirtualAddress != 0)
                {
                    uint resourceRVA = peInfo.OptionalHeader.DataDirectory[RESOURCE_DIRECTORY_INDEX].VirtualAddress;
                    long resourceOffset = PEResourceParserCore.RvaToOffset(resourceRVA, peInfo.SectionHeaders);

                    if (resourceOffset != -1 && resourceOffset < fs.Length)
                    {
                        // 解析资源目录以找到图标信息
                        ParseResourceDirectoryForIcons(fs, reader, peInfo, resourceOffset);
                    }
                }
                else if (peInfo.CLRInfo != null)
                {
                    // 对于.NET程序集，尝试从资源中查找图标
                    ParseDotNetIcons(fs, reader, peInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"图标信息解析错误: {ex.Message}");
                // 图标信息解析错误不中断程序执行
            }
        }

        /// <summary>
        /// 解析.NET程序集中的图标
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        private static void ParseDotNetIcons(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            try
            {
                // 检查CLR资源数据目录
                if (peInfo.CLRInfo != null && peInfo.CLRInfo.HasResources)
                {
                    // 尝试解析.NET资源中的图标
                    ParseDotNetResourcesForIcons(fs, reader, peInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($".NET图标解析错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析.NET资源中的图标
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        private static void ParseDotNetResourcesForIcons(FileStream fs, BinaryReader reader, PEInfo peInfo)
        {
            try
            {
                // 获取资源RVA并转换为文件偏移
                uint resourceRVA = peInfo.OptionalHeader.DataDirectory[2].VirtualAddress; // IMAGE_DIRECTORY_ENTRY_RESOURCE
                long resourceOffset = PEResourceParserCore.RvaToOffset(resourceRVA, peInfo.SectionHeaders);
                
                if (resourceOffset != -1 && resourceOffset < fs.Length)
                {
                    // 尝试查找命名资源（WPF程序通常将图标存储为命名资源）
                    ParseResourceDirectoryForNamedIcons(fs, reader, peInfo, resourceOffset);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($".NET资源图标解析错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析资源目录以查找命名图标资源
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="resourceOffset">资源节偏移</param>
        private static void ParseResourceDirectoryForNamedIcons(FileStream fs, BinaryReader reader, PEInfo peInfo, long resourceOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = resourceOffset;

                // 读取根资源目录
                var rootDirectory = new IMAGE_RESOURCE_DIRECTORY
                {
                    Characteristics = reader.ReadUInt32(),
                    TimeDateStamp = reader.ReadUInt32(),
                    MajorVersion = reader.ReadUInt16(),
                    MinorVersion = reader.ReadUInt16(),
                    NumberOfNamedEntries = reader.ReadUInt16(),
                    NumberOfIdEntries = reader.ReadUInt16()
                };

                int totalEntries = rootDirectory.NumberOfNamedEntries + rootDirectory.NumberOfIdEntries;

                // 遍历命名资源项
                for (int i = 0; i < rootDirectory.NumberOfNamedEntries; i++)
                {
                    fs.Position = resourceOffset + 16 + i * 8; // 16是IMAGE_RESOURCE_DIRECTORY大小，每项8字节

                    var entry = new IMAGE_RESOURCE_DIRECTORY_ENTRY
                    {
                        NameOrId = reader.ReadUInt32(),
                        OffsetToData = reader.ReadUInt32()
                    };

                    // 检查是否是命名资源（最高位为1）
                    if ((entry.NameOrId & 0x80000000) != 0)
                    {
                        long nextLevelOffset = resourceOffset + (entry.OffsetToData & 0x7FFFFFFF);
                        ParseNamedResourceDirectory(fs, reader, peInfo, nextLevelOffset, resourceOffset, entry.NameOrId & 0x7FFFFFFF);
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析资源目录以查找命名图标错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析命名资源目录
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="directoryOffset">目录偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        /// <param name="nameOffset">名称偏移</param>
        private static void ParseNamedResourceDirectory(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset, uint nameOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                
                // 读取资源名称
                string resourceName = ReadResourceName(fs, reader, resourceBaseOffset + nameOffset);
                
                // 检查资源名称是否可能包含图标（如包含"icon"、".ico"等关键字）
                if (!string.IsNullOrEmpty(resourceName) && 
                    (resourceName.IndexOf("icon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     resourceName.IndexOf(".ico", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     resourceName.IndexOf("app", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    fs.Position = directoryOffset;
                    
                    // 读取资源目录
                    var directory = new IMAGE_RESOURCE_DIRECTORY
                    {
                        Characteristics = reader.ReadUInt32(),
                        TimeDateStamp = reader.ReadUInt32(),
                        MajorVersion = reader.ReadUInt16(),
                        MinorVersion = reader.ReadUInt16(),
                        NumberOfNamedEntries = reader.ReadUInt16(),
                        NumberOfIdEntries = reader.ReadUInt16()
                    };

                    // 遍历子项
                    int totalEntries = directory.NumberOfNamedEntries + directory.NumberOfIdEntries;
                    for (int i = 0; i < totalEntries; i++)
                    {
                        fs.Position = directoryOffset + 16 + i * 8;

                        var entry = new IMAGE_RESOURCE_DIRECTORY_ENTRY
                        {
                            NameOrId = reader.ReadUInt32(),
                            OffsetToData = reader.ReadUInt32()
                        };

                        // 检查是否是叶子节点
                        if ((entry.OffsetToData & 0x80000000) != 0)
                        {
                            // 继续下一级目录
                            long nextLevelOffset = resourceBaseOffset + (entry.OffsetToData & 0x7FFFFFFF);
                            ParseNamedResourceDirectory(fs, reader, peInfo, nextLevelOffset, resourceBaseOffset, 0);
                        }
                        else
                        {
                            // 处理数据条目
                            long dataEntryOffset = resourceBaseOffset + entry.OffsetToData;
                            ParseNamedResourceDataEntry(fs, reader, peInfo, dataEntryOffset, resourceName);
                        }
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析命名资源目录错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 读取资源名称
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="nameOffset">名称偏移</param>
        /// <returns>资源名称</returns>
        private static string ReadResourceName(FileStream fs, BinaryReader reader, long nameOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = nameOffset;

                // 读取名称长度（Unicode字符数）
                if (fs.Position + 2 > fs.Length)
                {
                    fs.Position = originalPosition;
                    return string.Empty;
                }

                ushort nameLength = reader.ReadUInt16();
                if (nameLength == 0 || fs.Position + nameLength * 2 > fs.Length)
                {
                    fs.Position = originalPosition;
                    return string.Empty;
                }

                // 读取名称（Unicode字符串）
                byte[] nameBytes = reader.ReadBytes(nameLength * 2);
                string resourceName = Encoding.Unicode.GetString(nameBytes);
                
                fs.Position = originalPosition;
                return resourceName;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 解析命名资源数据项
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="dataEntryOffset">数据项偏移</param>
        /// <param name="resourceName">资源名称</param>
        private static void ParseNamedResourceDataEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset, string resourceName)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = dataEntryOffset;

                // 检查是否有足够的数据读取IMAGE_RESOURCE_DATA_ENTRY
                if (fs.Position + 16 > fs.Length)
                {
                    return;
                }

                // 读取资源数据项
                var dataEntry = new IMAGE_RESOURCE_DATA_ENTRY
                {
                    OffsetToData = reader.ReadUInt32(),
                    Size = reader.ReadUInt32(),
                    CodePage = reader.ReadUInt32(),
                    Reserved = reader.ReadUInt32()
                };

                // 计算实际数据偏移（注意：资源数据的OffsetToData是RVA）
                long dataOffset = PEResourceParserCore.RvaToOffset(dataEntry.OffsetToData, peInfo.SectionHeaders);
                if (dataOffset != -1 && dataOffset < fs.Length && dataEntry.Size > 0)
                {
                    // 读取资源数据
                    fs.Position = dataOffset;
                    if (dataEntry.Size <= fs.Length && dataOffset + dataEntry.Size <= fs.Length)
                    {
                        byte[] resourceData = reader.ReadBytes((int)dataEntry.Size);
                        
                        // 检查数据是否可能是图标数据
                        if (IsIconData(resourceData))
                        {
                            // 处理图标数据
                            ProcessIconData(peInfo, resourceData, resourceName);
                        }
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析命名资源数据项错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查数据是否可能是图标数据
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns>是否可能是图标数据</returns>
        private static bool IsIconData(byte[] data)
        {
            try
            {
                if (data == null || data.Length < 4)
                    return false;

                // 检查是否是ICO文件头
                if (data.Length >= 4 && 
                    data[0] == 0x00 && data[1] == 0x00 && // Reserved
                    data[2] == 0x01 && data[3] == 0x00)   // Type (1 = ICO)
                {
                    return true;
                }

                // 检查是否是有效的DIB数据（以BITMAPINFOHEADER开始）
                if (data.Length >= 4)
                {
                    // BITMAPINFOHEADER的biSize字段应该是40字节
                    uint biSize = BitConverter.ToUInt32(data, 0);
                    if (biSize == 40 && data.Length >= 16)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 处理图标数据
        /// </summary>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="iconData">图标数据</param>
        /// <param name="resourceName">资源名称</param>
        private static void ProcessIconData(PEInfo peInfo, byte[] iconData, string resourceName)
        {
            try
            {
                // 检查是否已经是完整的ICO文件
                if (iconData.Length >= 4 && 
                    iconData[0] == 0x00 && iconData[1] == 0x00 && // Reserved
                    iconData[2] == 0x01 && iconData[3] == 0x00)   // Type (1 = ICO)
                {
                    // 已经是完整的ICO文件，直接使用
                    var iconInfo = new IconInfo
                    {
                        Width = 0, // 从ICO文件头中提取
                        Height = 0, // 从ICO文件头中提取
                        BitsPerPixel = 0, // 从ICO文件头中提取
                        Size = iconData.Length,
                        Data = iconData
                    };

                    // 尝试从ICO文件头中提取信息
                    if (iconData.Length >= 22)
                    {
                        // 读取第一项目录信息
                        byte width = iconData[6];
                        byte height = iconData[7];
                        ushort bitCount = BitConverter.ToUInt16(iconData, 12);
                        
                        iconInfo.Width = width == 0 ? 256 : width;
                        iconInfo.Height = height == 0 ? 256 : height;
                        iconInfo.BitsPerPixel = bitCount;
                    }

                    peInfo.Icons.Add(iconInfo);
                }
                else
                {
                    // 是DIB数据，需要转换为ICO格式
                    ConvertDibToIco(peInfo, iconData, resourceName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理图标数据错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 将DIB数据转换为ICO格式
        /// </summary>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="dibData">DIB数据</param>
        /// <param name="resourceName">资源名称</param>
        private static void ConvertDibToIco(PEInfo peInfo, byte[] dibData, string resourceName)
        {
            try
            {
                // 检查BITMAPINFOHEADER
                if (dibData.Length < 40)
                    return;

                uint biSize = BitConverter.ToUInt32(dibData, 0);
                if (biSize != 40)
                    return;

                // 从BITMAPINFOHEADER中提取宽度和高度
                int width = BitConverter.ToInt32(dibData, 4);
                int height = BitConverter.ToInt32(dibData, 8);
                // 高度是实际高度的两倍（包含遮罩）
                height /= 2;
                
                // 从BITMAPINFOHEADER中提取色深
                ushort bitCount = BitConverter.ToUInt16(dibData, 14);
                
                // 构建完整的ICO文件数据
                int fullIconDataSize = 6 + 16 + dibData.Length;
                if (fullIconDataSize > 0 && fullIconDataSize < 10 * 1024 * 1024) // 限制最大10MB
                {
                    byte[] fullIconData = new byte[fullIconDataSize];
                    
                    // 写入ICO文件头 (6字节)
                    fullIconData[0] = 0x00; // Reserved
                    fullIconData[1] = 0x00; // Reserved
                    fullIconData[2] = 0x01; // Type (1 = ICO)
                    fullIconData[3] = 0x00; // Type
                    fullIconData[4] = 0x01; // Count (1个图标)
                    fullIconData[5] = 0x00; // Count
                
                    // 写入目录项 (16字节)
                    fullIconData[6] = (byte)(width & 0xFF);  // Width
                    fullIconData[7] = (byte)(height & 0xFF); // Height
                    fullIconData[8] = 0; // ColorCount
                    fullIconData[9] = 0; // Reserved
                    BitConverter.GetBytes((ushort)1).CopyTo(fullIconData, 10); // Planes
                    BitConverter.GetBytes(bitCount).CopyTo(fullIconData, 12); // BitCount
                    BitConverter.GetBytes((uint)dibData.Length).CopyTo(fullIconData, 14); // BytesInRes
                    // 图像数据偏移量 (从文件开始到图像数据的偏移量)
                    uint imageDataOffset = 6 + 16; // 文件头 + 目录项
                    BitConverter.GetBytes(imageDataOffset).CopyTo(fullIconData, 18);
                    
                    // 复制图像数据
                    Array.Copy(dibData, 0, fullIconData, 6 + 16, dibData.Length);
                    
                    var iconInfo = new IconInfo
                    {
                        Width = width,
                        Height = height,
                        BitsPerPixel = bitCount,
                        Size = fullIconDataSize,
                        Data = fullIconData
                    };

                    peInfo.Icons.Add(iconInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转换DIB到ICO错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析资源目录以查找图标信息
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="resourceOffset">资源节偏移</param>
        private static void ParseResourceDirectoryForIcons(FileStream fs, BinaryReader reader, PEInfo peInfo, long resourceOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = resourceOffset;

                // 读取根资源目录
                var rootDirectory = new IMAGE_RESOURCE_DIRECTORY
                {
                    Characteristics = reader.ReadUInt32(),
                    TimeDateStamp = reader.ReadUInt32(),
                    MajorVersion = reader.ReadUInt16(),
                    MinorVersion = reader.ReadUInt16(),
                    NumberOfNamedEntries = reader.ReadUInt16(),
                    NumberOfIdEntries = reader.ReadUInt16()
                };

                int totalEntries = rootDirectory.NumberOfNamedEntries + rootDirectory.NumberOfIdEntries;

                // 遍历资源目录项查找RT_GROUP_ICON资源类型 (ID = 14)
                bool foundGroupIcon = false;
                for (int i = 0; i < totalEntries; i++)
                {
                    fs.Position = resourceOffset + 16 + i * 8; // 16是IMAGE_RESOURCE_DIRECTORY大小，每项8字节

                    var entry = new IMAGE_RESOURCE_DIRECTORY_ENTRY
                    {
                        NameOrId = reader.ReadUInt32(),
                        OffsetToData = reader.ReadUInt32()
                    };

                    // 检查是否是RT_GROUP_ICON资源类型 (ID = 14)
                    if ((entry.NameOrId & 0xFFFF) == 14) // RT_GROUP_ICON = 14
                    {
                        long nextLevelOffset = resourceOffset + (entry.OffsetToData & 0x7FFFFFFF);
                        ParseGroupIconResource(fs, reader, peInfo, nextLevelOffset, resourceOffset);
                        foundGroupIcon = true;
                    }
                }

                // 如果没有找到RT_GROUP_ICON，尝试其他方法
                if (!foundGroupIcon)
                {
                    // 尝试查找RT_ICON资源类型 (ID = 3)
                    for (int i = 0; i < totalEntries; i++)
                    {
                        fs.Position = resourceOffset + 16 + i * 8;

                        var entry = new IMAGE_RESOURCE_DIRECTORY_ENTRY
                        {
                            NameOrId = reader.ReadUInt32(),
                            OffsetToData = reader.ReadUInt32()
                        };

                        // 检查是否是RT_ICON资源类型 (ID = 3)
                        if ((entry.NameOrId & 0xFFFF) == 3) // RT_ICON = 3
                        {
                            // 处理直接的RT_ICON资源
                            long nextLevelOffset = resourceOffset + (entry.OffsetToData & 0x7FFFFFFF);
                            ParseDirectIconResource(fs, reader, peInfo, nextLevelOffset, resourceOffset);
                        }
                    }
                    
                    // 尝试查找命名资源（WPF和其他.NET程序可能将图标存储为命名资源）
                    for (int i = 0; i < rootDirectory.NumberOfNamedEntries; i++)
                    {
                        fs.Position = resourceOffset + 16 + i * 8;

                        var entry = new IMAGE_RESOURCE_DIRECTORY_ENTRY
                        {
                            NameOrId = reader.ReadUInt32(),
                            OffsetToData = reader.ReadUInt32()
                        };

                        // 检查是否是命名资源（最高位为1）
                        if ((entry.NameOrId & 0x80000000) != 0)
                        {
                            // 这是一个命名资源，需要进一步检查
                            long nextLevelOffset = resourceOffset + (entry.OffsetToData & 0x7FFFFFFF);
                            ParseNamedResourceForIcons(fs, reader, peInfo, nextLevelOffset, resourceOffset);
                        }
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析资源目录以查找图标信息错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析命名资源以查找图标
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="directoryOffset">目录偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        private static void ParseNamedResourceForIcons(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = directoryOffset;

                // 读取资源目录
                var directory = new IMAGE_RESOURCE_DIRECTORY
                {
                    Characteristics = reader.ReadUInt32(),
                    TimeDateStamp = reader.ReadUInt32(),
                    MajorVersion = reader.ReadUInt16(),
                    MinorVersion = reader.ReadUInt16(),
                    NumberOfNamedEntries = reader.ReadUInt16(),
                    NumberOfIdEntries = reader.ReadUInt16()
                };

                // 遍历子项
                int totalEntries = directory.NumberOfNamedEntries + directory.NumberOfIdEntries;
                for (int i = 0; i < totalEntries; i++)
                {
                    fs.Position = directoryOffset + 16 + i * 8;

                    var entry = new IMAGE_RESOURCE_DIRECTORY_ENTRY
                    {
                        NameOrId = reader.ReadUInt32(),
                        OffsetToData = reader.ReadUInt32()
                    };

                    // 检查是否是叶子节点
                    if ((entry.OffsetToData & 0x80000000) != 0)
                    {
                        // 继续下一级目录
                        long nextLevelOffset = resourceBaseOffset + (entry.OffsetToData & 0x7FFFFFFF);
                        ParseNamedResourceForIcons(fs, reader, peInfo, nextLevelOffset, resourceBaseOffset);
                    }
                    else
                    {
                        // 检查数据条目
                        long dataEntryOffset = resourceBaseOffset + entry.OffsetToData;
                        ParseNamedResourceDataEntry(fs, reader, peInfo, dataEntryOffset, resourceBaseOffset);
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析命名资源以查找图标错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析命名资源数据项
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="dataEntryOffset">数据项偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        private static void ParseNamedResourceDataEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset, long resourceBaseOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = dataEntryOffset;

                // 检查是否有足够的数据读取IMAGE_RESOURCE_DATA_ENTRY
                if (fs.Position + 16 > fs.Length)
                {
                    return;
                }

                // 读取资源数据项
                var dataEntry = new IMAGE_RESOURCE_DATA_ENTRY
                {
                    OffsetToData = reader.ReadUInt32(),
                    Size = reader.ReadUInt32(),
                    CodePage = reader.ReadUInt32(),
                    Reserved = reader.ReadUInt32()
                };

                // 计算实际数据偏移（注意：资源数据的OffsetToData是RVA）
                long dataOffset = PEResourceParserCore.RvaToOffset(dataEntry.OffsetToData, peInfo.SectionHeaders);
                if (dataOffset != -1 && dataOffset < fs.Length && dataEntry.Size > 0)
                {
                    // 读取资源数据
                    fs.Position = dataOffset;
                    if (dataEntry.Size <= fs.Length && dataOffset + dataEntry.Size <= fs.Length)
                    {
                        byte[] resourceData = reader.ReadBytes((int)dataEntry.Size);
                        
                        // 检查数据是否可能是图标数据
                        if (IsLikelyIconData(resourceData))
                        {
                            // 处理可能的图标数据
                            ProcessPossibleIconData(peInfo, resourceData, dataEntry.Size);
                        }
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析命名资源数据项错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查数据是否可能是图标数据
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns>是否可能是图标数据</returns>
        private static bool IsLikelyIconData(byte[] data)
        {
            try
            {
                if (data == null || data.Length < 4)
                    return false;

                // 检查是否是ICO文件头
                if (data.Length >= 4 && 
                    data[0] == 0x00 && data[1] == 0x00 && // Reserved
                    data[2] == 0x01 && data[3] == 0x00)   // Type (1 = ICO)
                {
                    return true;
                }

                // 检查是否是BMP文件头
                if (data.Length >= 2 &&
                    data[0] == 0x42 && data[1] == 0x4D) // BM
                {
                    return true;
                }

                // 检查是否是有效的DIB数据（以BITMAPINFOHEADER开始）
                if (data.Length >= 4)
                {
                    // BITMAPINFOHEADER的biSize字段应该是40字节
                    uint biSize = BitConverter.ToUInt32(data, 0);
                    if (biSize == 40 && data.Length >= 16)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 处理可能的图标数据
        /// </summary>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="data">数据</param>
        /// <param name="size">数据大小</param>
        private static void ProcessPossibleIconData(PEInfo peInfo, byte[] data, uint size)
        {
            try
            {
                // 检查是否是ICO文件
                if (data.Length >= 4 && 
                    data[0] == 0x00 && data[1] == 0x00 && // Reserved
                    data[2] == 0x01 && data[3] == 0x00)   // Type (1 = ICO)
                {
                    // 已经是ICO文件，直接添加
                    var iconInfo = new IconInfo
                    {
                        Width = 0,
                        Height = 0,
                        BitsPerPixel = 0,
                        Size = (int)size,
                        Data = data
                    };

                    // 尝试从ICO文件头中提取信息
                    if (data.Length >= 22) // 至少包含一个图标目录项
                    {
                        // 读取第一项目录信息
                        byte width = data[6];
                        byte height = data[7];
                        ushort bitCount = BitConverter.ToUInt16(data, 12);
                        
                        iconInfo.Width = width == 0 ? 256 : width;
                        iconInfo.Height = height == 0 ? 256 : height;
                        iconInfo.BitsPerPixel = bitCount;
                        
                        // 验证图标尺寸，避免添加无效图标
                        if (iconInfo.Width > 0 && iconInfo.Height > 0)
                        {
                            peInfo.Icons.Add(iconInfo);
                        }
                    }
                    else
                    {
                        // 无法解析的ICO文件，但仍可能有效
                        peInfo.Icons.Add(iconInfo);
                    }
                    return;
                }

                // 检查是否是BMP文件
                if (data.Length >= 2 && data[0] == 0x42 && data[1] == 0x4D) // BM
                {
                    // 是BMP文件，转换为ICO格式
                    ConvertBmpToIco(peInfo, data);
                    return;
                }

                // 检查是否是DIB数据
                if (data.Length >= 4)
                {
                    uint biSize = BitConverter.ToUInt32(data, 0);
                    if (biSize == 40) // BITMAPINFOHEADER
                    {
                        // 是DIB数据，转换为ICO格式
                        ConvertDibToIco(peInfo, data);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理可能的图标数据错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 将BMP数据转换为ICO格式
        /// </summary>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="bmpData">BMP数据</param>
        private static void ConvertBmpToIco(PEInfo peInfo, byte[] bmpData)
        {
            try
            {
                // BMP文件头(14字节) + BITMAPINFOHEADER(40字节) = 54字节
                if (bmpData.Length < 54)
                    return;

                // 从BITMAPINFOHEADER中提取宽度和高度
                int width = BitConverter.ToInt32(bmpData, 18); // BMP文件头14字节 + BITMAPINFOHEADER偏移4字节
                int height = BitConverter.ToInt32(bmpData, 22); // BMP文件头14字节 + BITMAPINFOHEADER偏移8字节
                // 高度是实际高度的两倍（包含遮罩）
                height /= 2;
                
                // 从BITMAPINFOHEADER中提取色深
                ushort bitCount = BitConverter.ToUInt16(bmpData, 28); // BMP文件头14字节 + BITMAPINFOHEADER偏移14字节
                
                // 验证尺寸有效性
                if (width <= 0 || height <= 0)
                    return;
                
                // 构建完整的ICO文件数据
                // 需要去掉BMP文件头(14字节)
                int dibDataSize = bmpData.Length - 14;
                int fullIconDataSize = 6 + 16 + dibDataSize;
                if (fullIconDataSize > 0 && fullIconDataSize < 10 * 1024 * 1024) // 限制最大10MB
                {
                    byte[] fullIconData = new byte[fullIconDataSize];
                    
                    // 写入ICO文件头 (6字节)
                    fullIconData[0] = 0x00; // Reserved
                    fullIconData[1] = 0x00; // Reserved
                    fullIconData[2] = 0x01; // Type (1 = ICO)
                    fullIconData[3] = 0x00; // Type
                    fullIconData[4] = 0x01; // Count (1个图标)
                    fullIconData[5] = 0x00; // Count
                
                    // 写入目录项 (16字节)
                    fullIconData[6] = (byte)(width & 0xFF);  // Width
                    fullIconData[7] = (byte)(height & 0xFF); // Height
                    fullIconData[8] = 0; // ColorCount
                    fullIconData[9] = 0; // Reserved
                    BitConverter.GetBytes((ushort)1).CopyTo(fullIconData, 10); // Planes
                    BitConverter.GetBytes(bitCount).CopyTo(fullIconData, 12); // BitCount
                    BitConverter.GetBytes((uint)dibDataSize).CopyTo(fullIconData, 14); // BytesInRes
                    // 图像数据偏移量 (从文件开始到图像数据的偏移量)
                    uint imageDataOffset = 6 + 16; // 文件头 + 目录项
                    BitConverter.GetBytes(imageDataOffset).CopyTo(fullIconData, 18);
                    
                    // 复制图像数据（去掉BMP文件头）
                    Array.Copy(bmpData, 14, fullIconData, 6 + 16, dibDataSize);
                    
                    var iconInfo = new IconInfo
                    {
                        Width = width,
                        Height = height,
                        BitsPerPixel = bitCount,
                        Size = fullIconDataSize,
                        Data = fullIconData
                    };

                    peInfo.Icons.Add(iconInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转换BMP到ICO错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 将DIB数据转换为ICO格式
        /// </summary>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="dibData">DIB数据</param>
        private static void ConvertDibToIco(PEInfo peInfo, byte[] dibData)
        {
            try
            {
                // 检查BITMAPINFOHEADER
                if (dibData.Length < 40)
                    return;

                uint biSize = BitConverter.ToUInt32(dibData, 0);
                if (biSize != 40)
                    return;

                // 从BITMAPINFOHEADER中提取宽度和高度
                int width = BitConverter.ToInt32(dibData, 4);
                int height = BitConverter.ToInt32(dibData, 8);
                // 高度是实际高度的两倍（包含遮罩）
                height /= 2;
                
                // 从BITMAPINFOHEADER中提取色深
                ushort bitCount = BitConverter.ToUInt16(dibData, 14);
                
                // 验证尺寸有效性
                if (width <= 0 || height <= 0)
                    return;
                
                // 构建完整的ICO文件数据
                int fullIconDataSize = 6 + 16 + dibData.Length;
                if (fullIconDataSize > 0 && fullIconDataSize < 10 * 1024 * 1024) // 限制最大10MB
                {
                    byte[] fullIconData = new byte[fullIconDataSize];
                    
                    // 写入ICO文件头 (6字节)
                    fullIconData[0] = 0x00; // Reserved
                    fullIconData[1] = 0x00; // Reserved
                    fullIconData[2] = 0x01; // Type (1 = ICO)
                    fullIconData[3] = 0x00; // Type
                    fullIconData[4] = 0x01; // Count (1个图标)
                    fullIconData[5] = 0x00; // Count
                
                    // 写入目录项 (16字节)
                    fullIconData[6] = (byte)(width & 0xFF);  // Width
                    fullIconData[7] = (byte)(height & 0xFF); // Height
                    fullIconData[8] = 0; // ColorCount
                    fullIconData[9] = 0; // Reserved
                    BitConverter.GetBytes((ushort)1).CopyTo(fullIconData, 10); // Planes
                    BitConverter.GetBytes(bitCount).CopyTo(fullIconData, 12); // BitCount
                    BitConverter.GetBytes((uint)dibData.Length).CopyTo(fullIconData, 14); // BytesInRes
                    // 图像数据偏移量 (从文件开始到图像数据的偏移量)
                    uint imageDataOffset = 6 + 16; // 文件头 + 目录项
                    BitConverter.GetBytes(imageDataOffset).CopyTo(fullIconData, 18);
                    
                    // 复制图像数据
                    Array.Copy(dibData, 0, fullIconData, 6 + 16, dibData.Length);
                    
                    var iconInfo = new IconInfo
                    {
                        Width = width,
                        Height = height,
                        BitsPerPixel = bitCount,
                        Size = fullIconDataSize,
                        Data = fullIconData
                    };

                    peInfo.Icons.Add(iconInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"转换DIB到ICO错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析直接的RT_ICON资源（非GROUP_ICON中的）
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="directoryOffset">目录偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        private static void ParseDirectIconResource(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = directoryOffset;

                // 读取资源目录
                var directory = new IMAGE_RESOURCE_DIRECTORY
                {
                    Characteristics = reader.ReadUInt32(),
                    TimeDateStamp = reader.ReadUInt32(),
                    MajorVersion = reader.ReadUInt16(),
                    MinorVersion = reader.ReadUInt16(),
                    NumberOfNamedEntries = reader.ReadUInt16(),
                    NumberOfIdEntries = reader.ReadUInt16()
                };

                // 遍历子项查找语言节点
                int totalEntries = directory.NumberOfNamedEntries + directory.NumberOfIdEntries;
                for (int i = 0; i < totalEntries; i++)
                {
                    fs.Position = directoryOffset + 16 + i * 8;

                    var entry = new IMAGE_RESOURCE_DIRECTORY_ENTRY
                    {
                        NameOrId = reader.ReadUInt32(),
                        OffsetToData = reader.ReadUInt32()
                    };

                    // 检查是否是叶子节点
                    if ((entry.OffsetToData & 0x80000000) != 0)
                    {
                        // 继续下一级目录
                        long nextLevelOffset = resourceBaseOffset + (entry.OffsetToData & 0x7FFFFFFF);
                        ParseDirectIconResource(fs, reader, peInfo, nextLevelOffset, resourceBaseOffset);
                    }
                    else
                    {
                        // 处理数据条目
                        long dataEntryOffset = resourceBaseOffset + entry.OffsetToData;
                        ParseDirectIconDataEntry(fs, reader, peInfo, dataEntryOffset, resourceBaseOffset);
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析直接图标资源错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析直接的RT_ICON数据项
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="dataEntryOffset">数据项偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        private static void ParseDirectIconDataEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset, long resourceBaseOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = dataEntryOffset;

                // 检查是否有足够的数据读取IMAGE_RESOURCE_DATA_ENTRY
                if (fs.Position + 16 > fs.Length)
                {
                    return;
                }

                // 读取资源数据项
                var dataEntry = new IMAGE_RESOURCE_DATA_ENTRY
                {
                    OffsetToData = reader.ReadUInt32(),
                    Size = reader.ReadUInt32(),
                    CodePage = reader.ReadUInt32(),
                    Reserved = reader.ReadUInt32()
                };

                // 计算实际数据偏移（注意：资源数据的OffsetToData是RVA）
                long dataOffset = PEResourceParserCore.RvaToOffset(dataEntry.OffsetToData, peInfo.SectionHeaders);
                if (dataOffset != -1 && dataOffset < fs.Length && dataEntry.Size > 0)
                {
                    // 读取图标数据
                    fs.Position = dataOffset;
                    if (dataEntry.Size <= fs.Length && dataOffset + dataEntry.Size <= fs.Length)
                    {
                        byte[] iconData = reader.ReadBytes((int)dataEntry.Size);
                        
                        // 检查数据是否可能是图标数据
                        if (IsLikelyIconData(iconData))
                        {
                            // 处理可能的图标数据
                            ProcessPossibleIconData(peInfo, iconData, dataEntry.Size);
                        }
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析直接图标数据项错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析组图标资源
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="directoryOffset">目录偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        private static void ParseGroupIconResource(FileStream fs, BinaryReader reader, PEInfo peInfo, long directoryOffset, long resourceBaseOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = directoryOffset;

                // 读取资源目录
                var directory = new IMAGE_RESOURCE_DIRECTORY
                {
                    Characteristics = reader.ReadUInt32(),
                    TimeDateStamp = reader.ReadUInt32(),
                    MajorVersion = reader.ReadUInt16(),
                    MinorVersion = reader.ReadUInt16(),
                    NumberOfNamedEntries = reader.ReadUInt16(),
                    NumberOfIdEntries = reader.ReadUInt16()
                };

                // 遍历子项查找语言节点
                int totalEntries = directory.NumberOfNamedEntries + directory.NumberOfIdEntries;
                for (int i = 0; i < totalEntries; i++)
                {
                    fs.Position = directoryOffset + 16 + i * 8; // 跳过目录头(16字节)，每项8字节

                    var entry = new IMAGE_RESOURCE_DIRECTORY_ENTRY
                    {
                        NameOrId = reader.ReadUInt32(),
                        OffsetToData = reader.ReadUInt32()
                    };

                    // 检查是否是叶子节点
                    // 最高位为1表示指向下一级目录，为0表示指向数据条目
                    if ((entry.OffsetToData & 0x80000000) != 0)
                    {
                        // 最高位为1，表示指向下一级目录
                        // 清除最高位得到实际偏移
                        long nextLevelOffset = resourceBaseOffset + (entry.OffsetToData & 0x7FFFFFFF);
                        // 递归处理下一级目录
                        ParseGroupIconResource(fs, reader, peInfo, nextLevelOffset, resourceBaseOffset);
                    }
                    else
                    {
                        // 最高位为0，表示指向数据条目
                        long dataEntryOffset = resourceBaseOffset + entry.OffsetToData;
                        ParseGroupIconDataEntry(fs, reader, peInfo, dataEntryOffset, resourceBaseOffset);
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析组图标资源错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 解析组图标数据项
        /// </summary>
        /// <param name="fs">文件流</param>
        /// <param name="reader">二进制读取器</param>
        /// <param name="peInfo">PE文件信息</param>
        /// <param name="dataEntryOffset">数据项偏移</param>
        /// <param name="resourceBaseOffset">资源基址偏移</param>
        private static void ParseGroupIconDataEntry(FileStream fs, BinaryReader reader, PEInfo peInfo, long dataEntryOffset, long resourceBaseOffset)
        {
            try
            {
                long originalPosition = fs.Position;
                fs.Position = dataEntryOffset;

                // 检查是否有足够的数据读取IMAGE_RESOURCE_DATA_ENTRY
                if (fs.Position + 16 > fs.Length)
                {
                    return;
                }

                // 读取资源数据项
                var dataEntry = new IMAGE_RESOURCE_DATA_ENTRY
                {
                    OffsetToData = reader.ReadUInt32(),
                    Size = reader.ReadUInt32(),
                    CodePage = reader.ReadUInt32(),
                    Reserved = reader.ReadUInt32()
                };

                // 计算实际数据偏移（注意：资源数据的OffsetToData是RVA）
                long dataOffset = PEResourceParserCore.RvaToOffset(dataEntry.OffsetToData, peInfo.SectionHeaders);
                if (dataOffset != -1 && dataOffset < fs.Length && dataEntry.Size > 0)
                {
                    fs.Position = dataOffset;

                    // 读取图标目录头
                    if (fs.Position + 6 > fs.Length)
                        return;

                    var iconDirHeader = new ICON_DIR_HEADER
                    {
                        Reserved = reader.ReadUInt16(),
                        Type = reader.ReadUInt16(),
                        Count = reader.ReadUInt16()
                    };

                    // 检查是否为有效的图标资源
                    if (iconDirHeader.Type != 1 || iconDirHeader.Count == 0)
                        return;

                    // 读取图标目录项
                    var iconDirEntries = new ICON_DIR_ENTRY[iconDirHeader.Count];
                    for (int i = 0; i < iconDirHeader.Count; i++)
                    {
                        if (fs.Position + 16 > fs.Length)
                            break;

                        iconDirEntries[i] = new ICON_DIR_ENTRY
                        {
                            Width = reader.ReadByte(),
                            Height = reader.ReadByte(),
                            ColorCount = reader.ReadByte(),
                            Reserved = reader.ReadByte(),
                            Planes = reader.ReadUInt16(),
                            BitCount = reader.ReadUInt16(),
                            BytesInRes = reader.ReadUInt32(),
                            ImageOffset = reader.ReadUInt32()
                        };
                    }

                    // 为每个图标目录项解析实际的图标数据
                    foreach (var entry in iconDirEntries)
                    {
                        // 查找对应ID的RT_ICON资源
                        long iconDataOffset = PEResourceParserIconHelpers.FindIconDataByResourceId(fs, reader, peInfo, entry.ImageOffset, resourceBaseOffset);
                        if (iconDataOffset != -1)
                        {
                            // 验证图标尺寸，避免添加无效图标
                            int width = entry.Width == 0 ? 256 : entry.Width;
                            int height = entry.Height == 0 ? 256 : entry.Height;
                            
                            if (width <= 0 || height <= 0)
                                continue;
                                
                            var iconInfo = new IconInfo
                            {
                                Width = width,
                                Height = height,
                                BitsPerPixel = entry.BitCount,
                                Size = 6 + 16 + (int)entry.BytesInRes  // ICO文件头(6字节) + 目录项(16字节) + 图像数据
                            };

                            // 构建完整的ICO文件数据
                            // ICO文件结构: 6字节文件头 + 16字节目录项 + 图像数据
                            int fullIconDataSize = 6 + 16 + (int)entry.BytesInRes;
                            if (fullIconDataSize > 0 && fullIconDataSize < 10 * 1024 * 1024) // 限制最大10MB
                            {
                                byte[] fullIconData = new byte[fullIconDataSize];
                                
                                // 写入ICO文件头 (6字节)
                                fullIconData[0] = 0x00; // Reserved
                                fullIconData[1] = 0x00; // Reserved
                                fullIconData[2] = 0x01; // Type (1 = ICO)
                                fullIconData[3] = 0x00; // Type
                                fullIconData[4] = 0x01; // Count (1个图标)
                                fullIconData[5] = 0x00; // Count
                            
                                // 写入目录项 (16字节)
                                fullIconData[6] = entry.Width;
                                fullIconData[7] = entry.Height;
                                fullIconData[8] = entry.ColorCount;
                                fullIconData[9] = entry.Reserved;
                                BitConverter.GetBytes(entry.Planes).CopyTo(fullIconData, 10);
                                BitConverter.GetBytes(entry.BitCount).CopyTo(fullIconData, 12);
                                BitConverter.GetBytes(entry.BytesInRes).CopyTo(fullIconData, 14);
                                // 图像数据偏移量 (从文件开始到图像数据的偏移量)
                                uint imageDataOffset = 6 + 16; // 文件头 + 目录项
                                BitConverter.GetBytes(imageDataOffset).CopyTo(fullIconData, 18);
                            
                                // 读取并写入图像数据
                                if (entry.BytesInRes > 0 && entry.BytesInRes <= fs.Length && iconDataOffset + entry.BytesInRes <= fs.Length)
                                {
                                    fs.Position = iconDataOffset;
                                    byte[] imageData = reader.ReadBytes((int)entry.BytesInRes);
                                    Array.Copy(imageData, 0, fullIconData, 6 + 16, imageData.Length);
                                    iconInfo.Data = fullIconData;
                                }
                                else if (entry.BytesInRes > 0 && entry.BytesInRes <= fs.Length)
                                {
                                    // 如果计算出的大小超过了文件长度，尝试读取从iconDataOffset到文件末尾的所有数据
                                    long remainingBytes = fs.Length - iconDataOffset;
                                    if (remainingBytes > 0 && iconDataOffset < fs.Length)
                                    {
                                        fs.Position = iconDataOffset;
                                        byte[] imageData = reader.ReadBytes((int)Math.Min(remainingBytes, entry.BytesInRes));
                                        Array.Copy(imageData, 0, fullIconData, 6 + 16, imageData.Length);
                                        iconInfo.Data = fullIconData;
                                    }
                                }

                                peInfo.Icons.Add(iconInfo);
                            }
                        }
                    }
                }

                fs.Position = originalPosition;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析组图标数据项错误: {ex.Message}");
            }
        }
    }
}