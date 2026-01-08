namespace PersonalTools.Enums
{
    /// <summary>
    /// Motorola 68k Relocation Types
    /// </summary>
    [Flags]
    public enum M68kRelocationType : uint
    {
        R_68K_NONE = 0,        /* No reloc */
        R_68K_32 = 1,          /* Direct 32 bit  */
        R_68K_16 = 2,          /* Direct 16 bit  */
        R_68K_8 = 3,           /* Direct 8 bit  */
        R_68K_PC32 = 4,        /* PC relative 32 bit */
        R_68K_PC16 = 5,        /* PC relative 16 bit */
        R_68K_PC8 = 6,         /* PC relative 8 bit */
        R_68K_GOT32 = 7,       /* 32 bit PC relative GOT entry */
        R_68K_GOT16 = 8,       /* 16 bit PC relative GOT entry */
        R_68K_GOT8 = 9,        /* 8 bit PC relative GOT entry */
        R_68K_GOT32O = 10,     /* 32 bit GOT offset */
        R_68K_GOT16O = 11,     /* 16 bit GOT offset */
        R_68K_GOT8O = 12,      /* 8 bit GOT offset */
        R_68K_PLT32 = 13,      /* 32 bit PC relative PLT address */
        R_68K_PLT16 = 14,      /* 16 bit PC relative PLT address */
        R_68K_PLT8 = 15,       /* 8 bit PC relative PLT address */
        R_68K_PLT32O = 16,     /* 32 bit PLT offset */
        R_68K_PLT16O = 17,     /* 16 bit PLT offset */
        R_68K_PLT8O = 18,      /* 8 bit PLT offset */
        R_68K_COPY = 19,       /* Copy symbol at runtime */
        R_68K_GLOB_DAT = 20,   /* Create GOT entry */
        R_68K_JMP_SLOT = 21,   /* Create PLT entry */
        R_68K_RELATIVE = 22,   /* Adjust by program base */
        R_68K_TLS_GD32 = 25,   /* 32 bit GOT offset for GD */
        R_68K_TLS_GD16 = 26,   /* 16 bit GOT offset for GD */
        R_68K_TLS_GD8 = 27,    /* 8 bit GOT offset for GD */
        R_68K_TLS_LDM32 = 28,  /* 32 bit GOT offset for LDM */
        R_68K_TLS_LDM16 = 29,  /* 16 bit GOT offset for LDM */
        R_68K_TLS_LDM8 = 30,   /* 8 bit GOT offset for LDM */
        R_68K_TLS_LDO32 = 31,  /* 32 bit module-relative offset */
        R_68K_TLS_LDO16 = 32,  /* 16 bit module-relative offset */
        R_68K_TLS_LDO8 = 33,   /* 8 bit module-relative offset */
        R_68K_TLS_IE32 = 34,   /* 32 bit GOT offset for IE */
        R_68K_TLS_IE16 = 35,   /* 16 bit GOT offset for IE */
        R_68K_TLS_IE8 = 36,    /* 8 bit GOT offset for IE */
        R_68K_TLS_LE32 = 37,   /* 32 bit offset relative to static TLS block */
        R_68K_TLS_LE16 = 38,   /* 16 bit offset relative to static TLS block */
        R_68K_TLS_LE8 = 39,    /* 8 bit offset relative to static TLS block */
        R_68K_TLS_DTPMOD32 = 40, /* 32 bit module number */
        R_68K_TLS_DTPREL32 = 41, /* 32 bit module-relative offset */
        R_68K_TLS_TPREL32 = 42  /* 32 bit TP-relative offset */
    }
}