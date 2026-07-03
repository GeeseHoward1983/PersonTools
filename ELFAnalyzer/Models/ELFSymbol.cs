namespace PersonalTools.ELFAnalyzer.Models
{
    // 不用于 Marshal（字段由 ReadSymbol32/64 逐字段填充，且 ELF32/ELF64 磁盘布局不同），
    // 故不加 [StructLayout(Pack=1)]（死属性且误导）。
    internal struct ELFSymbol
    {
        public uint StName { get; set; }    // Symbol name (index into string table)
        public ulong StValue { get; set; }   // Symbol value (address)
        public ulong StSize { get; set; }    // Symbol size
        public byte StInfo { get; set; }    // Type and binding information
        public byte StOther { get; set; }   // Visibility
        public ushort StShndx { get; set; }   // Section index
    }
}