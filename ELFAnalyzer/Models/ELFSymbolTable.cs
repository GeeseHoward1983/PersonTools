using System;
using System.Runtime.InteropServices;

namespace PersonalTools.ELFAnalyzer.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ELFSymbol
    {
        public uint   st_name { get; set; }    // Symbol name (index into string table)
        public ulong  st_value { get; set; }   // Symbol value (address)
        public ulong  st_size { get; set; }    // Symbol size
        public byte   st_info { get; set; }    // Type and binding information
        public byte   st_other { get; set; }   // Visibility
        public ushort st_shndx { get; set; }   // Section index
    }
}