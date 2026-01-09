using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    public static class ELFRelocation
    {
        public static string GetRelocationTypeName(uint type, ushort machine)
        {
            // 根据机器类型返回不同的重定位类型名称
            return machine switch
            {
                (ushort)EMachine.EM_X86_64 => ELFParserUtils.GetTypeName(typeof(X86_64RelocationType), type, "R_X86_64_"),
                (ushort)EMachine.EM_386 => ELFParserUtils.GetTypeName(typeof(I386RelocationType), type, "R_386_"),
                (ushort)EMachine.EM_ARM => ELFParserUtils.GetTypeName(typeof(ARMRelocationType), type, "R_ARM_"),
                (ushort)EMachine.EM_AARCH64 => ELFParserUtils.GetTypeName(typeof(AArch64RelocationType), type, "R_AARCH64_"),
                (ushort)EMachine.EM_MIPS => ELFParserUtils.GetTypeName(typeof(MipsRelocationType), type, "R_MIPS_"),
                (ushort)EMachine.EM_MIPS_RS3_LE => ELFParserUtils.GetTypeName(typeof(MipsRelocationType), type, "R_MIPS_"), // MIPS RS3000 Little-endian
                (ushort)EMachine.EM_LOONGARCH => ELFParserUtils.GetTypeName(typeof(LoongArchRelocationType), type, "R_LARCH_"),
                
                // 新增其他架构支持
                (ushort)EMachine.EM_68K => ELFParserUtils.GetTypeName(typeof(M68kRelocationType), type, "R_68K_"),
                (ushort)EMachine.EM_SPARC => ELFParserUtils.GetTypeName(typeof(SPARCRelocationType), type, "R_SPARC_"),
                (ushort)EMachine.EM_SPARCV9 => ELFParserUtils.GetTypeName(typeof(SPARCRelocationType), type, "R_SPARC_"), // SPARC V9 64-bit
                (ushort)EMachine.EM_PARISC => ELFParserUtils.GetTypeName(typeof(HPPARelocationType), type, "R_PARISC_"),
                (ushort)EMachine.EM_ALPHA => ELFParserUtils.GetTypeName(typeof(AlphaRelocationType), type, "R_ALPHA_"),
                (ushort)EMachine.EM_PPC => ELFParserUtils.GetTypeName(typeof(PowerPCRelocationType), type, "R_PPC_"),
                (ushort)EMachine.EM_PPC64 => ELFParserUtils.GetTypeName(typeof(PowerPC64RelocationType), type, "R_PPC64_"),
                (ushort)EMachine.EM_IA_64 => ELFParserUtils.GetTypeName(typeof(IA64RelocationType), type, "R_IA64_"),
                (ushort)EMachine.EM_S390 => ELFParserUtils.GetTypeName(typeof(S390RelocationType), type, "R_390_"),
                (ushort)EMachine.EM_CRIS => ELFParserUtils.GetTypeName(typeof(CRISRelocationType), type, "R_CRIS_"),
                (ushort)EMachine.EM_MN10300 => ELFParserUtils.GetTypeName(typeof(AM33RelocationType), type, "R_MN10300_"), // AM33
                (ushort)EMachine.EM_M32R => ELFParserUtils.GetTypeName(typeof(M32RRelocationType), type, "R_M32R_"),
                (ushort)EMachine.EM_MICROBLAZE => ELFParserUtils.GetTypeName(typeof(MicroBlazeRelocationType), type, "R_MICROBLAZE_"),
                (ushort)EMachine.EM_ALTERA_NIOS2 => ELFParserUtils.GetTypeName(typeof(Nios2RelocationType), type, "R_NIOS2_"),
                (ushort)EMachine.EM_TILEPRO => ELFParserUtils.GetTypeName(typeof(TILEProRelocationType), type, "R_TILEPRO_"),
                (ushort)EMachine.EM_TILEGX => ELFParserUtils.GetTypeName(typeof(TILEGxRelocationType), type, "R_TILEGX_"),
                
                _ => $"R_UNKNOWN({type})",
            };
        }
    }
}