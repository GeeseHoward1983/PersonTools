using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// AM33 (MN10300) Relocation Types
    /// </summary>
    [Flags]
    internal enum AM33RelocationType : uint
    {
        R_MN10300_NONE = 0,        /* No reloc. */
        R_MN10300_32 = 1,          /* Direct 32 bit. */
        R_MN10300_16 = 2,          /* Direct 16 bit. */
        R_MN10300_8 = 3,           /* Direct 8 bit. */
        R_MN10300_PCREL32 = 4,     /* PC-relative 32-bit. */
        R_MN10300_PCREL16 = 5,     /* PC-relative 16-bit signed. */
        R_MN10300_PCREL8 = 6,      /* PC-relative 8-bit signed. */
        R_MN10300_GNU_VTINHERIT = 7, /* Ancient C++ vtable garbage... */
        R_MN10300_GNU_VTENTRY = 8,  /* ... collection annotation. */
        R_MN10300_24 = 9,          /* Direct 24 bit. */
        R_MN10300_GOTPC32 = 10,     /* 32-bit PCrel offset to GOT. */
        R_MN10300_GOTPC16 = 11,     /* 16-bit PCrel offset to GOT. */
        R_MN10300_GOTOFF32 = 12,    /* 32-bit offset from GOT. */
        R_MN10300_GOTOFF24 = 13,    /* 24-bit offset from GOT. */
        R_MN10300_GOTOFF16 = 14,    /* 16-bit offset from GOT. */
        R_MN10300_PLT32 = 15,       /* 32-bit PCrel to PLT entry. */
        R_MN10300_PLT16 = 16,       /* 16-bit PCrel to PLT entry. */
        R_MN10300_GOT32 = 17,       /* 32-bit offset to GOT entry. */
        R_MN10300_GOT24 = 18,       /* 24-bit offset to GOT entry. */
        R_MN10300_GOT16 = 19,       /* 16-bit offset to GOT entry. */
        R_MN10300_COPY = 20,        /* Copy symbol at runtime. */
        R_MN10300_GLOB_DAT = 21,    /* Create GOT entry. */
        R_MN10300_JMP_SLOT = 22,    /* Create PLT entry. */
        R_MN10300_RELATIVE = 23,    /* Adjust by program base. */
        R_MN10300_TLS_GD = 24,      /* 32-bit offset for global dynamic. */
        R_MN10300_TLS_LD = 25,      /* 32-bit offset for local dynamic. */
        R_MN10300_TLS_LDO = 26,     /* Module-relative offset. */
        R_MN10300_TLS_GOTIE = 27,   /* GOT offset for static TLS block offset. */
        R_MN10300_TLS_IE = 28,      /* GOT address for static TLS block offset. */
        R_MN10300_TLS_LE = 29,      /* Offset relative to static TLS block. */
        R_MN10300_TLS_DTPMOD = 30,  /* ID of module containing symbol. */
        R_MN10300_TLS_DTPOFF = 31,  /* Offset in module TLS block. */
        R_MN10300_TLS_TPOFF = 32,   /* Offset in static TLS block. */
        R_MN10300_SYM_DIFF = 33,    /* Adjustment for next reloc as needed by linker relaxation. */
        R_MN10300_ALIGN = 34        /* Alignment requirement for linker relaxation. */
    }
}