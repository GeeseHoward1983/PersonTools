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
                // 首先根据CLR头中的标志位判断.NET程序的目标架构类型
                if (Is32BitRequired)
                {
                    return "x86"; // 明确要求32位运行
                }
                else if (!Is32BitRequired && Is32BitPreferred)
                {
                    return "x86"; // 32位首选（在64位系统上通过WoW64运行）
                }
                else if (!Is32BitRequired && !Is32BitPreferred)
                {
                    return "Any CPU"; // 可以在任何CPU架构上运行
                }

                return "Unknown"; // 无法确定的架构
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