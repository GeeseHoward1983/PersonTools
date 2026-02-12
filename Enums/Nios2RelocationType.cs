using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// Nios2 Relocation Types
    /// </summary>
    [Flags]
    internal enum Nios2RelocationType : uint
    {
        R_NIOS2_NONE = 0,            /* No reloc. */
        R_NIOS2_S16 = 1,             /* Direct signed 16 bit. */
        R_NIOS2_U16 = 2,             /* Direct unsigned 16 bit. */
        R_NIOS2_PCREL16 = 3,         /* PC relative 16 bit. */
        R_NIOS2_CALL26 = 4,          /* Direct call. */
        R_NIOS2_IMM5 = 5,            /* 5 bit constant expression. */
        R_NIOS2_CACHE_OPX = 6,       /* 5 bit expression, shift 22. */
        R_NIOS2_IMM6 = 7,            /* 6 bit constant expression. */
        R_NIOS2_IMM8 = 8,            /* 8 bit constant expression. */
        R_NIOS2_HI16 = 9,            /* High 16 bit. */
        R_NIOS2_LO16 = 10,           /* Low 16 bit. */
        R_NIOS2_HIADJ16 = 11,        /* High 16 bit, adjusted. */
        R_NIOS2_BFD_RELOC_32 = 12,   /* 32 bit symbol value + addend. */
        R_NIOS2_BFD_RELOC_16 = 13,   /* 16 bit symbol value + addend. */
        R_NIOS2_BFD_RELOC_8 = 14,    /* 8 bit symbol value + addend. */
        R_NIOS2_GPREL = 15,          /* 16 bit GP pointer offset. */
        R_NIOS2_GNU_VTINHERIT = 16,  /* GNU C++ vtable hierarchy. */
        R_NIOS2_GNU_VTENTRY = 17,    /* GNU C++ vtable member usage. */
        R_NIOS2_UJMP = 18,           /* Unconditional branch. */
        R_NIOS2_CJMP = 19,           /* Conditional branch. */
        R_NIOS2_CALLR = 20,          /* Indirect call through register. */
        R_NIOS2_ALIGN = 21,          /* Alignment requirement for linker relaxation. */
        R_NIOS2_GOT16 = 22,          /* 16 bit GOT entry. */
        R_NIOS2_CALL16 = 23,         /* 16 bit GOT entry for function. */
        R_NIOS2_GOTOFF_LO = 24,      /* %lo of offset to GOT pointer. */
        R_NIOS2_GOTOFF_HA = 25,      /* %hiadj of offset to GOT pointer. */
        R_NIOS2_PCREL_LO = 26,       /* %lo of PC relative offset. */
        R_NIOS2_PCREL_HA = 27,       /* %hiadj of PC relative offset. */
        R_NIOS2_TLS_GD16 = 28,       /* 16 bit GOT offset for TLS GD. */
        R_NIOS2_TLS_LDM16 = 29,      /* 16 bit GOT offset for TLS LDM. */
        R_NIOS2_TLS_LDO16 = 30,      /* 16 bit module relative offset. */
        R_NIOS2_TLS_IE16 = 31,       /* 16 bit GOT offset for TLS IE. */
        R_NIOS2_TLS_LE16 = 32,       /* 16 bit LE TP-relative offset. */
        R_NIOS2_TLS_DTPMOD = 33,     /* Module number. */
        R_NIOS2_TLS_DTPREL = 34,     /* Module-relative offset. */
        R_NIOS2_TLS_TPREL = 35,      /* TP-relative offset. */
        R_NIOS2_COPY = 36,           /* Copy symbol at runtime. */
        R_NIOS2_GLOB_DAT = 37,       /* Create GOT entry. */
        R_NIOS2_JUMP_SLOT = 38,      /* Create PLT entry. */
        R_NIOS2_RELATIVE = 39,       /* Adjust by program base. */
        R_NIOS2_GOTOFF = 40,         /* 16 bit offset to GOT pointer. */
        R_NIOS2_CALL26_NOAT = 41,    /* Direct call in .noat section. */
        R_NIOS2_GOT_LO = 42,         /* %lo() of GOT entry. */
        R_NIOS2_GOT_HA = 43,         /* %hiadj() of GOT entry. */
        R_NIOS2_CALL_LO = 44,        /* %lo() of function GOT entry. */
        R_NIOS2_CALL_HA = 45         /* %hiadj() of function GOT entry. */
    }
}