namespace PersonalTools.Enums
{
    /// <summary>
    /// MIPS架构的重定位类型枚举
    /// </summary>
    internal enum MipsRelocationType : uint
    {
        R_MIPS_NONE = 0,            /* No reloc */
        R_MIPS_16 = 1,              /* Direct 16 bit */
        R_MIPS_32 = 2,              /* Direct 32 bit */
        R_MIPS_REL32 = 3,           /* PC relative 32 bit */
        R_MIPS_26 = 4,              /* Direct 26 bit shifted */
        R_MIPS_HI16 = 5,            /* High 16 bit */
        R_MIPS_LO16 = 6,            /* Low 16 bit */
        R_MIPS_GPREL16 = 7,         /* GP relative 16 bit */
        R_MIPS_LITERAL = 8,          /* 16 bit literal entry */
        R_MIPS_GOT16 = 9,            /* 16 bit GOT entry */
        R_MIPS_PC16 = 10,            /* PC relative 16 bit */
        R_MIPS_CALL16 = 11,          /* 16 bit call through GOT */
        R_MIPS_GPREL32 = 12,         /* GP relative 32 bit */
        R_MIPS_UNUSED1 = 13,         /* Unused */
        R_MIPS_UNUSED2 = 14,         /* Unused */
        R_MIPS_UNUSED3 = 15,         /* Unused */
        R_MIPS_SHIFT5 = 16,
        R_MIPS_SHIFT6 = 17,
        R_MIPS_64 = 18,
        R_MIPS_GOT_DISP = 19,
        R_MIPS_GOT_PAGE = 20,
        R_MIPS_GOT_OFST = 21,
        R_MIPS_GOT_HI16 = 22,        /* GOT HI 16 bit */
        R_MIPS_GOT_LO16 = 23,        /* GOT LO 16 bit */
        R_MIPS_SUB = 24,
        R_MIPS_INSERT_A = 25,
        R_MIPS_INSERT_B = 26,
        R_MIPS_DELETE = 27,
        R_MIPS_HIGHER = 28,
        R_MIPS_HIGHEST = 29,
        R_MIPS_CALL_HI16 = 30,       /* upper 16 bit GOT call entry */
        R_MIPS_CALL_LO16 = 31,       /* lower 16 bit GOT call entry */
        R_MIPS_SCN_DISP = 32,
        R_MIPS_REL16 = 33,
        R_MIPS_ADD_IMMEDIATE = 34,
        R_MIPS_PJUMP = 35,
        R_MIPS_RELGOT = 36,
        R_MIPS_JALR = 37,
        R_MIPS_TLS_DTPMOD32 = 38,    /* Module number 32 bit */
        R_MIPS_TLS_DTPREL32 = 39,    /* Module-relative offset 32 bit */
        R_MIPS_TLS_DTPMOD64 = 40,    /* Module number 64 bit */
        R_MIPS_TLS_DTPREL64 = 41,    /* Module-relative offset 64 bit */
        R_MIPS_TLS_GD = 42,          /* 16 bit GOT offset for GD */
        R_MIPS_TLS_LDM = 43,         /* 16 bit GOT offset for LDM */
        R_MIPS_TLS_DTPREL_HI16 = 44, /* Module-relative offset, high 16 bits */
        R_MIPS_TLS_DTPREL_LO16 = 45, /* Module-relative offset, low 16 bits */
        R_MIPS_TLS_GOTTPREL = 46,    /* 16 bit GOT offset for IE */
        R_MIPS_TLS_TPREL32 = 47,     /* TP-relative offset, 32 bit */
        R_MIPS_TLS_TPREL64 = 48,     /* TP-relative offset, 64 bit */
        R_MIPS_TLS_TPREL_HI16 = 49,  /* TP-relative offset, high 16 bits */
        R_MIPS_TLS_TPREL_LO16 = 50,  /* TP-relative offset, low 16 bits */
        R_MIPS_GLOB_DAT = 51,
        R_MIPS_COPY = 126,
        R_MIPS_JUMP_SLOT = 127
    }
}