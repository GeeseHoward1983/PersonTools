using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// PowerPC Relocation Types
    /// </summary>
    [Flags]
    internal enum PowerPCRelocationType : uint
    {
        R_PPC_NONE = 0,
        R_PPC_ADDR32 = 1,        /* 32bit absolute address */
        R_PPC_ADDR24 = 2,        /* 26bit address, 2 bits ignored. */
        R_PPC_ADDR16 = 3,        /* 16bit absolute address */
        R_PPC_ADDR16_LO = 4,     /* lower 16bit of absolute address */
        R_PPC_ADDR16_HI = 5,     /* high 16bit of absolute address */
        R_PPC_ADDR16_HA = 6,     /* adjusted high 16bit */
        R_PPC_ADDR14 = 7,        /* 16bit address, 2 bits ignored */
        R_PPC_ADDR14_BRTAKEN = 8,
        R_PPC_ADDR14_BRNTAKEN = 9,
        R_PPC_REL24 = 10,        /* PC relative 26 bit */
        R_PPC_REL14 = 11,        /* PC relative 16 bit */
        R_PPC_REL14_BRTAKEN = 12,
        R_PPC_REL14_BRNTAKEN = 13,
        R_PPC_GOT16 = 14,
        R_PPC_GOT16_LO = 15,
        R_PPC_GOT16_HI = 16,
        R_PPC_GOT16_HA = 17,
        R_PPC_PLTREL24 = 18,
        R_PPC_COPY = 19,
        R_PPC_GLOB_DAT = 20,
        R_PPC_JMP_SLOT = 21,
        R_PPC_RELATIVE = 22,
        R_PPC_LOCAL24PC = 23,
        R_PPC_UADDR32 = 24,
        R_PPC_UADDR16 = 25,
        R_PPC_REL32 = 26,
        R_PPC_PLT32 = 27,
        R_PPC_PLTREL32 = 28,
        R_PPC_PLT16_LO = 29,
        R_PPC_PLT16_HI = 30,
        R_PPC_PLT16_HA = 31,
        R_PPC_SDAREL16 = 32,
        R_PPC_SECTOFF = 33,
        R_PPC_SECTOFF_LO = 34,
        R_PPC_SECTOFF_HI = 35,
        R_PPC_SECTOFF_HA = 36,
        R_PPC_TLS = 67,          /* none	(sym+add)@tls */
        R_PPC_DTPMOD32 = 68,     /* word32	(sym+add)@dtpmod */
        R_PPC_TPREL16 = 69,      /* half16*	(sym+add)@tprel */
        R_PPC_TPREL16_LO = 70,   /* half16	(sym+add)@tprel@l */
        R_PPC_TPREL16_HI = 71,   /* half16	(sym+add)@tprel@h */
        R_PPC_TPREL16_HA = 72,   /* half16	(sym+add)@tprel@ha */
        R_PPC_TPREL32 = 73,      /* word32	(sym+add)@tprel */
        R_PPC_DTPREL16 = 74,     /* half16*	(sym+add)@dtprel */
        R_PPC_DTPREL16_LO = 75,  /* half16	(sym+add)@dtprel@l */
        R_PPC_DTPREL16_HI = 76,  /* half16	(sym+add)@dtprel@h */
        R_PPC_DTPREL16_HA = 77,  /* half16	(sym+add)@dtprel@ha */
        R_PPC_DTPREL32 = 78,     /* word32	(sym+add)@dtprel */
        R_PPC_GOT_TLSGD16 = 79,  /* half16*	(sym+add)@got@tlsgd */
        R_PPC_GOT_TLSGD16_LO = 80, /* half16	(sym+add)@got@tlsgd@l */
        R_PPC_GOT_TLSGD16_HI = 81, /* half16	(sym+add)@got@tlsgd@h */
        R_PPC_GOT_TLSGD16_HA = 82, /* half16	(sym+add)@got@tlsgd@ha */
        R_PPC_GOT_TLSLD16 = 83,  /* half16*	(sym+add)@got@tlsld */
        R_PPC_GOT_TLSLD16_LO = 84, /* half16	(sym+add)@got@tlsld@l */
        R_PPC_GOT_TLSLD16_HI = 85, /* half16	(sym+add)@got@tlsld@h */
        R_PPC_GOT_TLSLD16_HA = 86, /* half16	(sym+add)@got@tlsld@ha */
        R_PPC_GOT_TPREL16 = 87,  /* half16*	(sym+add)@got@tprel */
        R_PPC_GOT_TPREL16_LO = 88, /* half16	(sym+add)@got@tprel@l */
        R_PPC_GOT_TPREL16_HI = 89, /* half16	(sym+add)@got@tprel@h */
        R_PPC_GOT_TPREL16_HA = 90, /* half16	(sym+add)@got@tprel@ha */
        R_PPC_GOT_DTPREL16 = 91, /* half16*	(sym+add)@got@dtprel */
        R_PPC_GOT_DTPREL16_LO = 92, /* half16*	(sym+add)@got@dtprel@l */
        R_PPC_GOT_DTPREL16_HI = 93, /* half16*	(sym+add)@got@dtprel@h */
        R_PPC_GOT_DTPREL16_HA = 94, /* half16*	(sym+add)@got@dtprel@ha */
        R_PPC_TLSGD = 95,        /* none	(sym+add)@tlsgd */
        R_PPC_TLSLD = 96,        /* none	(sym+add)@tlsld */
        R_PPC_EMB_NADDR32 = 101,
        R_PPC_EMB_NADDR16 = 102,
        R_PPC_EMB_NADDR16_LO = 103,
        R_PPC_EMB_NADDR16_HI = 104,
        R_PPC_EMB_NADDR16_HA = 105,
        R_PPC_EMB_SDAI16 = 106,
        R_PPC_EMB_SDA2I16 = 107,
        R_PPC_EMB_SDA2REL = 108,
        R_PPC_EMB_SDA21 = 109,   /* 16 bit offset in SDA */
        R_PPC_EMB_MRKREF = 110,
        R_PPC_EMB_RELSEC16 = 111,
        R_PPC_EMB_RELST_LO = 112,
        R_PPC_EMB_RELST_HI = 113,
        R_PPC_EMB_RELST_HA = 114,
        R_PPC_EMB_BIT_FLD = 115,
        R_PPC_EMB_RELSDA = 116,  /* 16 bit relative offset in SDA */
        R_PPC_DIAB_SDA21_LO = 180, /* like EMB_SDA21, but lower 16 bit */
        R_PPC_DIAB_SDA21_HI = 181, /* like EMB_SDA21, but high 16 bit */
        R_PPC_DIAB_SDA21_HA = 182, /* like EMB_SDA21, adjusted high 16 */
        R_PPC_DIAB_RELSDA_LO = 183, /* like EMB_RELSDA, but lower 16 bit */
        R_PPC_DIAB_RELSDA_HI = 184, /* like EMB_RELSDA, but high 16 bit */
        R_PPC_DIAB_RELSDA_HA = 185, /* like EMB_RELSDA, adjusted high 16 */
        R_PPC_IRELATIVE = 248,
        R_PPC_REL16 = 249,       /* half16   (sym+add-.) */
        R_PPC_REL16_LO = 250,    /* half16   (sym+add-.)@l */
        R_PPC_REL16_HI = 251,    /* half16   (sym+add-.)@h */
        R_PPC_REL16_HA = 252,    /* half16   (sym+add-.)@ha */
        R_PPC_TOC16 = 255
    }
}