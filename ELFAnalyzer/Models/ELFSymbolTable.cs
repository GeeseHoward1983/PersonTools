using System;
using System.Runtime.InteropServices;

namespace PersonalTools.ELFAnalyzer.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ELFSymbol
    {
        public uint StName { get; set; }    // Symbol name (index into string table)
        public ulong StValue { get; set; }   // Symbol value (address)
        public ulong StSize { get; set; }    // Symbol size
        public byte StInfo { get; set; }    // Type and binding information
        public byte StOther { get; set; }   // Visibility
        public ushort StShndx { get; set; }   // Section index
    }
}