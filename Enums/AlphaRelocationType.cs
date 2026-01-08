using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// Alpha Relocation Types
    /// </summary>
    [Flags]
    public enum AlphaRelocationType : uint
    {
        R_ALPHA_NONE = 0,        /* No reloc */
        R_ALPHA_REFLONG = 1,     /* Direct 32 bit */
        R_ALPHA_REFQUAD = 2,     /* Direct 64 bit */
        R_ALPHA_GPREL32 = 3,     /* GP relative 32 bit */
        R_ALPHA_LITERAL = 4,     /* GP relative 16 bit w/optimization */
        R_ALPHA_LITUSE = 5,      /* Optimization hint for LITERAL */
        R_ALPHA_GPDISP = 6,      /* Add displacement to GP */
        R_ALPHA_BRADDR = 7,      /* PC+4 relative 23 bit shifted */
        R_ALPHA_HINT = 8,        /* PC+4 relative 16 bit shifted */
        R_ALPHA_SREL16 = 9,      /* PC relative 16 bit */
        R_ALPHA_SREL32 = 10,     /* PC relative 32 bit */
        R_ALPHA_SREL64 = 11,     /* PC relative 64 bit */
        R_ALPHA_GPRELHIGH = 17,  /* GP relative 32 bit, high 16 bits */
        R_ALPHA_GPRELLOW = 18,   /* GP relative 32 bit, low 16 bits */
        R_ALPHA_GPREL16 = 19,    /* GP relative 16 bit */
        R_ALPHA_COPY = 24,       /* Copy symbol at runtime */
        R_ALPHA_GLOB_DAT = 25,   /* Create GOT entry */
        R_ALPHA_JMP_SLOT = 26,   /* Create PLT entry */
        R_ALPHA_RELATIVE = 27,   /* Adjust by program base */
        R_ALPHA_TLS_GD_HI = 28,
        R_ALPHA_TLSGD = 29,
        R_ALPHA_TLS_LDM = 30,
        R_ALPHA_DTPMOD64 = 31,
        R_ALPHA_GOTDTPREL = 32,
        R_ALPHA_DTPREL64 = 33,
        R_ALPHA_DTPRELHI = 34,
        R_ALPHA_DTPRELLO = 35,
        R_ALPHA_DTPREL16 = 36,
        R_ALPHA_GOTTPREL = 37,
        R_ALPHA_TPREL64 = 38,
        R_ALPHA_TPRELHI = 39,
        R_ALPHA_TPRELLO = 40,
        R_ALPHA_TPREL16 = 41
    }
}