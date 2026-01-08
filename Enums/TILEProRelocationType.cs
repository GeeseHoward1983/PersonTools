using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// TILEPro Relocation Types
    /// </summary>
    [Flags]
    public enum TILEProRelocationType : uint
    {
        R_TILEPRO_NONE = 0,                    /* No reloc */
        R_TILEPRO_32 = 1,                      /* Direct 32 bit */
        R_TILEPRO_16 = 2,                      /* Direct 16 bit */
        R_TILEPRO_8 = 3,                       /* Direct 8 bit */
        R_TILEPRO_32_PCREL = 4,                /* PC relative 32 bit */
        R_TILEPRO_16_PCREL = 5,                /* PC relative 16 bit */
        R_TILEPRO_8_PCREL = 6,                 /* PC relative 8 bit */
        R_TILEPRO_LO16 = 7,                    /* Low 16 bit */
        R_TILEPRO_HI16 = 8,                    /* High 16 bit */
        R_TILEPRO_HA16 = 9,                    /* High 16 bit, adjusted */
        R_TILEPRO_COPY = 10,                   /* Copy relocation */
        R_TILEPRO_GLOB_DAT = 11,               /* Create GOT entry */
        R_TILEPRO_JMP_SLOT = 12,               /* Create PLT entry */
        R_TILEPRO_RELATIVE = 13,               /* Adjust by program base */
        R_TILEPRO_BROFF_X1 = 14,               /* X1 pipe branch offset */
        R_TILEPRO_JOFFLONG_X1 = 15,            /* X1 pipe jump offset */
        R_TILEPRO_JOFFLONG_X1_PLT = 16,        /* X1 pipe jump offset to PLT */
        R_TILEPRO_IMM8_X0 = 17,                /* X0 pipe 8-bit */
        R_TILEPRO_IMM8_Y0 = 18,                /* Y0 pipe 8-bit */
        R_TILEPRO_IMM8_X1 = 19,                /* X1 pipe 8-bit */
        R_TILEPRO_IMM8_Y1 = 20,                /* Y1 pipe 8-bit */
        R_TILEPRO_MT_IMM15_X1 = 21,            /* X1 pipe mtspr */
        R_TILEPRO_MF_IMM15_X1 = 22,            /* X1 pipe mfspr */
        R_TILEPRO_IMM16_X0 = 23,               /* X0 pipe 16-bit */
        R_TILEPRO_IMM16_X1 = 24,               /* X1 pipe 16-bit */
        R_TILEPRO_IMM16_X0_LO = 25,            /* X0 pipe low 16-bit */
        R_TILEPRO_IMM16_X1_LO = 26,            /* X1 pipe low 16-bit */
        R_TILEPRO_IMM16_X0_HI = 27,            /* X0 pipe high 16-bit */
        R_TILEPRO_IMM16_X1_HI = 28,            /* X1 pipe high 16-bit */
        R_TILEPRO_IMM16_X0_HA = 29,            /* X0 pipe high 16-bit, adjusted */
        R_TILEPRO_IMM16_X1_HA = 30,            /* X1 pipe high 16-bit, adjusted */
        R_TILEPRO_IMM16_X0_PCREL = 31,         /* X0 pipe PC relative 16 bit */
        R_TILEPRO_IMM16_X1_PCREL = 32,         /* X1 pipe PC relative 16 bit */
        R_TILEPRO_IMM16_X0_LO_PCREL = 33,      /* X0 pipe PC relative low 16 bit */
        R_TILEPRO_IMM16_X1_LO_PCREL = 34,      /* X1 pipe PC relative low 16 bit */
        R_TILEPRO_IMM16_X0_HI_PCREL = 35,      /* X0 pipe PC relative high 16 bit */
        R_TILEPRO_IMM16_X1_HI_PCREL = 36,      /* X1 pipe PC relative high 16 bit */
        R_TILEPRO_IMM16_X0_HA_PCREL = 37,      /* X0 pipe PC relative ha() 16 bit */
        R_TILEPRO_IMM16_X1_HA_PCREL = 38,      /* X1 pipe PC relative ha() 16 bit */
        R_TILEPRO_IMM16_X0_GOT = 39,           /* X0 pipe 16-bit GOT offset */
        R_TILEPRO_IMM16_X1_GOT = 40,           /* X1 pipe 16-bit GOT offset */
        R_TILEPRO_IMM16_X0_GOT_LO = 41,        /* X0 pipe low 16-bit GOT offset */
        R_TILEPRO_IMM16_X1_GOT_LO = 42,        /* X1 pipe low 16-bit GOT offset */
        R_TILEPRO_IMM16_X0_GOT_HI = 43,        /* X0 pipe high 16-bit GOT offset */
        R_TILEPRO_IMM16_X1_GOT_HI = 44,        /* X1 pipe high 16-bit GOT offset */
        R_TILEPRO_IMM16_X0_GOT_HA = 45,        /* X0 pipe ha() 16-bit GOT offset */
        R_TILEPRO_IMM16_X1_GOT_HA = 46,        /* X1 pipe ha() 16-bit GOT offset */
        R_TILEPRO_MMSTART_X0 = 47,             /* X0 pipe mm "start" */
        R_TILEPRO_MMEND_X0 = 48,               /* X0 pipe mm "end" */
        R_TILEPRO_MMSTART_X1 = 49,             /* X1 pipe mm "start" */
        R_TILEPRO_MMEND_X1 = 50,               /* X1 pipe mm "end" */
        R_TILEPRO_SHAMT_X0 = 51,               /* X0 pipe shift amount */
        R_TILEPRO_SHAMT_X1 = 52,               /* X1 pipe shift amount */
        R_TILEPRO_SHAMT_Y0 = 53,               /* Y0 pipe shift amount */
        R_TILEPRO_SHAMT_Y1 = 54,               /* Y1 pipe shift amount */
        R_TILEPRO_DEST_IMM8_X1 = 55,           /* X1 pipe destination 8-bit */
        R_TILEPRO_TLS_GD_CALL = 60,            /* "jal" for TLS GD */
        R_TILEPRO_IMM8_X0_TLS_GD_ADD = 61,     /* X0 pipe "addi" for TLS GD */
        R_TILEPRO_IMM8_X1_TLS_GD_ADD = 62,     /* X1 pipe "addi" for TLS GD */
        R_TILEPRO_IMM8_Y0_TLS_GD_ADD = 63,     /* Y0 pipe "addi" for TLS GD */
        R_TILEPRO_IMM8_Y1_TLS_GD_ADD = 64,     /* Y1 pipe "addi" for TLS GD */
        R_TILEPRO_TLS_IE_LOAD = 65,            /* "lw_tls" for TLS IE */
        R_TILEPRO_IMM16_X0_TLS_GD = 66,        /* X0 pipe 16-bit TLS GD offset */
        R_TILEPRO_IMM16_X1_TLS_GD = 67,        /* X1 pipe 16-bit TLS GD offset */
        R_TILEPRO_IMM16_X0_TLS_GD_LO = 68,     /* X0 pipe low 16-bit TLS GD offset */
        R_TILEPRO_IMM16_X1_TLS_GD_LO = 69,     /* X1 pipe low 16-bit TLS GD offset */
        R_TILEPRO_IMM16_X0_TLS_GD_HI = 70,     /* X0 pipe high 16-bit TLS GD offset */
        R_TILEPRO_IMM16_X1_TLS_GD_HI = 71,     /* X1 pipe high 16-bit TLS GD offset */
        R_TILEPRO_IMM16_X0_TLS_GD_HA = 72,     /* X0 pipe ha() 16-bit TLS GD offset */
        R_TILEPRO_IMM16_X1_TLS_GD_HA = 73,     /* X1 pipe ha() 16-bit TLS GD offset */
        R_TILEPRO_IMM16_X0_TLS_IE = 74,        /* X0 pipe 16-bit TLS IE offset */
        R_TILEPRO_IMM16_X1_TLS_IE = 75,        /* X1 pipe 16-bit TLS IE offset */
        R_TILEPRO_IMM16_X0_TLS_IE_LO = 76,     /* X0 pipe low 16-bit TLS IE offset */
        R_TILEPRO_IMM16_X1_TLS_IE_LO = 77,     /* X1 pipe low 16-bit TLS IE offset */
        R_TILEPRO_IMM16_X0_TLS_IE_HI = 78,     /* X0 pipe high 16-bit TLS IE offset */
        R_TILEPRO_IMM16_X1_TLS_IE_HI = 79,     /* X1 pipe high 16-bit TLS IE offset */
        R_TILEPRO_IMM16_X0_TLS_IE_HA = 80,     /* X0 pipe ha() 16-bit TLS IE offset */
        R_TILEPRO_IMM16_X1_TLS_IE_HA = 81,     /* X1 pipe ha() 16-bit TLS IE offset */
        R_TILEPRO_TLS_DTPMOD32 = 82,           /* ID of module containing symbol */
        R_TILEPRO_TLS_DTPOFF32 = 83,           /* Offset in TLS block */
        R_TILEPRO_TLS_TPOFF32 = 84,            /* Offset in static TLS block */
        R_TILEPRO_IMM16_X0_TLS_LE = 85,        /* X0 pipe 16-bit TLS LE offset */
        R_TILEPRO_IMM16_X1_TLS_LE = 86,        /* X1 pipe 16-bit TLS LE offset */
        R_TILEPRO_IMM16_X0_TLS_LE_LO = 87,     /* X0 pipe low 16-bit TLS LE offset */
        R_TILEPRO_IMM16_X1_TLS_LE_LO = 88,     /* X1 pipe low 16-bit TLS LE offset */
        R_TILEPRO_IMM16_X0_TLS_LE_HI = 89,     /* X0 pipe high 16-bit TLS LE offset */
        R_TILEPRO_IMM16_X1_TLS_LE_HI = 90,     /* X1 pipe high 16-bit TLS LE offset */
        R_TILEPRO_IMM16_X0_TLS_LE_HA = 91,     /* X0 pipe ha() 16-bit TLS LE offset */
        R_TILEPRO_IMM16_X1_TLS_LE_HA = 92,     /* X1 pipe ha() 16-bit TLS LE offset */
        R_TILEPRO_GNU_VTINHERIT = 128,         /* GNU C++ vtable hierarchy */
        R_TILEPRO_GNU_VTENTRY = 129             /* GNU C++ vtable member usage */
    }
}