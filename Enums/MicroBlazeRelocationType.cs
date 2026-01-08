using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// MicroBlaze Relocation Types
    /// </summary>
    [Flags]
    public enum MicroBlazeRelocationType : uint
    {
        R_MICROBLAZE_NONE = 0,            /* No reloc. */
        R_MICROBLAZE_32 = 1,              /* Direct 32 bit. */
        R_MICROBLAZE_32_PCREL = 2,        /* PC relative 32 bit. */
        R_MICROBLAZE_64_PCREL = 3,        /* PC relative 64 bit. */
        R_MICROBLAZE_32_PCREL_LO = 4,     /* Low 16 bits of PCREL32. */
        R_MICROBLAZE_64 = 5,              /* Direct 64 bit. */
        R_MICROBLAZE_32_LO = 6,           /* Low 16 bit. */
        R_MICROBLAZE_SRO32 = 7,           /* Read-only small data area. */
        R_MICROBLAZE_SRW32 = 8,           /* Read-write small data area. */
        R_MICROBLAZE_64_NONE = 9,         /* No reloc. */
        R_MICROBLAZE_32_SYM_OP_SYM = 10,  /* Symbol Op Symbol relocation. */
        R_MICROBLAZE_GNU_VTINHERIT = 11,  /* GNU C++ vtable hierarchy. */
        R_MICROBLAZE_GNU_VTENTRY = 12,    /* GNU C++ vtable member usage. */
        R_MICROBLAZE_GOTPC_64 = 13,       /* PC-relative GOT offset. */
        R_MICROBLAZE_GOT_64 = 14,         /* GOT entry offset. */
        R_MICROBLAZE_PLT_64 = 15,         /* PLT offset (PC-relative). */
        R_MICROBLAZE_REL = 16,            /* Adjust by program base. */
        R_MICROBLAZE_JUMP_SLOT = 17,      /* Create PLT entry. */
        R_MICROBLAZE_GLOB_DAT = 18,       /* Create GOT entry. */
        R_MICROBLAZE_GOTOFF_64 = 19,      /* 64 bit offset to GOT. */
        R_MICROBLAZE_GOTOFF_32 = 20,      /* 32 bit offset to GOT. */
        R_MICROBLAZE_COPY = 21,           /* Runtime copy. */
        R_MICROBLAZE_TLS = 22,            /* TLS Reloc. */
        R_MICROBLAZE_TLSGD = 23,          /* TLS General Dynamic. */
        R_MICROBLAZE_TLSLD = 24,          /* TLS Local Dynamic. */
        R_MICROBLAZE_TLSDTPMOD32 = 25,    /* TLS Module ID. */
        R_MICROBLAZE_TLSDTPREL32 = 26,    /* TLS Offset Within TLS Block. */
        R_MICROBLAZE_TLSDTPREL64 = 27,    /* TLS Offset Within TLS Block. */
        R_MICROBLAZE_TLSGOTTPREL32 = 28,  /* TLS Offset From Thread Pointer. */
        R_MICROBLAZE_TLSTPREL32 = 29      /* TLS Offset From Thread Pointer. */
    }
}