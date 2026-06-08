using PersonalTools.Enums;
using System.Globalization;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFAttributeInfo
    {
        public static string GetFormattedAttributeInfo(ELFParser parser)
        {
            StringBuilder sb = new();

            if (parser.SectionHeaders != null)
            {
                for (int i = 0; i < parser.SectionHeaders.Count; i++)
                {
                    if (parser.SectionHeaders[i].sh_type is ((uint)SectionType.SHT_GNU_ATTRIBUTES) or ((uint)SectionType.SHT_ARM_ATTRIBUTES))
                    {
                        string attrInfo = ParseAttributeSection(parser, parser.SectionHeaders[i]);
                        if (!string.IsNullOrEmpty(attrInfo))
                        {
                            sb.AppendLine(attrInfo);
                        }
                    }
                }
            }

            if (sb.Length == 0)
            {
                sb.AppendLine("No attribute sections found.");
            }

            return sb.ToString();
        }

        private static string ParseAttributeSection(ELFParser parser, Models.ELFSectionHeader section)
        {
            StringBuilder sb = new();

            // 读取属性段的数据
            byte[] data = parser.CopySectionData(in section);
            bool isLittleEndian = parser.Header.IsLittleEndian();

            int offset = 0;

            // 解析属性段格式版本 (固定为'A' = 0x41)
            if (offset >= data.Length || data[offset] != 0x41)
            {
                return sb.ToString();
            }
            offset++;

            // 逐个解析供应商子节，直到数据耗尽或子节读取失败
            while (offset < data.Length && ParseVendorSubsection(parser, data, ref offset, isLittleEndian, sb))
            {
            }

            return sb.ToString();
        }

        // 解析一个供应商子节；返回 false 表示数据不足应停止
        private static bool ParseVendorSubsection(ELFParser parser, byte[] data, ref int offset, bool isLittleEndian, StringBuilder sb)
        {
            // 子节长度字段：含本 4 字节长度 + 供应商名 + 数据（ARM/GNU 规范）
            int subSectionStart = offset;
            uint subSectionLength = ELFParserUtils.ReadUInt32(data, offset, isLittleEndian);
            offset += 4;

            if (offset >= data.Length)
            {
                return false;
            }

            // 解析供应商名称 (null终止字符串)
            int vendorNameStart = offset;
            while (offset < data.Length && data[offset] != 0)
            {
                offset++;
            }
            string vendorName = Encoding.UTF8.GetString(data, vendorNameStart, offset - vendorNameStart);
            offset++; // 跳过null终止符

            sb.AppendLine(CultureInfo.InvariantCulture, $"Attribute Section: {vendorName}");

            // 子节结束位置 = 子节起始 + 子节长度（length 字段含其自身与 vendor 名）
            int subSectionEnd = subSectionStart + (int)subSectionLength;
            if (subSectionEnd > data.Length || subSectionLength == 0)
            {
                subSectionEnd = data.Length;
            }

            if (vendorName == "aeabi")
            {
                ParseAEABIAttributes(data, ref offset, subSectionEnd, sb);
            }
            else if (vendorName == "riscv")
            {
                ParseRiscvAttributes(data, ref offset, subSectionEnd, sb);
            }
            else
            {
                // 通用 GNU 方言：MIPS/PowerPC/LoongArch/S390/SPARC 等（vendor 通常为 "gnu"）
                ParseGnuAttributes(parser, data, ref offset, subSectionEnd, sb);
            }

            // 确保offset不会倒退
            if (offset < subSectionEnd)
            {
                offset = subSectionEnd;
            }
            return true;
        }

        // ---- AEABI (ARM EABI) 属性解析，输出对齐 readelf ----
        // 各整型标签的取值→名称表（与 binutils arm_attr_tag_* 保持一致）
        private static readonly string[] s_aeabiCpuArch = ["Pre-v4", "v4", "v4T", "v5T", "v5TE", "v5TEJ", "v6", "v6KZ", "v6T2", "v6K", "v7", "v6-M", "v6S-M", "v7E-M", "v8-A", "v8-R", "v8-M.baseline", "v8-M.mainline", "v8.1-A", "v8.2-A", "v8.3-A", "v8.1-M.mainline", "v9"];
        private static readonly string[] s_aeabiArmIsaUse = ["No", "Yes"];
        private static readonly string[] s_aeabiThumbIsaUse = ["No", "Thumb-1", "Thumb-2", "Yes"];
        private static readonly string[] s_aeabiFpArch = ["No", "VFPv1", "VFPv2", "VFPv3", "VFPv3-D16", "VFPv4", "VFPv4-D16", "FP for ARMv8", "FPv5/FP-D16 for ARMv8"];
        private static readonly string[] s_aeabiWmmxArch = ["No", "WMMXv1", "WMMXv2"];
        private static readonly string[] s_aeabiAdvSimdArch = ["No", "NEONv1", "NEONv1 with Fused-MAC", "NEON for ARMv8", "NEON for ARMv8.1"];
        private static readonly string[] s_aeabiPcsConfig = ["None", "Bare platform", "Linux application", "Linux DSO", "PalmOS 2004", "PalmOS (reserved)", "SymbianOS 2004", "SymbianOS (reserved)"];
        private static readonly string[] s_aeabiPcsR9Use = ["V6", "SB", "TLS", "Unused"];
        private static readonly string[] s_aeabiPcsRwData = ["Absolute", "PC-relative", "SB-relative", "None"];
        private static readonly string[] s_aeabiPcsRoData = ["Absolute", "PC-relative", "None"];
        private static readonly string[] s_aeabiPcsGotUse = ["None", "direct", "GOT-indirect"];
        private static readonly string[] s_aeabiPcsWcharT = ["None", "??? 1", "2", "??? 3", "4"];
        private static readonly string[] s_aeabiFpRounding = ["Unused", "Needed"];
        private static readonly string[] s_aeabiFpDenormal = ["Unused", "Needed", "Sign only"];
        private static readonly string[] s_aeabiFpExceptions = ["Unused", "Needed"];
        private static readonly string[] s_aeabiFpUserExceptions = ["Unused", "Needed"];
        private static readonly string[] s_aeabiFpNumberModel = ["Unused", "Finite", "RTABI", "IEEE 754"];
        private static readonly string[] s_aeabiEnumSize = ["Unused", "small", "int", "forced to int"];
        private static readonly string[] s_aeabiHardFpUse = ["As Tag_FP_arch", "SP only", "Reserved", "Deprecated"];
        private static readonly string[] s_aeabiVfpArgs = ["AAPCS", "VFP registers", "custom", "compatible"];
        private static readonly string[] s_aeabiWmmxArgs = ["AAPCS", "WMMX registers", "custom"];
        private static readonly string[] s_aeabiOptGoals = ["None", "Prefer Speed", "Aggressively prefer Speed", "Prefer Size", "Aggressively prefer Size", "Prefer Debug", "Aggressively prefer Debug", "Reserved"];
        private static readonly string[] s_aeabiFpOptGoals = ["None", "Prefer Speed", "Aggressively prefer Speed", "Prefer Size", "Aggressively prefer Size", "Prefer Accuracy", "Aggressively prefer Accuracy", "Reserved"];
        private static readonly string[] s_aeabiCpuUnalignedAccess = ["None", "v6"];
        private static readonly string[] s_aeabiFp16bitFormat = ["None", "IEEE 754", "Alternative Format"];
        private static readonly string[] s_aeabiFpHpExtension = ["Not Allowed", "Allowed"];
        private static readonly string[] s_aeabiT2eeUse = ["Not Allowed", "Allowed"];
        private static readonly string[] s_aeabiVirtualizationUse = ["Not Allowed", "TrustZone", "Virtualization Extensions", "TrustZone and Virtualization Extensions"];
        private static readonly string[] s_aeabiMpExtensionUse = ["Not Allowed", "Allowed"];
        private static readonly string[] s_aeabiDivUse = ["Allowed in Thumb-ISA, v7-R or v7-M", "Not allowed", "Allowed in v7-A with integer division extension"];
        private static readonly string[] s_aeabiDspExtension = ["Follow architecture", "Allowed"];
        private static readonly string[] s_aeabiMveArch = ["No MVE", "MVE Integer only", "MVE Integer and Floating Point"];

        private static string GetAEABITagName(int tag)
        {
            return tag switch
            {
                4 => "Tag_CPU_raw_name",
                5 => "Tag_CPU_name",
                6 => "Tag_CPU_arch",
                7 => "Tag_CPU_arch_profile",
                8 => "Tag_ARM_ISA_use",
                9 => "Tag_THUMB_ISA_use",
                10 => "Tag_FP_arch",
                11 => "Tag_WMMX_arch",
                12 => "Tag_Advanced_SIMD_arch",
                13 => "Tag_PCS_config",
                14 => "Tag_ABI_PCS_R9_use",
                15 => "Tag_ABI_PCS_RW_data",
                16 => "Tag_ABI_PCS_RO_data",
                17 => "Tag_ABI_PCS_GOT_use",
                18 => "Tag_ABI_PCS_wchar_t",
                19 => "Tag_ABI_FP_rounding",
                20 => "Tag_ABI_FP_denormal",
                21 => "Tag_ABI_FP_exceptions",
                22 => "Tag_ABI_FP_user_exceptions",
                23 => "Tag_ABI_FP_number_model",
                24 => "Tag_ABI_align_needed",
                25 => "Tag_ABI_align_preserved",
                26 => "Tag_ABI_enum_size",
                27 => "Tag_ABI_HardFP_use",
                28 => "Tag_ABI_VFP_args",
                29 => "Tag_ABI_WMMX_args",
                30 => "Tag_ABI_optimization_goals",
                31 => "Tag_ABI_FP_optimization_goals",
                32 => "Tag_compatibility",
                34 => "Tag_CPU_unaligned_access",
                36 => "Tag_FP_HP_extension",
                38 => "Tag_ABI_FP_16bit_format",
                42 => "Tag_MPextension_use",
                44 => "Tag_DIV_use",
                46 => "Tag_DSP_extension",
                48 => "Tag_MVE_arch",
                64 => "Tag_nodefaults",
                65 => "Tag_also_compatible_with",
                66 => "Tag_T2EE_use",
                67 => "Tag_conformance",
                68 => "Tag_Virtualization_use",
                70 => "Tag_MPextension_use",
                _ => $"Tag_unknown_{tag}"
            };
        }

        private static string[]? GetAEABIValueTable(int tag)
        {
            return tag switch
            {
                6 => s_aeabiCpuArch,
                8 => s_aeabiArmIsaUse,
                9 => s_aeabiThumbIsaUse,
                10 => s_aeabiFpArch,
                11 => s_aeabiWmmxArch,
                12 => s_aeabiAdvSimdArch,
                13 => s_aeabiPcsConfig,
                14 => s_aeabiPcsR9Use,
                15 => s_aeabiPcsRwData,
                16 => s_aeabiPcsRoData,
                17 => s_aeabiPcsGotUse,
                18 => s_aeabiPcsWcharT,
                19 => s_aeabiFpRounding,
                20 => s_aeabiFpDenormal,
                21 => s_aeabiFpExceptions,
                22 => s_aeabiFpUserExceptions,
                23 => s_aeabiFpNumberModel,
                26 => s_aeabiEnumSize,
                27 => s_aeabiHardFpUse,
                28 => s_aeabiVfpArgs,
                29 => s_aeabiWmmxArgs,
                30 => s_aeabiOptGoals,
                31 => s_aeabiFpOptGoals,
                34 => s_aeabiCpuUnalignedAccess,
                36 => s_aeabiFpHpExtension,
                38 => s_aeabiFp16bitFormat,
                42 => s_aeabiMpExtensionUse,
                44 => s_aeabiDivUse,
                46 => s_aeabiDspExtension,
                48 => s_aeabiMveArch,
                66 => s_aeabiT2eeUse,
                68 => s_aeabiVirtualizationUse,
                70 => s_aeabiMpExtensionUse,
                _ => null
            };
        }

        // 读取 ULEB128，并推进 offset（专用于 AEABI 路径，不影响 MIPS/通用路径的 ReadULEB128）
        private static int ReadAEABIUleb128(byte[] data, ref int offset, int endOffset)
        {
            int value = 0;
            int shift = 0;
            while (offset < endOffset && offset < data.Length)
            {
                byte b = data[offset++];
                value |= (b & 0x7f) << shift;
                if ((b & 0x80) == 0)
                {
                    break;
                }
                shift += 7;
            }
            return value;
        }

        private static void AppendAEABIStringAttr(string name, byte[] data, ref int offset, StringBuilder sb)
        {
            string value = ELFParserUtils.ExtractStringFromBytes(data, offset);
            offset += value.Length + 1; // 含 null 终止符
            sb.AppendLine(CultureInfo.InvariantCulture, $"  {name}: \"{value}\"");
        }

        private static void AppendAEABIIntAttr(int tag, byte[] data, ref int offset, int endOffset, StringBuilder sb)
        {
            string[]? table = GetAEABIValueTable(tag);
            if (table != null)
            {
                int val = ReadAEABIUleb128(data, ref offset, endOffset);
                string valueText = val >= 0 && val < table.Length ? table[val] : $"??? ({val})";
                sb.AppendLine(CultureInfo.InvariantCulture, $"  {GetAEABITagName(tag)}: {valueText}");
            }
            else if ((tag & 1) != 0)
            {
                // 未知奇数标签 → 字符串
                AppendAEABIStringAttr(GetAEABITagName(tag), data, ref offset, sb);
            }
            else
            {
                // 未知偶数标签 → ULEB128
                int val = ReadAEABIUleb128(data, ref offset, endOffset);
                sb.AppendLine(CultureInfo.InvariantCulture, $"  {GetAEABITagName(tag)}: {val}");
            }
        }

        private static void AppendAEABIProfileAttr(byte[] data, ref int offset, int endOffset, StringBuilder sb)
        {
            int val = ReadAEABIUleb128(data, ref offset, endOffset);
            string text = val switch
            {
                0 => "None",
                'A' => "Application",
                'R' => "Realtime",
                'M' => "Microcontroller",
                'S' => "Application or Realtime",
                _ => $"??? ({val})"
            };
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Tag_CPU_arch_profile: {text}");
        }

        private static void AppendAEABIAlignAttr(string name, bool preserved, byte[] data, ref int offset, int endOffset, StringBuilder sb)
        {
            int val = ReadAEABIUleb128(data, ref offset, endOffset);
            string text;
            if (val == 0)
            {
                text = "None";
            }
            else if (val == 1)
            {
                text = preserved ? "8-byte, except leaf SP" : "8-byte";
            }
            else if (val == 2)
            {
                text = preserved ? "8-byte" : "4-byte";
            }
            else if (val == 3)
            {
                text = "??? 3";
            }
            else if (val <= 12)
            {
                text = preserved
                    ? $"8-byte and up to {1 << val}-byte extended, except leaf SP"
                    : $"8-byte and up to {1 << val}-byte extended";
            }
            else
            {
                text = $"??? ({val})";
            }
            sb.AppendLine(CultureInfo.InvariantCulture, $"  {name}: {text}");
        }

        private static void AppendAEABICompatibilityAttr(byte[] data, ref int offset, int endOffset, StringBuilder sb)
        {
            int flag = ReadAEABIUleb128(data, ref offset, endOffset);
            string vendor = ELFParserUtils.ExtractStringFromBytes(data, offset);
            offset += vendor.Length + 1;
            string line = flag == 0
                ? "  Tag_compatibility: No"
                : string.Create(CultureInfo.InvariantCulture, $"  Tag_compatibility: flag = {flag}, vendor = {vendor}");
            sb.AppendLine(line);
        }

        private static void AppendAEABIAlsoCompatibleAttr(byte[] data, ref int offset, int endOffset, StringBuilder sb)
        {
            int innerTag = ReadAEABIUleb128(data, ref offset, endOffset);
            string text;
            if (innerTag == 6) // Tag_CPU_arch
            {
                int val = ReadAEABIUleb128(data, ref offset, endOffset);
                text = val >= 0 && val < s_aeabiCpuArch.Length ? s_aeabiCpuArch[val] : $"??? ({val})";
            }
            else if (innerTag == 0)
            {
                text = "None";
                if (offset < endOffset && data[offset] == 0)
                {
                    offset++; // 跳过空字符串终止符
                }
            }
            else
            {
                string s = ELFParserUtils.ExtractStringFromBytes(data, offset);
                offset += s.Length + 1;
                text = s;
            }
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Tag_also_compatible_with: {text}");
        }

        private static void ParseAEABIFileAttributes(byte[] data, ref int offset, int endOffset, StringBuilder sb)
        {
            while (offset < endOffset)
            {
                int tag = ReadAEABIUleb128(data, ref offset, endOffset);

                switch (tag)
                {
                    case 4: // Tag_CPU_raw_name
                    case 5: // Tag_CPU_name
                    case 67: // Tag_conformance
                        AppendAEABIStringAttr(GetAEABITagName(tag), data, ref offset, sb);
                        break;
                    case 7: // Tag_CPU_arch_profile
                        AppendAEABIProfileAttr(data, ref offset, endOffset, sb);
                        break;
                    case 24: // Tag_ABI_align_needed
                        AppendAEABIAlignAttr("Tag_ABI_align_needed", false, data, ref offset, endOffset, sb);
                        break;
                    case 25: // Tag_ABI_align_preserved
                        AppendAEABIAlignAttr("Tag_ABI_align_preserved", true, data, ref offset, endOffset, sb);
                        break;
                    case 32: // Tag_compatibility: ULEB128 + null 终止字符串
                        AppendAEABICompatibilityAttr(data, ref offset, endOffset, sb);
                        break;
                    case 64: // Tag_nodefaults: 取值需读取但忽略
                        _ = ReadAEABIUleb128(data, ref offset, endOffset);
                        sb.AppendLine("  Tag_nodefaults: True");
                        break;
                    case 65: // Tag_also_compatible_with
                        AppendAEABIAlsoCompatibleAttr(data, ref offset, endOffset, sb);
                        break;
                    default:
                        AppendAEABIIntAttr(tag, data, ref offset, endOffset, sb);
                        break;
                }
            }
        }

        private static void ParseAEABIAttributes(byte[] data, ref int offset, int endOffset, StringBuilder sb)
        {
            int limit = Math.Min(endOffset, data.Length);

            // 供应商子节内部为若干"子-子节"：作用域标签(1字节: Tag_File=1/Section=2/Symbol=3) + 长度(4字节)
            // 这里解析 Tag_File 作用域（编译器通常只生成该作用域，与 readelf 输出一致）
            if (offset + 5 > limit)
            {
                return;
            }

            byte scopeTag = data[offset];
            offset += 5; // 跳过作用域标签 + 长度字段

            if (scopeTag == 1) // Tag_File
            {
                sb.AppendLine("File Attributes");
                ParseAEABIFileAttributes(data, ref offset, limit, sb);
            }
        }

        // 通用取值规则（readelf display_tag_value）：奇数 tag→字符串，偶数 tag→ULEB128
        private static int FormatGenericTagValue(string name, int tag, byte[] data, int offset, int endOffset, StringBuilder sb)
        {
            if ((tag & 1) != 0)
            {
                string value = ELFParserUtils.ExtractStringFromBytes(data, offset);
                sb.AppendLine(CultureInfo.InvariantCulture, $"  {name}: \"{value}\"");
                return value.Length + 1;
            }

            int bytesRead = ReadULEB128(data, offset, endOffset, out int val);
            sb.AppendLine(CultureInfo.InvariantCulture, $"  {name}: {val} (0x{val:x})");
            return bytesRead;
        }

        // Tag_compatibility (32)：ULEB128 flag + null 终止的 vendor 字符串
        private static int AppendGnuCompatibility(byte[] data, int offset, int endOffset, StringBuilder sb)
        {
            int start = offset;
            offset += ReadULEB128(data, offset, endOffset, out int flag);
            string vendor = ELFParserUtils.ExtractStringFromBytes(data, offset);
            offset += vendor.Length + 1;
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Tag_compatibility: flag = {flag}, vendor = {vendor}");
            return offset - start;
        }

        // MIPS GNU 属性取值表（与 binutils display_mips_gnu_attribute 一致）
        private static readonly string[] s_mipsFpAbi = ["Hard or soft float", "Hard float (double precision)", "Hard float (single precision)", "Soft float", "Hard float (MIPS32r2 64-bit FPU 12 callee-saved)", "Hard float (32-bit CPU, Any FPU)", "Hard float (32-bit CPU, 64-bit FPU)", "Hard float compat (32-bit CPU, 64-bit FPU)"];
        private static readonly string[] s_mipsMsa = ["Any", "128-bit MSA"];

        // 返回消费字节数；返回 -1 表示该 tag 非 MIPS 专属，交给通用规则
        private static int TryFormatMipsAttr(int tag, byte[] data, int offset, int endOffset, StringBuilder sb)
        {
            switch (tag)
            {
                case 4: // Tag_GNU_MIPS_ABI_FP
                {
                    int bytesRead = ReadULEB128(data, offset, endOffset, out int val);
                    string text = val >= 0 && val < s_mipsFpAbi.Length ? s_mipsFpAbi[val] : $"??? ({val})";
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  Tag_GNU_MIPS_ABI_FP: {text}");
                    return bytesRead;
                }
                case 8: // Tag_GNU_MIPS_ABI_MSA
                {
                    int bytesRead = ReadULEB128(data, offset, endOffset, out int val);
                    string text = val >= 0 && val < s_mipsMsa.Length ? s_mipsMsa[val] : $"??? ({val})";
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  Tag_GNU_MIPS_ABI_MSA: {text}");
                    return bytesRead;
                }
                default:
                    return -1;
            }
        }

        // PowerPC GNU 属性取值（与 binutils display_power_gnu_attribute 一致）
        // 返回消费字节数；返回 -1 表示该 tag 非 PowerPC 专属，交给通用规则
        private static int TryFormatPowerAttr(int tag, byte[] data, int offset, int endOffset, StringBuilder sb)
        {
            switch (tag)
            {
                case 4: // Tag_GNU_Power_ABI_FP
                {
                    int bytesRead = ReadULEB128(data, offset, endOffset, out int val);
                    string fp = (val & 3) switch
                    {
                        0 => "unspecified hard/soft float",
                        1 => "hard float",
                        2 => "soft float",
                        _ => "single-precision hard float"
                    };
                    string ld = (val & 0xC) switch
                    {
                        0 => "unspecified long double",
                        4 => "128-bit IBM long double",
                        8 => "64-bit long double",
                        _ => "128-bit IEEE long double"
                    };
                    string prefix = val > 15 ? $"(0x{val:x}), " : "";
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  Tag_GNU_Power_ABI_FP: {prefix}{fp}, {ld}");
                    return bytesRead;
                }
                case 8: // Tag_GNU_Power_ABI_Vector
                {
                    int bytesRead = ReadULEB128(data, offset, endOffset, out int val);
                    string vec = (val & 3) switch
                    {
                        0 => "unspecified",
                        1 => "generic",
                        2 => "AltiVec",
                        _ => "SPE"
                    };
                    string prefix = val > 3 ? $"(0x{val:x}), " : "";
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  Tag_GNU_Power_ABI_Vector: {prefix}{vec}");
                    return bytesRead;
                }
                case 12: // Tag_GNU_Power_ABI_Struct_Return
                {
                    int bytesRead = ReadULEB128(data, offset, endOffset, out int val);
                    string sr = val switch
                    {
                        0 => "unspecified",
                        1 => "r3/r4",
                        2 => "memory",
                        _ => $"(0x{val:x})"
                    };
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  Tag_GNU_Power_ABI_Struct_Return: {sr}");
                    return bytesRead;
                }
                default:
                    return -1;
            }
        }

        // 通用 GNU 方言（vendor "gnu"）：作用域头 + (tag, value)*，按 e_machine 选用各架构取值表
        private static void ParseGnuAttributes(ELFParser parser, byte[] data, ref int offset, int endOffset, StringBuilder sb)
        {
            int limit = Math.Min(endOffset, data.Length);

            // 子-子节头：作用域标签(1字节: Tag_File=1/Section=2/Symbol=3) + 长度(uint32)
            if (offset + 5 > limit)
            {
                return;
            }

            byte scopeTag = data[offset];
            offset += 5; // 跳过作用域标签 + 长度字段
            if (scopeTag != 1) // 仅展开 Tag_File 作用域
            {
                return;
            }

            sb.AppendLine("File Attributes");
            ushort machine = parser.Header.e_machine;

            while (offset < limit)
            {
                offset += ReadULEB128(data, offset, limit, out int tag);
                if (tag == 0)
                {
                    break; // 结束 / 填充
                }

                if (tag == 32) // Tag_compatibility（唯一通用特殊标签）
                {
                    offset += AppendGnuCompatibility(data, offset, limit, sb);
                    continue;
                }

                int consumed = machine switch
                {
                    (ushort)EMachine.EM_MIPS => TryFormatMipsAttr(tag, data, offset, limit, sb),
                    (ushort)EMachine.EM_PPC or (ushort)EMachine.EM_PPC64 => TryFormatPowerAttr(tag, data, offset, limit, sb),
                    _ => -1
                };

                if (consumed < 0)
                {
                    // 未知/无专属表：按通用规则（奇数字符串、偶数 ULEB128），标签名 Tag_unknown_N
                    consumed = FormatGenericTagValue($"Tag_unknown_{tag}", tag, data, offset, limit, sb);
                }

                offset += consumed;
            }
        }

        // RISC-V 属性方言（vendor "riscv"，与 binutils display_riscv_attribute 一致）
        private static void ParseRiscvAttributes(byte[] data, ref int offset, int endOffset, StringBuilder sb)
        {
            int limit = Math.Min(endOffset, data.Length);

            if (offset + 5 > limit)
            {
                return;
            }

            byte scopeTag = data[offset];
            offset += 5; // 作用域标签 + 长度
            if (scopeTag != 1)
            {
                return;
            }

            sb.AppendLine("File Attributes");

            while (offset < limit)
            {
                offset += ReadULEB128(data, offset, limit, out int tag);
                if (tag == 0)
                {
                    break;
                }

                switch (tag)
                {
                    case 5: // Tag_RISCV_arch（字符串）
                    {
                        string value = ELFParserUtils.ExtractStringFromBytes(data, offset);
                        offset += value.Length + 1;
                        sb.AppendLine(CultureInfo.InvariantCulture, $"  Tag_RISCV_arch: \"{value}\"");
                        break;
                    }
                    case 4: // Tag_RISCV_stack_align
                    {
                        offset += ReadULEB128(data, offset, limit, out int val);
                        sb.AppendLine(CultureInfo.InvariantCulture, $"  Tag_RISCV_stack_align: {val}-bytes");
                        break;
                    }
                    case 6: // Tag_RISCV_unaligned_access
                    {
                        offset += ReadULEB128(data, offset, limit, out int val);
                        string text = val switch
                        {
                            0 => "No unaligned access",
                            1 => "Unaligned access",
                            _ => $"??? ({val})"
                        };
                        sb.AppendLine(CultureInfo.InvariantCulture, $"  Tag_RISCV_unaligned_access: {text}");
                        break;
                    }
                    case 8 or 10 or 12 or 14 or 16: // priv_spec / minor / revision / atomic_abi / x3_reg_usage
                    {
                        offset += ReadULEB128(data, offset, limit, out int val);
                        sb.AppendLine(CultureInfo.InvariantCulture, $"  {GetRiscvTagName(tag)}: {val}");
                        break;
                    }
                    default:
                        offset += FormatGenericTagValue($"Tag_unknown_{tag}", tag, data, offset, limit, sb);
                        break;
                }
            }
        }

        // 辅助函数：获取RISC-V标签的名称
        private static string GetRiscvTagName(int tag)
        {
            return tag switch
            {
                4 => "Tag_RISCV_stack_align",
                5 => "Tag_RISCV_arch",
                6 => "Tag_RISCV_unaligned_access",
                8 => "Tag_RISCV_priv_spec",
                10 => "Tag_RISCV_priv_spec_minor",
                12 => "Tag_RISCV_priv_spec_revision",
                14 => "Tag_RISCV_atomic_abi",
                16 => "Tag_RISCV_x3_reg_usage",
                _ => $"Tag_unknown_{tag}"
            };
        }

        // 读取 ULEB128（遇续位停止），返回消费的字节数；value 通过 out 返回
        private static int ReadULEB128(byte[] data, int offset, int endOffset, out int value)
        {
            value = 0;
            int shift = 0;
            int cur = offset;
            int max = Math.Min(endOffset, data.Length);

            while (cur < max)
            {
                byte b = data[cur++];
                value |= (b & 0x7f) << shift;
                if ((b & 0x80) == 0)
                {
                    break;
                }
                shift += 7;
            }

            return cur - offset;
        }
    }
}