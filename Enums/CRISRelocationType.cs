using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// CRIS Relocation Types
    /// </summary>
    [Flags]
    internal enum CRISRelocationType : uint
    {
        R_CRIS_NONE = 0,
        R_CRIS_8 = 1,
        R_CRIS_16 = 2,
        R_CRIS_32 = 3,
        R_CRIS_8_PCREL = 4,
        R_CRIS_16_PCREL = 5,
        R_CRIS_32_PCREL = 6,
        R_CRIS_GNU_VTINHERIT = 7,
        R_CRIS_GNU_VTENTRY = 8,
        R_CRIS_COPY = 9,
        R_CRIS_GLOB_DAT = 10,
        R_CRIS_JUMP_SLOT = 11,
        R_CRIS_RELATIVE = 12,
        R_CRIS_16_GOT = 13,
        R_CRIS_32_GOT = 14,
        R_CRIS_16_GOTPLT = 15,
        R_CRIS_32_GOTPLT = 16,
        R_CRIS_32_GOTREL = 17,
        R_CRIS_32_PLT_GOTREL = 18,
        R_CRIS_32_PLT_PCREL = 19
    }
}