using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// ARM架构的重定位类型枚举
    /// </summary>
    [Flags]
    internal enum ARMRelocationType : uint
    {
        R_ARM_NONE = 0,               /* No reloc */
        R_ARM_PC24 = 1,               /* Deprecated PC relative 26 bit branch. */
        R_ARM_ABS32 = 2,              /* Direct 32 bit */
        R_ARM_REL32 = 3,              /* PC relative 32 bit */
        R_ARM_LDR_PC_G0 = 4,
        R_ARM_ABS16 = 5,              /* Direct 16 bit */
        R_ARM_ABS12 = 6,              /* Direct 12 bit */
        R_ARM_THM_ABS5 = 7,           /* Direct & 0x7C (LDR, STR). */
        R_ARM_ABS8 = 8,               /* Direct 8 bit */
        R_ARM_SBREL32 = 9,
        R_ARM_THM_PC22 = 10,          /* PC relative 24 bit (Thumb32 BL). */
        R_ARM_THM_PC8 = 11,           /* PC relative & 0x3FC (Thumb16 LDR, ADD, ADR). */
        R_ARM_AMP_VCALL9 = 12,
        R_ARM_TLS_DESC = 13,             /* Obsolete static relocation. */
        R_ARM_THM_SWI8 = 14,          /* Reserved. */
        R_ARM_XPC25 = 15,             /* Reserved. */
        R_ARM_THM_XPC22 = 16,         /* Reserved. */
        R_ARM_TLS_DTPMOD32 = 17,      /* ID of module containing symbol */
        R_ARM_TLS_DTPOFF32 = 18,      /* Offset in TLS block */
        R_ARM_TLS_TPOFF32 = 19,       /* Offset in static TLS block */
        R_ARM_COPY = 20,              /* Copy symbol at runtime */
        R_ARM_GLOB_DAT = 21,          /* Create GOT entry */
        R_ARM_JUMP_SLOT = 22,         /* Create PLT entry */
        R_ARM_RELATIVE = 23,          /* Adjust by program base */
        R_ARM_GOTOFF = 24,            /* 32 bit offset to GOT */
        R_ARM_GOTPC = 25,             /* 32 bit PC relative offset to GOT */
        R_ARM_GOT32 = 26,             /* 32 bit GOT entry */
        R_ARM_PLT32 = 27,             /* Deprecated, 32 bit PLT address. */
        R_ARM_CALL = 28,              /* PC relative 24 bit (BL, BLX). */
        R_ARM_JUMP24 = 29,            /* PC relative 24 bit (B, BL<cond>). */
        R_ARM_THM_JUMP24 = 30,        /* PC relative 24 bit (Thumb32 B.W). */
        R_ARM_BASE_ABS = 31,          /* Adjust by program base. */
        R_ARM_ALU_PCREL_7_0 = 32,     /* Obsolete. */
        R_ARM_ALU_PCREL_15_8 = 33,    /* Obsolete. */
        R_ARM_ALU_PCREL_23_15 = 34,   /* Obsolete. */
        R_ARM_LDR_SBREL_11_0 = 35,    /* Deprecated, prog. base relative. */
        R_ARM_ALU_SBREL_19_12 = 36,   /* Deprecated, prog. base relative. */
        R_ARM_ALU_SBREL_27_20 = 37,   /* Deprecated, prog. base relative. */
        R_ARM_TARGET1 = 38,
        R_ARM_SBREL31 = 39,           /* Program base relative. */
        R_ARM_V4BX = 40,
        R_ARM_TARGET2 = 41,
        R_ARM_PREL31 = 42,            /* 32 bit PC relative. */
        R_ARM_MOVW_ABS_NC = 43,       /* Direct 16-bit (MOVW). */
        R_ARM_MOVT_ABS = 44,          /* Direct high 16-bit (MOVT). */
        R_ARM_MOVW_PREL_NC = 45,      /* PC relative 16-bit (MOVW). */
        R_ARM_MOVT_PREL = 46,         /* PC relative (MOVT). */
        R_ARM_THM_MOVW_ABS_NC = 47,   /* Direct 16 bit (Thumb32 MOVW). */
        R_ARM_THM_MOVT_ABS = 48,      /* Direct high 16 bit (Thumb32 MOVT). */
        R_ARM_THM_MOVW_PREL_NC = 49,  /* PC relative 16 bit (Thumb32 MOVW). */
        R_ARM_THM_MOVT_PREL = 50,     /* PC relative high 16 bit (Thumb32 MOVT). */
        R_ARM_THM_JUMP19 = 51,        /* PC relative 20 bit (Thumb32 B<cond>.W). */
        R_ARM_THM_JUMP6 = 52,         /* PC relative X & 0x7E (Thumb16 CBZ, CBNZ). */
        R_ARM_THM_ALU_PREL_11_0 = 53, /* PC relative 12 bit (Thumb32 ADR.W). */
        R_ARM_THM_PC12 = 54,          /* PC relative 12 bit (Thumb32 LDR{D,SB,H,SH}). */
        R_ARM_ABS32_NOI = 55,         /* Direct 32-bit. */
        R_ARM_REL32_NOI = 56,         /* PC relative 32-bit. */
        R_ARM_ALU_PC_G0_NC = 57,      /* PC relative (ADD, SUB). */
        R_ARM_ALU_PC_G0 = 58,         /* PC relative (ADD, SUB). */
        R_ARM_ALU_PC_G1_NC = 59,      /* PC relative (ADD, SUB). */
        R_ARM_ALU_PC_G1 = 60,         /* PC relative (ADD, SUB). */
        R_ARM_ALU_PC_G2 = 61,         /* PC relative (ADD, SUB). */
        R_ARM_LDR_PC_G1 = 62,         /* PC relative (LDR,STR,LDRB,STRB). */
        R_ARM_LDR_PC_G2 = 63,         /* PC relative (LDR,STR,LDRB,STRB). */
        R_ARM_LDRS_PC_G0 = 64,        /* PC relative (STR{D,H}, LDR{D,SB,H,SH}). */
        R_ARM_LDRS_PC_G1 = 65,        /* PC relative (STR{D,H}, LDR{D,SB,H,SH}). */
        R_ARM_LDRS_PC_G2 = 66,        /* PC relative (STR{D,H}, LDR{D,SB,H,SH}). */
        R_ARM_LDC_PC_G0 = 67,         /* PC relative (LDC, STC). */
        R_ARM_LDC_PC_G1 = 68,         /* PC relative (LDC, STC). */
        R_ARM_LDC_PC_G2 = 69,         /* PC relative (LDC, STC). */
        R_ARM_ALU_SB_G0_NC = 70,      /* Program base relative (ADD,SUB). */
        R_ARM_ALU_SB_G0 = 71,         /* Program base relative (ADD,SUB). */
        R_ARM_ALU_SB_G1_NC = 72,      /* Program base relative (ADD,SUB). */
        R_ARM_ALU_SB_G1 = 73,         /* Program base relative (ADD,SUB). */
        R_ARM_ALU_SB_G2 = 74,         /* Program base relative (ADD,SUB). */
        R_ARM_LDR_SB_G0 = 75,         /* Program base relative (LDR, STR, LDRB, STRB). */
        R_ARM_LDR_SB_G1 = 76,         /* Program base relative (LDR, STR, LDRB, STRB). */
        R_ARM_LDR_SB_G2 = 77,         /* Program base relative (LDR, STR, LDRB, STRB). */
        R_ARM_LDRS_SB_G0 = 78,        /* Program base relative (LDR, STR, LDRB, STRB). */
        R_ARM_LDRS_SB_G1 = 79,        /* Program base relative (LDR, STR, LDRB, STRB). */
        R_ARM_LDRS_SB_G2 = 80,        /* Program base relative (LDR, STR, LDRB, STRB). */
        R_ARM_LDC_SB_G0 = 81,         /* Program base relative (LDC,STC). */
        R_ARM_LDC_SB_G1 = 82,         /* Program base relative (LDC,STC). */
        R_ARM_LDC_SB_G2 = 83,         /* Program base relative (LDC,STC). */
        R_ARM_MOVW_BREL_NC = 84,      /* Program base relative 16 bit (MOVW). */
        R_ARM_MOVT_BREL = 85,         /* Program base relative high 16 bit (MOVT). */
        R_ARM_MOVW_BREL = 86,         /* Program base relative 16 bit (MOVW). */
        R_ARM_THM_MOVW_BREL_NC = 87,  /* Program base relative 16 bit (Thumb32 MOVW). */
        R_ARM_THM_MOVT_BREL = 88,     /* Program base relative high 16 bit (Thumb32 MOVT). */
        R_ARM_THM_MOVW_BREL = 89,     /* Program base relative 16 bit (Thumb32 MOVW). */
        R_ARM_TLS_GOTDESC = 90,
        R_ARM_TLS_CALL = 91,
        R_ARM_TLS_DESCSEQ = 92,       /* TLS relaxation. */
        R_ARM_THM_TLS_CALL = 93,
        R_ARM_PLT32_ABS = 94,
        R_ARM_GOT_ABS = 95,           /* GOT entry. */
        R_ARM_GOT_PREL = 96,          /* PC relative GOT entry. */
        R_ARM_GOT_BREL12 = 97,        /* GOT entry relative to GOT origin (LDR). */
        R_ARM_GOTOFF12 = 98,          /* 12 bit, GOT entry relative to GOT origin (LDR, STR). */
        R_ARM_GOTRELAX = 99,
        R_ARM_GNU_VTENTRY = 100,
        R_ARM_GNU_VTINHERIT = 101,
        R_ARM_THM_PC11 = 102,         /* PC relative & 0xFFE (Thumb16 B). */
        R_ARM_THM_PC9 = 103,          /* PC relative & 0x1FE (Thumb16 B/B<cond>). */
        R_ARM_TLS_GD32 = 104,         /* PC-rel 32 bit for global dynamic thread local data */
        R_ARM_TLS_LDM32 = 105,        /* PC-rel 32 bit for local dynamic thread local data */
        R_ARM_TLS_LDO32 = 106,        /* 32 bit offset relative to TLS block */
        R_ARM_TLS_IE32 = 107,         /* PC-rel 32 bit for GOT entry of static TLS block offset */
        R_ARM_TLS_LE32 = 108,         /* 32 bit offset relative to static TLS block */
        R_ARM_TLS_LDO12 = 109,        /* 12 bit relative to TLS block (LDR, STR). */
        R_ARM_TLS_LE12 = 110,         /* 12 bit relative to static TLS block (LDR, STR). */
        R_ARM_TLS_IE12GP = 111,       /* 12 bit GOT entry relative to GOT origin (LDR). */
        R_ARM_PRIVATE_0,
        R_ARM_PRIVATE_1,
        R_ARM_PRIVATE_2,
        R_ARM_PRIVATE_3,
        R_ARM_PRIVATE_4,
        R_ARM_PRIVATE_5,
        R_ARM_PRIVATE_6,
        R_ARM_PRIVATE_7,
        R_ARM_PRIVATE_8,
        R_ARM_PRIVATE_9,
        R_ARM_PRIVATE_10,
        R_ARM_PRIVATE_11,
        R_ARM_PRIVATE_12,
        R_ARM_PRIVATE_13,
        R_ARM_PRIVATE_14,
        R_ARM_PRIVATE_15,
        R_ARM_ME_TOO = 128,           /* Obsolete. */
        R_ARM_THM_TLS_DESCSEQ16 = 129,
        R_ARM_THM_TLS_DESCSEQ32 = 130,
        R_ARM_THM_GOT_BREL12 = 131,   /* GOT entry relative to GOT origin, 12 bit (Thumb32 LDR). */
        R_ARM_THM_ALU_ABS_G0_NC,
        R_ARM_THM_ALU_ABS_G1_NC,
        R_ARM_THM_ALU_ABS_G2_NC,
        R_ARM_THM_ALU_ABS_G3,
        R_ARM_THM_BF16,
        R_ARM_THM_BF12,
        R_ARM_THM_BF18,
        R_ARM_IRELATIVE = 160,
        R_ARM_PRIVATE_16,
        R_ARM_PRIVATE_17,
        R_ARM_PRIVATE_18,
        R_ARM_PRIVATE_19,
        R_ARM_PRIVATE_20,
        R_ARM_PRIVATE_21,
        R_ARM_PRIVATE_22,
        R_ARM_PRIVATE_23,
        R_ARM_PRIVATE_24,
        R_ARM_PRIVATE_25,
        R_ARM_PRIVATE_26,
        R_ARM_PRIVATE_27,
        R_ARM_PRIVATE_28,
        R_ARM_PRIVATE_29,
        R_ARM_PRIVATE_30,
        R_ARM_PRIVATE_31,
        R_ARM_RXPC25 = 249,
        R_ARM_RSBREL32 = 250,
        R_ARM_THM_RPC22 = 251,
        R_ARM_RREL32 = 252,
        R_ARM_RABS22 = 253,
        R_ARM_RPC24 = 254,
        R_ARM_RBASE = 255
    }
}