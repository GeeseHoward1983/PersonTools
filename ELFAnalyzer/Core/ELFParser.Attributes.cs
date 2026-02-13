using PersonalTools.Enums;
using PersonalTools.PEAnalyzer.Parsers;
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
            byte[] data = new byte[section.sh_size];
            Array.Copy(parser.FileData, (long)section.sh_offset, data, 0, (int)section.sh_size);

            int offset = 0;

            // 解析属性段格式版本 (固定为'A' = 0x41)
            if (offset < data.Length && data[offset] == 0x41) // 'A'
            {
                offset++;

                while (offset < data.Length)
                {
                    // 解析子节长度 (4字节整数)
                    if (!parser.Header.IsLittleEndian()) // 如果不是小端序
                    {
                        Array.Reverse(data, offset, 4);
                    }
                    uint subSectionLength = BitConverter.ToUInt32(data, offset);
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
                        sb.Append(ParseAEABIAttributes(data, ref offset, subSectionEnd));
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

        private static string GetAttributeNameByTag(int tag)
        {
            return tag switch
            {
                5 => "Tag_CPU_name",
                6 => "Tag_CPU_arch",
                7 => "Tag_CPU_arch_profile",
                8 => "Tag_ARM_ISA_use",
                9 => "Tag_THUMB_ISA_use",
                10 => "Tag_FP_arch",
                11 => "Tag_WMMX_arch",
                12 => "Tag_Advanced_SIMD_arch",
                13 => "Tag_PCS_config",
                14 => "Tag_PCS_R9_use",
                15 => "Tag_Unknown_15",
                16 => "Tag_Advanced_SIMD_arch",
                17 => "Tag_ABI_PCS_GOT_use",
                18 => "Tag_ABI_PCS_wchar_t",
                19 => "Tag_ABI_FP_rounding",
                20 => "Tag_ABI_FP_denormal",
                21 => "Tag_ABI_FP_exceptions",
                23 => "Tag_ABI_FP_number_model",
                24 => "Tag_ABI_align_needed",
                25 => "Tag_ABI_align_preserved",
                26 => "Tag_ABI_enum_size",
                27 => "Tag_ABI_HardFP_use",
                28 => "Tag_ABI_VFP_args",
                30 => "Tag_ABI_optimization_goals",
                34 => "Tag_CPU_unaligned_access",
                38 => "Tag_ABI_FP_16bit_format",
                68 => "Tag_Virtualization_use",
                _ => $"Unknown Tag {tag}"
            };
        }

        private static int DealWithAttrSingleByteAttribute(byte[] data, int offset, StringBuilder sb, int tag)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"  {GetAttributeNameByTag(tag)}: {ELFParserUtils.GetTypeName(typeof(ARMCPUArch), data[offset], "")}");
            return 1;
        }

        private static int DealWithAttrNullTerminatedString(byte[] data, int offset, StringBuilder sb, int tag)
        {
            string value = ELFParserUtils.ExtractStringFromBytes(data, offset);
            sb.AppendLine(CultureInfo.InvariantCulture, $"  {GetAttributeNameByTag(tag)}: \"{value}\"");
            return value.Length;
        }

        private static int GetAttributeValueNameByTag(int tag, byte[] data, int offset, StringBuilder sb)
        {
            return tag switch
            {
                6 or 7 or 8 or 9 or 10 or 11 or 12 or 16 or 17 or 18 or 19 or 20 or 21 or 23 or 24 or 25 or 26 or 27 or 28 or 34 or 38 or 68 => DealWithAttrSingleByteAttribute(data, offset, sb, tag),
                _ => DealWithAttrNullTerminatedString(data, offset, sb, tag) // 对于未知标签，暂时不处理
            };
        }

        private static string ParseAEABIAttributes(byte[] data, ref int offset, int endOffset)
        {
            StringBuilder sb = new();
            offset += 5; // 长度
            // 现在真正解析属性
            while (offset + 1 < Math.Min(endOffset, data.Length)) // 至少需要1个字节(tag) + 1个字节长度
            {
                int attrTag = data[offset++];

                if (attrTag < 5)
                {
                    continue; // 标签序列结束
                }

                // 对于AEABI，属性值的长度也在第一个字节中编码
                if (offset >= data.Length)
                {
                    break;
                }

                offset += GetAttributeValueNameByTag(attrTag, data, offset, sb);
            }
            return sb.Length > 0 ? "File Attributes:\n" + sb.ToString() : string.Empty;
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
                            if (!parser.Header.IsLittleEndian())
                            {
                                Array.Reverse(data, offset, 4);
                            }
                            int osNum = BitConverter.ToInt32(data, offset);

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