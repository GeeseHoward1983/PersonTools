using PersonalTools.PEAnalyzer.Models;
using System.IO;

namespace PersonalTools
{
    /// <summary>
    /// PE文件头解析器
    /// 专门负责解析PE文件的各种头结构
    /// </summary>
    public static partial class PEParser
    {
        /// <summary>
        /// 解析DOS头
        /// </summary>
        /// <param name="reader">二进制读取器</param>
        /// <returns>DOS头结构</returns>
        internal static IMAGEDOSHEADER ParseDosHeader(BinaryReader reader)
        {
            return new IMAGEDOSHEADER
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
        internal static IMAGENTHEADERS ParseNtHeaders(BinaryReader reader)
        {
            IMAGENTHEADERS ntHeaders = new()
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
        internal static IMAGEOPTIONALHEADER ParseOptionalHeader(BinaryReader reader, ushort sizeOfOptionalHeader)
        {
            IMAGEOPTIONALHEADER optionalHeader = new()
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

            bool is32Bit = optionalHeader.Magic == 0x10b;
            bool is64Bit = optionalHeader.Magic == 0x20b;

            if (is32Bit)
            {
                optionalHeader.BaseOfData = reader.ReadUInt32();
                optionalHeader.ImageBase = reader.ReadUInt32();
            }
            else if (is64Bit)
            {
                optionalHeader.BaseOfData = 0; // PE32+没有BaseOfData字段
                optionalHeader.ImageBase = reader.ReadUInt64();
            }

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

            if (is32Bit)
            {
                optionalHeader.SizeOfStackReserve = reader.ReadUInt32();
                optionalHeader.SizeOfStackCommit = reader.ReadUInt32();
                optionalHeader.SizeOfHeapReserve = reader.ReadUInt32();
                optionalHeader.SizeOfHeapCommit = reader.ReadUInt32();
            }
            else if (is64Bit)
            {
                optionalHeader.SizeOfStackReserve = reader.ReadUInt64();
                optionalHeader.SizeOfStackCommit = reader.ReadUInt64();
                optionalHeader.SizeOfHeapReserve = reader.ReadUInt64();
                optionalHeader.SizeOfHeapCommit = reader.ReadUInt64();
            }

            optionalHeader.LoaderFlags = reader.ReadUInt32();
            optionalHeader.NumberOfRvaAndSizes = reader.ReadUInt32();

            // 读取数据目录
            int dataDirCount = (int)Math.Min(optionalHeader.NumberOfRvaAndSizes, 16);
            optionalHeader.DataDirectory = new IMAGEDATADIRECTORY[dataDirCount];
            for (int i = 0; i < dataDirCount; i++)
            {
                optionalHeader.DataDirectory[i] = new IMAGEDATADIRECTORY
                {
                    VirtualAddress = reader.ReadUInt32(),
                    Size = reader.ReadUInt32()
                };
            }

            // 如果数据目录不足16个，跳过剩余部分
            int remainingBytes = sizeOfOptionalHeader - (is64Bit ? 112 : 96) - (dataDirCount * 8);
            if (remainingBytes > 0)
            {
                reader.ReadBytes(remainingBytes);
            }

            return optionalHeader;
        }

        /// <summary>
        /// 解析节头
        /// </summary>
        /// <param name="reader">二进制读取器</param>
        /// <param name="numberOfSections">节的数量</param>
        /// <returns>节头列表</returns>
        internal static List<IMAGESECTIONHEADER> ParseSectionHeaders(BinaryReader reader, ushort numberOfSections)
        {
            List<IMAGESECTIONHEADER> sections = [];

            for (int i = 0; i < numberOfSections; i++)
            {
                IMAGESECTIONHEADER section = new()
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