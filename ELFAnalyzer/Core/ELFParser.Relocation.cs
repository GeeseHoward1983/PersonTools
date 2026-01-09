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
                (ushort)EMachine.EM_X86_64 => GetX86_64RelocationTypeName(type),
                (ushort)EMachine.EM_386 => GetX86RelocationTypeName(type),
                (ushort)EMachine.EM_ARM => GetArmRelocationTypeName(type),
                (ushort)EMachine.EM_AARCH64 => GetAArch64RelocationTypeName(type),
                (ushort)EMachine.EM_MIPS => GetMipsRelocationTypeName(type),
                (ushort)EMachine.EM_MIPS_RS3_LE => GetMipsRelocationTypeName(type), // MIPS RS3000 Little-endian
                (ushort)EMachine.EM_LOONGARCH => GetLoongArchRelocationTypeName(type),
                
                // 新增其他架构支持
                (ushort)EMachine.EM_68K => GetM68kRelocationTypeName(type),
                (ushort)EMachine.EM_SPARC => GetSparcRelocationTypeName(type),
                (ushort)EMachine.EM_SPARCV9 => GetSparcRelocationTypeName(type), // SPARC V9 64-bit
                (ushort)EMachine.EM_PARISC => GetHPPARelocationTypeName(type),
                (ushort)EMachine.EM_ALPHA => GetAlphaRelocationTypeName(type),
                (ushort)EMachine.EM_PPC => GetPowerPCRelocationTypeName(type),
                (ushort)EMachine.EM_PPC64 => GetPowerPC64RelocationTypeName(type),
                (ushort)EMachine.EM_IA_64 => GetIA64RelocationTypeName(type),
                (ushort)EMachine.EM_S390 => GetS390RelocationTypeName(type),
                (ushort)EMachine.EM_CRIS => GetCRISRelocationTypeName(type),
                (ushort)EMachine.EM_MN10300 => GetAM33RelocationTypeName(type), // AM33
                (ushort)EMachine.EM_M32R => GetM32RRelocationTypeName(type),
                (ushort)EMachine.EM_MICROBLAZE => GetMicroBlazeRelocationTypeName(type),
                (ushort)EMachine.EM_ALTERA_NIOS2 => GetNios2RelocationTypeName(type),
                (ushort)EMachine.EM_TILEPRO => GetTILEProRelocationTypeName(type),
                (ushort)EMachine.EM_TILEGX => GetTILEGxRelocationTypeName(type),
                
                _ => $"R_UNKNOWN({type})",
            };
        }

        private static string GetX86_64RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(X86_64RelocationType), type))
            {
                return Enum.GetName(typeof(X86_64RelocationType), type) ?? $"R_X86_64_UNKNOWN({type})";
            }
            return $"R_X86_64_UNKNOWN({type})";
        }

        private static string GetX86RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(I386RelocationType), type))
            {
                return Enum.GetName(typeof(I386RelocationType), type) ?? $"R_386_UNKNOWN({type})";
            }
            return $"R_386_UNKNOWN({type})";
        }

        private static string GetArmRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(ARMRelocationType), type))
            {
                return Enum.GetName(typeof(ARMRelocationType), type) ?? $"R_ARM_UNKNOWN({type})";
            }
            return $"R_ARM_UNKNOWN({type})";
        }

        private static string GetAArch64RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(AArch64RelocationType), type))
            {
                return Enum.GetName(typeof(AArch64RelocationType), type) ?? $"R_AARCH64_UNKNOWN({type})";
            }
            return $"R_AARCH64_UNKNOWN({type})";
        }

        private static string GetMipsRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(MipsRelocationType), type))
            {
                return Enum.GetName(typeof(MipsRelocationType), type) ?? $"R_MIPS_UNKNOWN({type})";
            }
            return $"R_MIPS_UNKNOWN({type})";
        }

        private static string GetLoongArchRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(LoongArchRelocationType), type))
            {
                return Enum.GetName(typeof(LoongArchRelocationType), type) ?? $"R_LARCH_UNKNOWN({type})";
            }
            return $"R_LARCH_UNKNOWN({type})";
        }
        
        // 新增各种架构的重定位类型名称获取方法
        private static string GetM68kRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(M68kRelocationType), type))
            {
                return Enum.GetName(typeof(M68kRelocationType), type) ?? $"R_68K_UNKNOWN({type})";
            }
            return $"R_68K_UNKNOWN({type})";
        }
        
        private static string GetSparcRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(SPARCRelocationType), type))
            {
                return Enum.GetName(typeof(SPARCRelocationType), type) ?? $"R_SPARC_UNKNOWN({type})";
            }
            return $"R_SPARC_UNKNOWN({type})";
        }
        
        private static string GetHPPARelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(HPPARelocationType), type))
            {
                return Enum.GetName(typeof(HPPARelocationType), type) ?? $"R_PARISC_UNKNOWN({type})";
            }
            return $"R_PARISC_UNKNOWN({type})";
        }
        
        private static string GetAlphaRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(AlphaRelocationType), type))
            {
                return Enum.GetName(typeof(AlphaRelocationType), type) ?? $"R_ALPHA_UNKNOWN({type})";
            }
            return $"R_ALPHA_UNKNOWN({type})";
        }
        
        private static string GetPowerPCRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(PowerPCRelocationType), type))
            {
                return Enum.GetName(typeof(PowerPCRelocationType), type) ?? $"R_PPC_UNKNOWN({type})";
            }
            return $"R_PPC_UNKNOWN({type})";
        }
        
        private static string GetPowerPC64RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(PowerPC64RelocationType), type))
            {
                return Enum.GetName(typeof(PowerPC64RelocationType), type) ?? $"R_PPC64_UNKNOWN({type})";
            }
            return $"R_PPC64_UNKNOWN({type})";
        }
        
        private static string GetIA64RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(IA64RelocationType), type))
            {
                return Enum.GetName(typeof(IA64RelocationType), type) ?? $"R_IA64_UNKNOWN({type})";
            }
            return $"R_IA64_UNKNOWN({type})";
        }
        
        private static string GetS390RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(S390RelocationType), type))
            {
                return Enum.GetName(typeof(S390RelocationType), type) ?? $"R_390_UNKNOWN({type})";
            }
            return $"R_390_UNKNOWN({type})";
        }
        
        private static string GetCRISRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(CRISRelocationType), type))
            {
                return Enum.GetName(typeof(CRISRelocationType), type) ?? $"R_CRIS_UNKNOWN({type})";
            }
            return $"R_CRIS_UNKNOWN({type})";
        }
        
        private static string GetAM33RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(AM33RelocationType), type))
            {
                return Enum.GetName(typeof(AM33RelocationType), type) ?? $"R_MN10300_UNKNOWN({type})";
            }
            return $"R_MN10300_UNKNOWN({type})";
        }
        
        private static string GetM32RRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(M32RRelocationType), type))
            {
                return Enum.GetName(typeof(M32RRelocationType), type) ?? $"R_M32R_UNKNOWN({type})";
            }
            return $"R_M32R_UNKNOWN({type})";
        }
        
        private static string GetMicroBlazeRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(MicroBlazeRelocationType), type))
            {
                return Enum.GetName(typeof(MicroBlazeRelocationType), type) ?? $"R_MICROBLAZE_UNKNOWN({type})";
            }
            return $"R_MICROBLAZE_UNKNOWN({type})";
        }
        
        private static string GetNios2RelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(Nios2RelocationType), type))
            {
                return Enum.GetName(typeof(Nios2RelocationType), type) ?? $"R_NIOS2_UNKNOWN({type})";
            }
            return $"R_NIOS2_UNKNOWN({type})";
        }
        
        private static string GetTILEProRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(TILEProRelocationType), type))
            {
                return Enum.GetName(typeof(TILEProRelocationType), type) ?? $"R_TILEPRO_UNKNOWN({type})";
            }
            return $"R_TILEPRO_UNKNOWN({type})";
        }
        
        private static string GetTILEGxRelocationTypeName(uint type)
        {
            if (Enum.IsDefined(typeof(TILEGxRelocationType), type))
            {
                return Enum.GetName(typeof(TILEGxRelocationType), type) ?? $"R_TILEGX_UNKNOWN({type})";
            }
            return $"R_TILEGX_UNKNOWN({type})";
        }
    }
}