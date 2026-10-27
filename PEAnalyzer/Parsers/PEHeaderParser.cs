using PersonalTools.PEAnalyzer.Models;
using System.Data;
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

        private static IMAGE_FILE_HEADER CreateFileHeader(BinaryReader reader)
        {
            return new IMAGE_FILE_HEADER
            {
                Machine = reader.ReadUInt16(),
                NumberOfSections = reader.ReadUInt16(),
                TimeDateStamp = reader.ReadUInt32(),
                PointerToSymbolTable = reader.ReadUInt32(),
                NumberOfSymbols = reader.ReadUInt32(),
                SizeOfOptionalHeader = reader.ReadUInt16(),
                Characteristics = reader.ReadUInt16()
            };
        }

        /// <summary>
        /// 解析NT头
        /// </summary>
        /// <param name="reader">二进制读取器</param>
        /// <returns>NT头结构</returns>
        internal static IMAGE_NT_HEADERS ParseNtHeaders(BinaryReader reader)
        {
            return new()
            {
                Signature = reader.ReadUInt32(),
                FileHeader = CreateFileHeader(reader)
            };
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
            return ParseOptionalHeaderCore2(CreateNomarlOptionalHeader(reader), reader, sizeOfOptionalHeader, optionalHeaderStart);
        }

        private static IMAGE_OPTIONAL_HEADER ParseOptionalHeaderCore2(IMAGE_OPTIONAL_HEADER optionalHeader, BinaryReader reader, ushort sizeOfOptionalHeader, long optionalHeaderStart)
        {
            ParseOptionalHeaderCore3(optionalHeader, reader, sizeOfOptionalHeader, optionalHeaderStart, ValidateOptionalHeaderMagic(optionalHeader.Magic, sizeOfOptionalHeader)); 
            return optionalHeader;
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
        private static bool ValidateOptionalHeaderMagicPe32Magic(ushort sizeOfOptionalHeader)
        {

            return CheckSizeOfOptionalHeader(sizeOfOptionalHeader) switch
            {
                true => false,
                _ => throw new InvalidDataException("文件不是有效的PE文件: PE32 可选头过短。"),
            };
        }
        private static bool ValidateOptionalHeaderMagicPe32PlusMagic(ushort sizeOfOptionalHeader)
        {

            return CheckSizeOfOptionalHeader(sizeOfOptionalHeader) switch
            {
                true => true,
                _ => throw new InvalidDataException("文件不是有效的PE文件: PE32 可选头过短。"),
            };
        }
        /// <summary>
        /// 校验可选头魔数与最小长度，返回是否为 64 位（PE32+）。
        /// </summary>
        private static bool ValidateOptionalHeaderMagic(ushort magic, ushort sizeOfOptionalHeader)
        {
            return magic switch
            {
                PEConstants.Pe32Magic => ValidateOptionalHeaderMagicPe32PlusMagic(sizeOfOptionalHeader),
                PEConstants.Pe32PlusMagic => ValidateOptionalHeaderMagicPe32PlusMagic(sizeOfOptionalHeader),
                _ => throw new InvalidDataException($"文件不是有效的PE文件: Unsupported optional header magic 0x{magic:X4}.")
            };
        }

        /// <summary>
        /// 读取 ImageBase 与 BaseOfData（位数相关：PE32 各 4 字节；PE32+ 的 ImageBase 为 8 字节且无 BaseOfData）。
        /// </summary>
        private static void ReadImageBaseFields(BinaryReader reader, ref IMAGE_OPTIONAL_HEADER optionalHeader, bool is64Bit)
        {
            (optionalHeader.BaseOfData, optionalHeader.ImageBase) = (is64Bit ? 0 : optionalHeader.BaseOfData, reader.ReadUInt64());
        }

        /// <summary>
        /// 读取栈/堆的保留与提交大小（位数相关：PE32 为 4 字节，PE32+ 为 8 字节）。
        /// </summary>
        private static void ReadStackHeapFields(BinaryReader reader, ref IMAGE_OPTIONAL_HEADER optionalHeader, bool is64Bit)
        {
            (optionalHeader.SizeOfStackReserve, optionalHeader.SizeOfStackCommit, optionalHeader.SizeOfHeapReserve, optionalHeader.SizeOfHeapCommit) = is64Bit
                ? (reader.ReadUInt64(), reader.ReadUInt64(), reader.ReadUInt64(), reader.ReadUInt64())
                : (reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32(), reader.ReadUInt32());
        }

        private static int GetDataDirCount(BinaryReader reader, uint numberOfRvaAndSizes, long optionalHeaderEnd)
        {
            return GetDataDirCount2((int)Math.Min(numberOfRvaAndSizes, PEConstants.MaxDataDirectories), (optionalHeaderEnd - reader.BaseStream.Position) / 8, (reader.BaseStream.Length - reader.BaseStream.Position) / 8);
        }

        private static int GetDataDirCount2(int dataDirCount, long maxByOptionalHeader, long available)
        {
            return GetDataDirCount3(maxByOptionalHeader < dataDirCount ? (int)Math.Max(0, maxByOptionalHeader) : dataDirCount, available);
        }

        private static int GetDataDirCount3(int dataDirCount, long available)
        {
            return available < dataDirCount ? (int)Math.Min(dataDirCount, available) : dataDirCount;
        }
        private static void SetDataDirCore(IMAGE_DATA_DIRECTORY[] dataDirectory, uint virtualAddress, uint size)
        {
            for (int i = 0; i < dataDirectory.Length; i++)
            {
                dataDirectory[i] = SetDataDir(virtualAddress, size);
            }
        }
        private static IMAGE_DATA_DIRECTORY[] SetDataDir(IMAGE_DATA_DIRECTORY[] dataDirectory, uint virtualAddress, uint size)
        {
            SetDataDirCore(dataDirectory, virtualAddress, size);
            return dataDirectory;
        }

        private static IMAGE_DATA_DIRECTORY SetDataDir(uint virtualAddress, uint size)
        {
            return new IMAGE_DATA_DIRECTORY
            {
                VirtualAddress = virtualAddress,
                Size = size
            };
        }


        /// <summary>
        /// 读取数据目录数组（每项 8 字节，最多 16 项；项布局与位数无关）。
        /// </summary>
        private static IMAGE_DATA_DIRECTORY[] ReadDataDirectories(BinaryReader reader, uint numberOfRvaAndSizes, long optionalHeaderEnd)
        {          
            return SetDataDir(new IMAGE_DATA_DIRECTORY[GetDataDirCount(reader, numberOfRvaAndSizes, optionalHeaderEnd)], reader.ReadUInt32(), reader.ReadUInt32());
        }

        private static IMAGE_SECTION_HEADER NewIMAGE_SECTION_HEADER(BinaryReader reader)
        {
            return new IMAGE_SECTION_HEADER
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
        }

        private static void SetSectionHeadersCore(List<IMAGE_SECTION_HEADER> sections, BinaryReader reader, ushort numberOfSections)
        {
            for (int i = 0; i < numberOfSections; i++)
            {
                sections.Add(NewIMAGE_SECTION_HEADER(reader));
            }
        }
        private static List<IMAGE_SECTION_HEADER> SetSectionHeaders(List<IMAGE_SECTION_HEADER> sections, BinaryReader reader, ushort numberOfSections)
        {
            SetSectionHeadersCore(sections, reader, numberOfSections);
            return sections;
        }

        /// <summary>
        /// 解析节头
        /// </summary>
        /// <param name="reader">二进制读取器</param>
        /// <param name="numberOfSections">节的数量</param>
        /// <returns>节头列表</returns>
        internal static List<IMAGE_SECTION_HEADER> ParseSectionHeaders(BinaryReader reader, ushort numberOfSections)
        {
            return SetSectionHeaders([], reader, numberOfSections);
        }
    }
}