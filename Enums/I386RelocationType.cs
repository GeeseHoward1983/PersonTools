using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// I386 Relocation Types
    /// </summary>
    [Flags]
    internal enum I386RelocationType : uint
    {
        R_386_NONE = 0,        /* No reloc */
        R_386_32 = 1,          /* Direct 32 bit  */
        R_386_PC32 = 2,        /* PC relative 32 bit */
        R_386_GOT32 = 3,       /* 32 bit GOT entry */
        R_386_PLT32 = 4,       /* 32 bit PLT address */
        R_386_COPY = 5,        /* Copy symbol at runtime */
        R_386_GLOB_DAT = 6,    /* Create GOT entry */
        R_386_JMP_SLOT = 7,    /* Create PLT entry */
        R_386_RELATIVE = 8,    /* Adjust by program base */
        R_386_GOTOFF = 9,      /* 32 bit offset to GOT */
        R_386_GOTPC = 10,      /* 32 bit PC relative offset to GOT */
        R_386_32PLT = 11,
        R_386_TLS_TPOFF = 14,  /* Offset in static TLS block */
        R_386_TLS_IE = 15,     /* Address of GOT entry for static TLS block offset */
        R_386_TLS_GOTIE = 16,  /* GOT entry for static TLS block offset */
        R_386_TLS_LE = 17,     /* Offset relative to static TLS block */
        R_386_TLS_GD = 18,     /* Direct 32 bit for GNU version of general dynamic thread local data */
        R_386_TLS_LDM = 19,    /* Direct 32 bit for GNU version of local dynamic thread local data in LE code */
        R_386_16 = 20,
        R_386_PC16 = 21,
        R_386_8 = 22,
        R_386_PC8 = 23,
        R_386_TLS_GD_32 = 24,  /* Direct 32 bit for general dynamic thread local data */
        R_386_TLS_GD_PUSH = 25, /* Tag for pushl in GD TLS code */
        R_386_TLS_GD_CALL = 26, /* Relocation for call to __tls_get_addr() */
        R_386_TLS_GD_POP = 27,  /* Tag for popl in GD TLS code */
        R_386_TLS_LDM_32 = 28,  /* Direct 32 bit for local dynamic thread local data in LE code */
        R_386_TLS_LDM_PUSH = 29, /* Tag for pushl in LDM TLS code */
        R_386_TLS_LDM_CALL = 30, /* Relocation for call to __tls_get_addr() in LDM code */
        R_386_TLS_LDM_POP = 31,  /* Tag for popl in LDM TLS code */
        R_386_TLS_LDO_32 = 32,   /* Offset relative to TLS block */
        R_386_TLS_IE_32 = 33,    /* GOT entry for negated static TLS block offset */
        R_386_TLS_LE_32 = 34,    /* Negated offset relative to static TLS block */
        R_386_TLS_DTPMOD32 = 35, /* ID of module containing symbol */
        R_386_TLS_DTPOFF32 = 36, /* Offset in TLS block */
        R_386_TLS_TPOFF32 = 37,  /* Negated offset in static TLS block */
        R_386_SIZE32 = 38,       /* 32-bit symbol size */
        R_386_TLS_GOTDESC = 39,  /* GOT offset for TLS descriptor */
        R_386_TLS_DESC_CALL = 40, /* Marker of call through TLS descriptor for relaxation */
        R_386_TLS_DESC = 41,      /* TLS descriptor containing pointer to code and to argument, returning the TLS offset for the symbol */
        R_386_IRELATIVE = 42      /* Adjust indirectly by program base */
    }
}