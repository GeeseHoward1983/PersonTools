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

        /// <summary>
        /// 解析可选头
        /// </summary>
        /// <param name="reader">二进制读取器</param>
        /// <param name="sizeOfOptionalHeader">可选头大小</param>
        /// <returns>可选头结构</returns>
        internal static IMAGE_OPTIONAL_HEADER ParseOptionalHeader(BinaryReader reader, ushort sizeOfOptionalHeader)
        {
            long optionalHeaderStart = reader.BaseStream.Position;

            IMAGE_OPTIONAL_HEADER optionalHeader = new()
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

            // 校验魔数并判定位数（PE32 / PE32+），二者的差异随后集中在专用读取方法中处理
            bool is64Bit = ValidateOptionalHeaderMagic(optionalHeader.Magic, sizeOfOptionalHeader);

            ReadImageBaseFields(reader, ref optionalHeader, is64Bit);

            optionalHeader.SectionAlignment = reader.ReadUInt32();
            optionalHeader.FileAlignment = reader.ReadUInt32();
            optionalHeader.MajorOperatingSystemVersion = reader.ReadUInt16();
            optionalHeader.MinorOperatingSystemVersion = reader.ReadUInt16();
            optionalHeader.MajorImageVersion = reader.ReadUInt16();
            optionalHeader.MinorImageVersion = reader.ReadUInt16();
            optionalHeader.MajorSubsystemVersion = reader.ReadUInt16();
            optionalHeader.MinorSubsystemVersion = reader.ReadUInt16();
            optionalHeader.Win32VersionValue = reader.ReadUInt32();
            optionalHeader.SizeOfImage = reader.ReadUInt32();
            optionalHeader.SizeOfHeaders = reader.ReadUInt32();
            optionalHeader.CheckSum = reader.ReadUInt32();
            optionalHeader.Subsystem = reader.ReadUInt16();
            optionalHeader.DllCharacteristics = reader.ReadUInt16();

            ReadStackHeapFields(reader, ref optionalHeader, is64Bit);

            optionalHeader.LoaderFlags = reader.ReadUInt32();
            optionalHeader.NumberOfRvaAndSizes = reader.ReadUInt32();

            optionalHeader.DataDirectory = ReadDataDirectories(reader, optionalHeader.NumberOfRvaAndSizes);

            // 直接定位到可选头之后（即节表起始处），不再依赖对 96/112 基准与目录数量的算术推断，
            // 对异常的 SizeOfOptionalHeader 更健壮（调用方已校验该范围在文件内）。
            reader.BaseStream.Position = optionalHeaderStart + sizeOfOptionalHeader;

            return optionalHeader;
        }

        /// <summary>
        /// 校验可选头魔数与最小长度，返回是否为 64 位（PE32+）。
        /// </summary>
        private static bool ValidateOptionalHeaderMagic(ushort magic, ushort sizeOfOptionalHeader)
        {
            if (magic == PEConstants.Pe32Magic)
            {
                if (sizeOfOptionalHeader < PEConstants.Pe32OptionalHeaderBaseSize)
                {
                    throw new InvalidDataException("文件不是有效的PE文件: PE32 可选头过短。");
                }

                return false;
            }

            if (magic == PEConstants.Pe32PlusMagic)
            {
                if (sizeOfOptionalHeader < PEConstants.Pe32PlusOptionalHeaderBaseSize)
                {
                    throw new InvalidDataException("文件不是有效的PE文件: PE32+ 可选头过短。");
                }

                return true;
            }

            throw new InvalidDataException($"文件不是有效的PE文件: Unsupported optional header magic 0x{magic:X4}.");
        }

        /// <summary>
        /// 读取 ImageBase 与 BaseOfData（位数相关：PE32 各 4 字节；PE32+ 的 ImageBase 为 8 字节且无 BaseOfData）。
        /// </summary>
        private static void ReadImageBaseFields(BinaryReader reader, ref IMAGE_OPTIONAL_HEADER optionalHeader, bool is64Bit)
        {
            if (is64Bit)
            {
                optionalHeader.BaseOfData = 0; // PE32+ 没有 BaseOfData 字段
                optionalHeader.ImageBase = reader.ReadUInt64();
            }
            else
            {
                optionalHeader.BaseOfData = reader.ReadUInt32();
                optionalHeader.ImageBase = reader.ReadUInt32();
            }
        }

        /// <summary>
        /// 读取栈/堆的保留与提交大小（位数相关：PE32 为 4 字节，PE32+ 为 8 字节）。
        /// </summary>
        private static void ReadStackHeapFields(BinaryReader reader, ref IMAGE_OPTIONAL_HEADER optionalHeader, bool is64Bit)
        {
            if (is64Bit)
            {
                optionalHeader.SizeOfStackReserve = reader.ReadUInt64();
                optionalHeader.SizeOfStackCommit = reader.ReadUInt64();
                optionalHeader.SizeOfHeapReserve = reader.ReadUInt64();
                optionalHeader.SizeOfHeapCommit = reader.ReadUInt64();
            }
            else
            {
                optionalHeader.SizeOfStackReserve = reader.ReadUInt32();
                optionalHeader.SizeOfStackCommit = reader.ReadUInt32();
                optionalHeader.SizeOfHeapReserve = reader.ReadUInt32();
                optionalHeader.SizeOfHeapCommit = reader.ReadUInt32();
            }
        }

        /// <summary>
        /// 读取数据目录数组（每项 8 字节，最多 16 项；项布局与位数无关）。
        /// </summary>
        private static IMAGE_DATA_DIRECTORY[] ReadDataDirectories(BinaryReader reader, uint numberOfRvaAndSizes)
        {
            int dataDirCount = (int)Math.Min(numberOfRvaAndSizes, PEConstants.MaxDataDirectories);
            // 限制为文件实际可读的数量，避免被截断的文件触发未捕获的 EndOfStreamException
            long available = (reader.BaseStream.Length - reader.BaseStream.Position) / 8;
            if (available < dataDirCount)
            {
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