using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// 虚拟化使用情况枚举
    /// </summary>
    internal enum VirtualizationUse : byte
    {
        None = 0,
        TrustZone = 1,
        Virtualization_Extensions = 2,
        TrustZone_plus_Virtualization = 3
    }
}