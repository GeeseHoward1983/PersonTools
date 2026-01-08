using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// HPPA Relocation Types
    /// </summary>
    [Flags]
    public enum HPPARelocationType : uint
    {
        R_PARISC_NONE = 0,        /* No reloc. */
        R_PARISC_DIR32 = 1,       /* Direct 32-bit reference. */
        R_PARISC_DIR21L = 2,      /* Left 21 bits of eff. address. */
        R_PARISC_DIR17R = 3,      /* Right 17 bits of eff. address. */
        R_PARISC_DIR17F = 4,      /* 17 bits of eff. address. */
        R_PARISC_DIR14R = 6,      /* Right 14 bits of eff. address. */
        R_PARISC_PCREL32 = 9,     /* 32-bit rel. address. */
        R_PARISC_PCREL21L = 10,   /* Left 21 bits of rel. address. */
        R_PARISC_PCREL17R = 11,   /* Right 17 bits of rel. address. */
        R_PARISC_PCREL17F = 12,   /* 17 bits of rel. address. */
        R_PARISC_PCREL14R = 14,   /* Right 14 bits of rel. address. */
        R_PARISC_DPREL21L = 18,   /* Left 21 bits of rel. address. */
        R_PARISC_DPREL14R = 22,   /* Right 14 bits of rel. address. */
        R_PARISC_GPREL21L = 26,   /* GP-relative, left 21 bits. */
        R_PARISC_GPREL14R = 30,   /* GP-relative, right 14 bits. */
        R_PARISC_LTOFF21L = 34,   /* LT-relative, left 21 bits. */
        R_PARISC_LTOFF14R = 38,   /* LT-relative, right 14 bits. */
        R_PARISC_SECREL32 = 41,   /* 32 bits section rel. address. */
        R_PARISC_SEGBASE = 48,    /* No relocation, set segment base. */
        R_PARISC_SEGREL32 = 49,   /* 32 bits segment rel. address. */
        R_PARISC_PLTOFF21L = 50,  /* PLT rel. address, left 21 bits. */
        R_PARISC_PLTOFF14R = 54,  /* PLT rel. address, right 14 bits. */
        R_PARISC_LTOFF_FPTR32 = 57, /* 32 bits LT-rel. function pointer. */
        R_PARISC_LTOFF_FPTR21L = 58, /* LT-rel. fct ptr, left 21 bits. */
        R_PARISC_LTOFF_FPTR14R = 62, /* LT-rel. fct ptr, right 14 bits. */
        R_PARISC_FPTR64 = 64,     /* 64 bits function address. */
        R_PARISC_PLABEL32 = 65,   /* 32 bits function address. */
        R_PARISC_PLABEL21L = 66,  /* Left 21 bits of fdesc address. */
        R_PARISC_PLABEL14R = 70,  /* Right 14 bits of fdesc address. */
        R_PARISC_PCREL64 = 72,    /* 64 bits PC-rel. address. */
        R_PARISC_PCREL22F = 74,   /* 22 bits PC-rel. address. */
        R_PARISC_PCREL14WR = 75,  /* PC-rel. address, right 14 bits. */
        R_PARISC_PCREL14DR = 76,  /* PC rel. address, right 14 bits. */
        R_PARISC_PCREL16F = 77,   /* 16 bits PC-rel. address. */
        R_PARISC_PCREL16WF = 78,  /* 16 bits PC-rel. address. */
        R_PARISC_PCREL16DF = 79,  /* 16 bits PC-rel. address. */
        R_PARISC_DIR64 = 80,      /* 64 bits of eff. address. */
        R_PARISC_DIR14WR = 83,    /* 14 bits of eff. address. */
        R_PARISC_DIR14DR = 84,    /* 14 bits of eff. address. */
        R_PARISC_DIR16F = 85,     /* 16 bits of eff. address. */
        R_PARISC_DIR16WF = 86,    /* 16 bits of eff. address. */
        R_PARISC_DIR16DF = 87,    /* 16 bits of eff. address. */
        R_PARISC_GPREL64 = 88,    /* 64 bits of GP-rel. address. */
        R_PARISC_GPREL14WR = 91,  /* GP-rel. address, right 14 bits. */
        R_PARISC_GPREL14DR = 92,  /* GP-rel. address, right 14 bits. */
        R_PARISC_GPREL16F = 93,   /* 16 bits GP-rel. address. */
        R_PARISC_GPREL16WF = 94,  /* 16 bits GP-rel. address. */
        R_PARISC_GPREL16DF = 95,  /* 16 bits GP-rel. address. */
        R_PARISC_LTOFF64 = 96,    /* 64 bits LT-rel. address. */
        R_PARISC_LTOFF14WR = 99,  /* LT-rel. address, right 14 bits. */
        R_PARISC_LTOFF14DR = 100, /* LT-rel. address, right 14 bits. */
        R_PARISC_LTOFF16F = 101,  /* 16 bits LT-rel. address. */
        R_PARISC_LTOFF16WF = 102, /* 16 bits LT-rel. address. */
        R_PARISC_LTOFF16DF = 103, /* 16 bits LT-rel. address. */
        R_PARISC_SECREL64 = 104,  /* 64 bits section rel. address. */
        R_PARISC_SEGREL64 = 112,  /* 64 bits segment rel. address. */
        R_PARISC_PLTOFF14WR = 115, /* PLT-rel. address, right 14 bits. */
        R_PARISC_PLTOFF14DR = 116, /* PLT-rel. address, right 14 bits. */
        R_PARISC_PLTOFF16F = 117, /* 16 bits LT-rel. address. */
        R_PARISC_PLTOFF16WF = 118, /* 16 bits PLT-rel. address. */
        R_PARISC_PLTOFF16DF = 119, /* 16 bits PLT-rel. address. */
        R_PARISC_LTOFF_FPTR64 = 120, /* 64 bits LT-rel. function ptr. */
        R_PARISC_LTOFF_FPTR14WR = 123, /* LT-rel. fct. ptr., right 14 bits. */
        R_PARISC_LTOFF_FPTR14DR = 124, /* LT-rel. fct. ptr., right 14 bits. */
        R_PARISC_LTOFF_FPTR16F = 125, /* 16 bits LT-rel. function ptr. */
        R_PARISC_LTOFF_FPTR16WF = 126, /* 16 bits LT-rel. function ptr. */
        R_PARISC_LTOFF_FPTR16DF = 127, /* 16 bits LT-rel. function ptr. */
        R_PARISC_LORESERVE = 128,
        R_PARISC_COPY = R_PARISC_LORESERVE,      /* Copy relocation. */
        R_PARISC_IPLT = 129,      /* Dynamic reloc, imported PLT */
        R_PARISC_EPLT = 130,      /* Dynamic reloc, exported PLT */
        R_PARISC_TPREL32 = 153,   /* 32 bits TP-rel. address. */
        R_PARISC_TPREL21L = 154,  /* TP-rel. address, left 21 bits. */
        R_PARISC_TPREL14R = 158,  /* TP-rel. address, right 14 bits. */
        R_PARISC_LTOFF_TP21L = 162, /* LT-TP-rel. address, left 21 bits. */
        R_PARISC_LTOFF_TP14R = 166, /* LT-TP-rel. address, right 14 bits.*/
        R_PARISC_LTOFF_TP14F = 167, /* 14 bits LT-TP-rel. address. */
        R_PARISC_TPREL64 = 216,   /* 64 bits TP-rel. address. */
        R_PARISC_TPREL14WR = 219, /* TP-rel. address, right 14 bits. */
        R_PARISC_TPREL14DR = 220, /* TP-rel. address, right 14 bits. */
        R_PARISC_TPREL16F = 221,  /* 16 bits TP-rel. address. */
        R_PARISC_TPREL16WF = 222, /* 16 bits TP-rel. address. */
        R_PARISC_TPREL16DF = 223, /* 16 bits TP-rel. address. */
        R_PARISC_LTOFF_TP64 = 224, /* 64 bits LT-TP-rel. address. */
        R_PARISC_LTOFF_TP14WR = 227, /* LT-TP-rel. address, right 14 bits.*/
        R_PARISC_LTOFF_TP14DR = 228, /* LT-TP-rel. address, right 14 bits.*/
        R_PARISC_LTOFF_TP16F = 229, /* 16 bits LT-TP-rel. address. */
        R_PARISC_LTOFF_TP16WF = 230, /* 16 bits LT-TP-rel. address. */
        R_PARISC_LTOFF_TP16DF = 231, /* 16 bits LT-TP-rel. address. */
        R_PARISC_GNU_VTENTRY = 232,
        R_PARISC_GNU_VTINHERIT = 233,
        R_PARISC_TLS_GD21L = 234, /* GD 21-bit left. */
        R_PARISC_TLS_GD14R = 235, /* GD 14-bit right. */
        R_PARISC_TLS_GDCALL = 236, /* GD call to __t_g_a. */
        R_PARISC_TLS_LDM21L = 237, /* LD module 21-bit left. */
        R_PARISC_TLS_LDM14R = 238, /* LD module 14-bit right. */
        R_PARISC_TLS_LDMCALL = 239, /* LD module call to __t_g_a. */
        R_PARISC_TLS_LDO21L = 240, /* LD offset 21-bit left. */
        R_PARISC_TLS_LDO14R = 241, /* LD offset 14-bit right. */
        R_PARISC_TLS_DTPMOD32 = 242, /* DTP module 32-bit. */
        R_PARISC_TLS_DTPMOD64 = 243, /* DTP module 64-bit. */
        R_PARISC_TLS_DTPOFF32 = 244, /* DTP offset 32-bit. */
        R_PARISC_TLS_DTPOFF64 = 245, /* DTP offset 32-bit. */
        R_PARISC_TLS_LE21L = R_PARISC_TPREL21L,
        R_PARISC_TLS_LE14R = R_PARISC_TPREL14R,
        R_PARISC_TLS_IE21L = R_PARISC_LTOFF_TP21L,
        R_PARISC_TLS_IE14R = R_PARISC_LTOFF_TP14R,
        R_PARISC_TLS_TPREL32 = R_PARISC_TPREL32,
        R_PARISC_TLS_TPREL64 = R_PARISC_TPREL64,
        R_PARISC_HIRESERVE = 255
    }
}