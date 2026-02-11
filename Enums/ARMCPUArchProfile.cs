using System;

namespace PersonalTools.Enums
{
    /// <summary>
    /// ARM CPU架构配置文件枚举
    /// </summary>
    public enum ARMCPUArchProfile : sbyte
    {
        Application = (sbyte)'A',
        Realtime = (sbyte)'R',
        Microcontroller = (sbyte)'M'
    }
}