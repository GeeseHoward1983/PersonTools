using System;
using System.Runtime.InteropServices;

namespace PersonalTools.ELFAnalyzer.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ELFSymbol
    {
        public uint   st_name;    // Symbol name (index into string table)
        public ulong  st_value;   // Symbol value (address)
        public ulong  st_size;    // Symbol size
        public byte   st_info;    // Type and binding information
        public byte   st_other;   // Visibility
        public ushort st_shndx;   // Section index
    }
}