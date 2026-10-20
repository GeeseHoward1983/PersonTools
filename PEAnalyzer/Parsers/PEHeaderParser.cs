using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools.PEAnalyzer.Parsers
{
    /// <summary>
    /// PE文件头解析器
    /// 专门负责解析PE文件的各种头结构
    /// </summary>
    internal static class PEHeaderParser
    {
        /// <summary>
        /// 解析DOS头
        /// </summary>
        /// <param name="reader">二进制读取器</param>
        /// <returns>DOS头结构</returns>
        internal static IMAGE_DOS_HEADER ParseDosHeader(BinaryReader reader)
        {
            return new IMAGE_DOS_HEADER
            {
                e_magic = reader.ReadUInt16(),
                e_cblp = reader.ReadUInt16(),
                e_cp = reader.ReadUInt16(),
                e_crlc = reader.ReadUInt16(),
                e_cparhdr = reader.ReadUInt16(),
                e_minalloc = reader.ReadUInt16(),
                e_maxalloc = reader.ReadUInt16(),
                e_ss = reader.ReadUInt16(),
                e_sp = reader.ReadUInt16(),
                e_csum = reader.ReadUInt16(),
                e_ip = reader.ReadUInt16(),
                e_cs = reader.ReadUInt16(),
                e_lfarlc = reader.ReadUInt16(),
                e_ovno = reader.ReadUInt16(),
                e_res1 = [reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16()],
                e_oemid = reader.ReadUInt16(),
                e_oeminfo = reader.ReadUInt16(),
                e_res2 = [
                    reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(),
                    reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16()
                ],
                e_lfanew = reader.ReadUInt32()
            };
        }

        /// <summary>
        /// 解析NT头
        /// </summary>
        /// <param name="reader">二进制读取器</param>
        /// <returns>NT头结构</returns>
        internal static IMAGE_NT_HEADERS ParseNtHeaders(BinaryReader reader)
        {
            IMAGE_NT_HEADERS ntHeaders = new()
            {
                Signature = reader.ReadUInt32(),
                FileHeader = new()
                {
                    Machine = reader.ReadUInt16(),
                    NumberOfSections = reader.ReadUInt16(),
                    TimeDateStamp = reader.ReadUInt32(),
                    PointerToSymbolTable = reader.ReadUInt32(),
                    NumberOfSymbols = reader.ReadUInt32(),
                    SizeOfOptionalHeader = reader.ReadUInt16(),
                    Characteristics = reader.ReadUInt16()
                }
            };

            return ntHeaders;
        }

        private static IMAGE_OPTIONAL_HEADER CreateNomarlOptionalHeader(BinaryReader reader)
        {
                return new()
                {
                    // 读取通用部分
                    Magic = reader.ReadUInt16(),
                    MajorLinkerVersion = reader.ReadByte(),
                    MinorLinkerVersion = reader.ReadByte(),
                    SizeOfCode = reader.ReadUInt32(),
                    SizeOfInitializedData = reader.ReadUInt32(),
                    SizeOfUninitializedData = reader.ReadUInt32(),
                    AddressOfEntryPoint = reader.ReadUInt32(),
                    BaseOfCode = reader.ReadUInt32()
                };
        }

        private static void CreateNomarlOptionalHeader2(ref IMAGE_OPTIONAL_HEADER optionalHeader, BinaryReader reader)
        {
            (optionalHeader.SectionAlignment, optionalHeader.FileAlignment, optionalHeader.MajorOperatingSystemVersion, optionalHeader.MinorOperatingSystemVersion, optionalHeader.MajorImageVersion, optionalHeader.MinorImageVersion, optionalHeader.MajorSubsystemVersion, optionalHeader.MinorSubsystemVersion, optionalHeader.Win32VersionValue, optionalHeader.SizeOfImage, optionalHeader.SizeOfHeaders, optionalHeader.CheckSum, optionalHeader.Subsystem, optionalHeader.DllCharacteristics) = (reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt16(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt16(), reader.ReadUInt16()); // 保留字段，暂不使用
        }

        /// <summary>
        /// 解析可选头
        /// </summary>
        /// <param name="reader">二进制读取器</param>
        /// <param name="sizeOfOptionalHeader">可选头大小</param>
        /// <returns>可选头结构</returns>
        internal static IMAGE_OPTIONAL_HEADER ParseOptionalHeader(BinaryReader reader, ushort sizeOfOptionalHeader)
        {
            return ParseOptionalHeaderCore(reader, sizeOfOptionalHeader, reader.BaseStream.Position);
        }

        private static IMAGE_OPTIONAL_HEADER ParseOptionalHeaderCore(BinaryReader reader, ushort sizeOfOptionalHeader, long optionalHeaderStart)
        {
            IMAGE_OPTIONAL_HEADER optionalHeader = CreateNomarlOptionalHeader(reader);

            ParseOptionalHeaderCore2(optionalHeader, reader, sizeOfOptionalHeader, optionalHeaderStart);

            return optionalHeader;
        }

        private static void ParseOptionalHeaderCore2(IMAGE_OPTIONAL_HEADER optionalHeader, BinaryReader reader, ushort sizeOfOptionalHeader, long optionalHeaderStart)
        {
            ParseOptionalHeaderCore3(optionalHeader, reader, sizeOfOptionalHeader, optionalHeaderStart, ValidateOptionalHeaderMagic(optionalHeader.Magic, sizeOfOptionalHeader));
        }

        private static void ParseOptionalHeaderCore3(IMAGE_OPTIONAL_HEADER optionalHeader, BinaryReader reader, ushort sizeOfOptionalHeader, long optionalHeaderStart, bool is64Bit)
        {
            ReadImageBaseFields(reader, ref optionalHeader, is64Bit);
            ParseOptionalHeaderCore4(optionalHeader, reader, sizeOfOptionalHeader, optionalHeaderStart, is64Bit);
        }

        private static void ParseOptionalHeaderCore4(IMAGE_OPTIONAL_HEADER optionalHeader, BinaryReader reader, ushort sizeOfOptionalHeader, long optionalHeaderStart, bool is64Bit)
        {
            CreateNomarlOptionalHeader2(ref optionalHeader, reader);
            ParseOptionalHeaderCore5(optionalHeader, reader, sizeOfOptionalHeader, optionalHeaderStart, is64Bit);
        }

        private static void ParseOptionalHeaderCore5(IMAGE_OPTIONAL_HEADER optionalHeader, BinaryReader reader, ushort sizeOfOptionalHeader, long optionalHeaderStart, bool is64Bit)
        {
            ReadStackHeapFields(reader, ref optionalHeader, is64Bit);

            ParseOptionalHeaderCore6(optionalHeader, reader, sizeOfOptionalHeader, optionalHeaderStart);
        }

        private static void ParseOptionalHeaderCore6(IMAGE_OPTIONAL_HEADER optionalHeader, BinaryReader reader, ushort sizeOfOptionalHeader, long optionalHeaderStart)
        {
            (optionalHeader.LoaderFlags, optionalHeader.NumberOfRvaAndSizes, optionalHeader.DataDirectory) = (reader.ReadUInt32(), reader.ReadUInt32(), ReadDataDirectories(reader, reader.ReadUInt32(), optionalHeaderStart + sizeOfOptionalHeader));
        }

        private static bool CheckSizeOfOptionalHeader(ushort sizeOfOptionalHeader)
        {
            return sizeOfOptionalHeader > PEConstants.Pe32OptionalHeaderBaseSize;
        }

        /// <summary>
        /// 校验可选头魔数与最小长度，返回是否为 64 位（PE32+）。
        /// </summary>
        private static bool ValidateOptionalHeaderMagic(ushort magic, ushort sizeOfOptionalHeader)
        {
            return magic switch
            {
                PEConstants.Pe32Magic => CheckSizeOfOptionalHeader(sizeOfOptionalHeader) switch
                {
                    true => false,
                    _ => throw new InvalidDataException("文件不是有效的PE文件: PE32 可选头过短。"),
                },
                PEConstants.Pe32PlusMagic => CheckSizeOfOptionalHeader(sizeOfOptionalHeader) switch
                {
                    true => true,
                    _ => throw new InvalidDataException("文件不是有效的PE文件: PE32+ 可选头过短。"),
                },
                _ => throw new InvalidDataException($"文件不是有效的PE文件: Unsupported optional header magic 0x{magic:X4}.")
            };
        }

        /// <summary>
        /// 读取 ImageBase 与 BaseOfData（位数相关：PE32 各 4 字节；PE32+ 的 ImageBase 为 8 字节且无 BaseOfData）。
        /// </summary>
        private static void ReadImageBaseFields(BinaryReader reader, ref IMAGE_OPTIONAL_HEADER optionalHeader, bool is64Bit)
        {
            optionalHeader.BaseOfData = is64Bit ? 0 : optionalHeader.BaseOfData; // PE32+ 没有 BaseOfData 字段
            optionalHeader.ImageBase = reader.ReadUInt64();
        }

        /// <summary>
        /// 读取栈/堆的保留与提交大小（位数相关：PE32 为 4 字节，PE32+ 为 8 字节）。
        /// </summary>
        private static void ReadStackHeapFields(BinaryReader reader, ref IMAGE_OPTIONAL_HEADER optionalHeader, bool is64Bit)
        {
            //optionalHeader.SizeOfStackReserve = is64Bit ? reader.ReadUInt64() : reader.ReadUInt32();
            //optionalHeader.SizeOfStackCommit = is64Bit ? reader.ReadUInt64() : reader.ReadUInt32();
            //optionalHeader.SizeOfHeapReserve = is64Bit ? reader.ReadUInt64() : reader.ReadUInt32();
            //optionalHeader.SizeOfHeapCommit = is64Bit ? reader.ReadUInt64() : reader.ReadUInt32();
            (optionalHeader.SizeOfStackReserve, optionalHeader.SizeOfStackCommit, optionalHeader.SizeOfHeapReserve, optionalHeader.SizeOfHeapCommit) = is64Bit
                ? (reader.ReadUInt64(), reader.ReadUInt64(), reader.ReadUInt64(), reader.ReadUInt64())
                : (reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());
        }

        /// <summary>
        /// 读取数据目录数组（每项 8 字节，最多 16 项；项布局与位数无关）。
        /// </summary>
        private static IMAGE_DATA_DIRECTORY[] ReadDataDirectories(BinaryReader reader, uint numberOfRvaAndSizes, long optionalHeaderEnd)
        {
            int dataDirCount = (int)Math.Min(numberOfRvaAndSizes, PEConstants.MaxDataDirectories);
            // 目录项必须位于可选头范围内：按 SizeOfOptionalHeader 声明的可选头结尾再夹一次，
            // 阻止畸形文件把节表字节当作目录项（每项 8 字节）。
            long maxByOptionalHeader = (optionalHeaderEnd - reader.BaseStream.Position) / 8;
            if (maxByOptionalHeader < dataDirCount)
            {
                dataDirCount = (int)Math.Max(0, maxByOptionalHeader);
            }

            // 限制为文件实际可读的数量，避免被截断的文件触发未捕获的 EndOfStreamException
            long available = (reader.BaseStream.Length - reader.BaseStream.Position) / 8;
            if (available < dataDirCount)
            {
                // 文件截断致可读目录项少于应有数量：解析行为不变（仍按可读数量返回短数组），
                // 但记录日志，便于排查"数组长度与 Presenter 显示的 NumberOfRvaAndSizes 不一致"的现象。
                PersonalTools.Utils.AppLogger.Log(
                    $"数据目录被截断：应有 {dataDirCount} 项(NumberOfRvaAndSizes={numberOfRvaAndSizes})，文件仅可读 {Math.Max(0, available)} 项。");
                dataDirCount = (int)Math.Max(0, available);
            }

            IMAGE_DATA_DIRECTORY[] dataDirectory = new IMAGE_DATA_DIRECTORY[dataDirCount];
            for (int i = 0; i < dataDirCount; i++)
            {
                dataDirectory[i] = new IMAGE_DATA_DIRECTORY
                {
                    VirtualAddress = reader.ReadUInt32(),
                    Size = reader.ReadUInt32()
                };
            }

            return dataDirectory;
        }

        /// <summary>
        /// 解析节头
        /// </summary>
        /// <param name="reader">二进制读取器</param>
        /// <param name="numberOfSections">节的数量</param>
        /// <returns>节头列表</returns>
        internal static List<IMAGE_SECTION_HEADER> ParseSectionHeaders(BinaryReader reader, ushort numberOfSections)
        {
            List<IMAGE_SECTION_HEADER> sections = [];

            for (int i = 0; i < numberOfSections; i++)
            {
                IMAGE_SECTION_HEADER section = new()
                {
                    Name = reader.ReadBytes(8),
                    VirtualSize = reader.ReadUInt32(),
                    VirtualAddress = reader.ReadUInt32(),
                    SizeOfRawData = reader.ReadUInt32(),
                    PointerToRawData = reader.ReadUInt32(),
                    PointerToRelocations = reader.ReadUInt32(),
                    PointerToLinenumbers = reader.ReadUInt32(),
                    NumberOfRelocations = reader.ReadUInt16(),
                    NumberOfLinenumbers = reader.ReadUInt16(),
                    Characteristics = reader.ReadUInt32()
                };

                sections.Add(section);
            }

            return sections;
        }
    }
}