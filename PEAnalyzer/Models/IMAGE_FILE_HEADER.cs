using System.Runtime.InteropServices;

namespace PersonalTools.PEAnalyzer.Models
{
    // 文件头
    [StructLayout(LayoutKind.Sequential)]
    internal struct IMAGE_FILE_HEADER
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    }
}