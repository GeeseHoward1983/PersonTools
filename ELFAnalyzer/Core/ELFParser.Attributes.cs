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
            if (offset < data.Length && data[offset] == 0x41) // 'A'
            {
                offset++;

                while (offset < data.Length)
                {
                    // 解析子节长度 (4字节整数)
                    uint subSectionLength = ELFParserUtils.ReadUInt32(data, offset, isLittleEndian);
                    offset += 4;

                    if (offset >= data.Length)
                    {
                        break;
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

                    // 继续解析属性内容直到达到子节末尾
                    // subSectionLength 是供应商名称之后的属性数据长度
                    int subSectionEnd = (int)(offset + subSectionLength); // 修正：不再减去4，subSectionLength已经是剩余数据长度
                    if (subSectionEnd > data.Length)
                    {
                        subSectionEnd = data.Length;
                    }

                    if (vendorName == "aeabi")
                    {
                        ParseAEABIAttributes(data, ref offset, subSectionEnd, sb);
                    }
                    else if (vendorName.Contains("gnu", StringComparison.CurrentCulture) && parser.Header.e_machine == (ushort)EMachine.EM_MIPS)
                    {
                        // 专门处理GNU属性
                        ParseMipsGNUAttributes(parser, data, ref offset, subSectionEnd, sb);
                    }
                    else
                    {
                        ParseGenericAttributes(data, ref offset, subSectionEnd, sb);
                    }

                    // 确保offset不会倒退
                    if (offset < subSectionEnd)
                    {
                        offset = subSectionEnd;
                    }
                }
            }

            return sb.ToString();
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

        private static int DealWithSingleByteAttribute(byte[] data, int offset, StringBuilder sb, int tag)
        {
            if (offset < data.Length)
            {
                byte value = data[offset];
                sb.AppendLine(CultureInfo.InvariantCulture, $"  {GetTagName(tag)}: {value}");
                return 1;
            }
            else
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"  {GetTagName(tag)}: <error reading value>");
                return 0;
            }
        }

        private static int DealWithNullTerminatedString(byte[] data, int offset, StringBuilder sb, int tag)
        {
            string value = ELFParserUtils.ExtractStringFromBytes(data, offset);
            sb.AppendLine(CultureInfo.InvariantCulture, $"  {GetTagName(tag)}: \"{value}\"");
            return value.Length + 1; // 包括null终止符
        }

        private static int DealWithLengthPrefixedValue(byte[] data, int offset, StringBuilder sb, int tag)
        {
            int bytesRead = ReadULEB128(data, offset, out int valueLen);
            if (bytesRead == 0)
            {
                return 0;
            }
            offset += bytesRead;
            if (offset + valueLen > data.Length)
            {
                sb.AppendLine($"  Error: Attribute value length exceeds data bounds");
                return bytesRead; // 只返回已读取的长度
            }
            string value = Encoding.UTF8.GetString(data, offset, valueLen);
            sb.AppendLine(CultureInfo.InvariantCulture, $"  {GetTagName(tag)}: {value}");
            return bytesRead + valueLen;
        }

        private static int DealWithFlagAttribute(StringBuilder sb, int tag)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"  {GetTagName(tag)}: Flag");
            return 0; // 标志类型没有值长度
        }

        private static void ParseGenericAttributes(byte[] data, ref int offset, int endOffset, StringBuilder sb)
        {
            // 根据供应商名称选择不同的处理方式
            while (offset < Math.Min(endOffset, data.Length))
            {
                int bytesRead = ReadULEB128(data, offset, out int attrTag);

                offset += bytesRead;

                if (attrTag == 0)
                {
                    break; // 标签序列结束
                }
                offset += attrTag switch
                {
                    2 or 32 or 65 or 70 or 73 or 76 or 77 or 78 or 79 or 80 or 81 => DealWithFlagAttribute(sb, attrTag),
                    1 or 3 or 4 or 66 or 68 => DealWithNullTerminatedString(data, offset, sb, attrTag),
                    5 or 6 or 7 or 8 or 9 or 10 or 11 or 12 or 16 or 17 or 18 or 19 or 20 or 21 or 22 or 23 or 24 or 25 or 26 or 27 or 28 or 34 or 36 or 38 or 39 or 40 or 42 or 69 or 71 or 72 or 74 or 75 => DealWithSingleByteAttribute(data, offset, sb, attrTag),
                    _ => DealWithLengthPrefixedValue(data, offset, sb, attrTag)
                };
            }
        }

        // 专门处理GNU属性的函数
        private static void ParseMipsGNUAttributes(ELFParser parser, byte[] data, ref int offset, int endOffset, StringBuilder sb)
        {
            // 按照readelf的输出格式，首先添加"File Attributes"标题
            sb.AppendLine("  File Attributes");

            while (offset < endOffset)
            {
                int attrTag = data[offset++];  // 读取一个字节作为标签类型

                if (attrTag == 0)
                {
                    break; // 标签序列结束
                }

                // 根据标签类型处理
                switch (attrTag)
                {
                    case 1: // Tag_File - 这种情况下需要读取长度
                        {
                            int bytesRead = ReadULEB128(data, offset, out _);
                            if (bytesRead == 0)
                            {
                                break;
                            }

                            offset += bytesRead;
                        }
                        break;

                    case 3: // Tag_GNU_ABI_TAG
                        if (offset + 4 <= endOffset)  // 需要至少4字节
                        {
                            // 读取4个字节的版本信息
                            int osNum = ELFParserUtils.ReadInt32(data, offset, parser.Header.IsLittleEndian());

                            string osName = osNum switch
                            {
                                0 => "linux",
                                1 => "hurd",
                                2 => "unix",
                                3 => "standalone",
                                _ => $"unknown({osNum})"
                            };

                            sb.AppendLine(CultureInfo.InvariantCulture, $"    Tag_GNU_ABI_TAG: {osName}");
                            offset += 4; // 跳过4字节数据
                        }
                        break;

                    case 4: // Tag_GNU_MIPS_ABI_FP (MIPS架构)
                        if (offset < endOffset)
                        {
                            int fpAbi = data[offset++] + 1;
                            string fpAbiDesc = fpAbi switch
                            {
                                0 => "任意浮点ABI",              // Val_GNU_MIPS_ABI_FP_ANY
                                1 => "硬浮点 (双精度)",          // Val_GNU_MIPS_ABI_FP_DOUBLE
                                2 => "硬浮点 (单精度)",          // Val_GNU_MIPS_ABI_FP_SINGLE
                                3 => "软浮点",                   // Val_GNU_MIPS_ABI_FP_SOFT
                                4 => "旧64位浮点",               // Val_GNU_MIPS_ABI_FP_OLD_64
                                5 => "未知浮点扩展",             // Val_GNU_MIPS_ABI_FP_XX
                                6 => "64位浮点",                 // Val_GNU_MIPS_ABI_FP_64
                                7 => "64位浮点增强",             // Val_GNU_MIPS_ABI_FP_64A
                                _ => $"未知({fpAbi})"
                            };

                            sb.AppendLine(CultureInfo.InvariantCulture, $"    Tag_GNU_MIPS_ABI_FP: {fpAbiDesc}");
                        }
                        break;

                    case 8: // Tag_GNU_MIPS_ABI_MSA (MIPS架构)
                        if (offset < endOffset)
                        {
                            int msaAbi = data[offset++];
                            string msaAbiDesc = msaAbi switch
                            {
                                0 => "任意MSA ABI",        // Val_GNU_MIPS_ABI_MSA_ANY
                                1 => "MSA 128-bit",       // Val_GNU_MIPS_ABI_MSA_128
                                _ => $"未知({msaAbi})"
                            };

                            sb.AppendLine(CultureInfo.InvariantCulture, $"    Tag_GNU_MIPS_ABI_MSA: {msaAbiDesc}");
                        }
                        break;

                    default:
                        // 对于未知标签，尝试读取长度并跳过对应数据
                        int unknownValueLen;
                        int unknownBytesRead = ReadULEB128(data, offset, out unknownValueLen);
                        if (unknownBytesRead > 0)
                        {
                            offset += unknownBytesRead;
                            offset += Math.Min(unknownValueLen, endOffset - offset);
                        }
                        break;
                }
            }
        }

        // 辅助函数：获取标签的名称
        private static string GetTagName(int tag)
        {
            return tag switch
            {
                1 => "Tag_File",
                2 => "Tag_Section",
                3 => "Tag_Symbol",
                4 => "Tag_CPU_name",
                5 => "Tag_CPU_arch",
                6 => "Tag_CPU_arch_profile",
                7 => "Tag_ARM_ISA_use",
                8 => "Tag_THUMB_ISA_use",
                9 => "Tag_FP_arch",
                10 => "Tag_WMMX_arch",
                11 => "Tag_Advanced_SIMD_arch",
                12 => "Tag_Virtualization_use",
                16 => "Tag_ABI_PCS_GOT_use",
                17 => "Tag_ABI_PCS_wchar_t",
                18 => "Tag_ABI_FP_roundings",
                19 => "Tag_ABI_FP_denormal",
                20 => "Tag_ABI_FP_exceptions",
                21 => "Tag_ABI_FP_number_model",
                22 => "Tag_ABI_align_needed",
                23 => "Tag_ABI_align_preserved",
                24 => "Tag_ABI_enum_size",
                25 => "Tag_ABI_HardFP_use",
                26 => "Tag_ABI_VFP_args",
                27 => "Tag_ABI_WMMX_args",
                28 => "Tag_ABI_optimization_goals",
                32 => "Tag_nodefaults",
                34 => "Tag_CPU_unaligned_access",
                36 => "Tag_FP_HP_extension",
                38 => "Tag_ABI_FP_16bit_format",
                39 => "Tag_DSP_extension",
                40 => "Tag_MVE_arch",
                42 => "Tag_also_compatible_el",
                65 => "Tag_also_compatible_at",
                66 => "Tag_also_compatible_with_1",
                67 => "Tag_conformance_1",
                68 => "Tag_conformance_2",
                69 => "Tag_CPU_arch_2",
                70 => "Tag_T2EE_use",
                71 => "Tag_also_compatible_with_2",
                72 => "Tag_conformance_3",
                74 => "Tag_MPextension_use",
                75 => "Tag_DIV_use",
                76 => "Tag_FP_HP_extension_2",
                77 => "Tag_IDIV_use",
                78 => "Tag_VEC_use",
                79 => "Tag_DSP_use",
                80 => "Tag_MVE_arch_2",
                81 => "Tag_MVE_use",
                _ => $"Tag_{tag}"
            };
        }

        private static int ReadULEB128(byte[] data, int offset, out int value)
        {
            value = 0;
            int shift = 0;
            int currentOffset = offset;

            while (currentOffset < data.Length && currentOffset - offset < 4)
            {
                byte b = data[currentOffset++];
                value |= (b & 0x7f) << shift;
                shift += 8;
            }

            return currentOffset - offset;
        }
    }
}