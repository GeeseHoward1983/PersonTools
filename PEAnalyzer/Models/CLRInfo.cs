namespace PersonalTools.PEAnalyzer.Models
{
    /// <summary>
    /// CLR信息
    /// </summary>
    internal sealed class CLRInfo
    {
        public ushort MajorRuntimeVersion { get; set; }
        public ushort MinorRuntimeVersion { get; set; }
        public uint Flags { get; set; }
        public uint EntryPointTokenOrRva { get; set; }
        public bool HasMetaData { get; set; }
        public bool HasResources { get; set; }
        public bool HasStrongNameSignature { get; set; }
        public bool IsILonly { get; set; }
        public bool Is32BitRequired { get; set; }
        public bool Is32BitPreferred { get; set; }
        public bool IsStrongNameSigned { get; set; }

        // 保存PE头中的Machine字段，用于更准确地判断架构
        public ushort PEMachineType { get; set; }

        // 获取运行时版本描述
        public string RuntimeVersion => $"{MajorRuntimeVersion}.{MinorRuntimeVersion}";

        /// <summary>
        /// 获取.NET程序的架构类型
        /// </summary>
        public string Architecture
        {
            get
            {
                // 依据 CorFlags 的 (32BITREQUIRED, 32BITPREFERRED) 组合判定目标架构。
                // 注意：32BITPREFERRED(anycpu32bitpreferred) 是 VS2012 起 AnyCPU EXE 的默认，
                // 架构中立、仅在 64 位系统上默认以 32 位进程运行，不能等同于纯 x86。
                // 当两标志都未置位时，程序集为架构中立，需借 COFF Machine 字段区分原生 x64/ARM64。
                return (Is32BitRequired, Is32BitPreferred) switch
                {
                    (true, true) => "Any CPU (32-bit preferred)", // anycpu32bitpreferred
                    (true, false) => "x86",                        // 明确要求32位运行
                    (false, true) => "Any CPU (32-bit preferred)", // 罕见：首选但不强制，语义同上
                    (false, false) => PEMachineType switch          // 无32位标志：由 Machine 判原生架构
                    {
                        0x8664 => "x64",   // IMAGE_FILE_MACHINE_AMD64
                        0xAA64 => "ARM64", // IMAGE_FILE_MACHINE_ARM64
                        0x01C4 => "ARM",   // IMAGE_FILE_MACHINE_ARMNT
                        _ => "Any CPU"     // I386(0x14C) 等：架构中立
                    }
                };
            }
        }

        // 获取标志位描述
        public List<string> FlagDescriptions
        {
            get
            {
                List<string> descriptions = [];
                if (IsILonly)
                {
                    descriptions.Add("IL Only");
                }

                if (Is32BitRequired)
                {
                    descriptions.Add("32-Bit Required");
                }

                if (Is32BitPreferred)
                {
                    descriptions.Add("32-Bit Preferred");
                }

                if (IsStrongNameSigned)
                {
                    descriptions.Add("Strong Name Signed");
                }

                return descriptions;
            }
        }
    }
}