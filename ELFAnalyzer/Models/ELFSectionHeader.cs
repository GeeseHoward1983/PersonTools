using System;
using System.Runtime.InteropServices;

namespace PersonalTools.ELFAnalyzer.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ELFSectionHeader
    {
        public uint sh_name;      // Section name (index into string table)
        public uint sh_type;      // Section type
        public ulong sh_flags;    // Section flags
        public ulong sh_addr;     // Section virtual address at execution
        public ulong sh_offset;   // Section file offset
        public ulong sh_size;     // Section size in bytes
        public uint sh_link;      // Link to other section
        public uint sh_info;      // Additional section information
        public ulong sh_addralign; // Section alignment
        public ulong sh_entsize;  // Entry size if section holds table
    }
}