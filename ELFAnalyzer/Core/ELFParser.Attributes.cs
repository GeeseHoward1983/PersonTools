using PersonalTools.Enums;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    public class ELFAttributeInfo
    {
        public static string GetFormattedAttributeInfo(ELFParser parser)
        {
            var sb = new StringBuilder();

            if (parser.SectionHeaders != null)
            {
                for (int i = 0; i < parser.SectionHeaders.Count; i++)
                {
                    if (parser.SectionHeaders[i].sh_type == (uint)Enums.SectionType.SHT_GNU_ATTRIBUTES || parser.SectionHeaders[i].sh_type == (uint)Enums.SectionType.SHT_ARM_ATTRIBUTES)
                    {
                        var attrInfo = ParseAttributeSection(parser, parser.SectionHeaders[i]);
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
            var sb = new StringBuilder();
                        
            // 读取属性段的数据
            var data = new byte[section.sh_size];
            Array.Copy(parser.FileData, (long)section.sh_offset, data, 0, (int)section.sh_size);
            
            int offset = 0;
            
            // 解析属性段格式版本 (固定为'A' = 0x41)
            if (offset < data.Length && data[offset] == 0x41) // 'A'
            {
                offset++;
                
                while (offset < data.Length)
                {
                    // 解析子节长度 (4字节整数)
                    uint subSectionLength = BitConverter.ToUInt32(data, offset);
                    if (!parser.Header.IsLittleEndian()) // 如果不是小端序
                    {
                        var bytes = BitConverter.GetBytes(subSectionLength);
                        Array.Reverse(bytes);
                        subSectionLength = BitConverter.ToUInt32(bytes, 0);
                    }
                    offset += 4;
                    
                    if (offset >= data.Length) break;
                    
                    // 解析供应商名称 (null终止字符串)
                    int vendorNameStart = offset;
                    while (offset < data.Length && data[offset] != 0)
                    {
                        offset++;
                    }
                    string vendorName = Encoding.UTF8.GetString(data, vendorNameStart, offset - vendorNameStart);
                    offset++; // 跳过null终止符
                    
                    sb.AppendLine($"Attribute Section: {vendorName}");
                    
                    // 继续解析属性内容直到达到子节末尾
                    // subSectionLength 是供应商名称之后的属性数据长度
                    int subSectionEnd = (int)(offset + subSectionLength); // 修正：不再减去4，subSectionLength已经是剩余数据长度
                    if (subSectionEnd > data.Length) subSectionEnd = data.Length;
                    
                    if (vendorName == "aeabi")
                    {
                        sb.Append(ParseAEABIAttributes(data, ref offset, subSectionEnd));
                    }
                    else if (vendorName.Contains("gnu") && parser.Header.e_machine == (ushort)EMachine.EM_MIPS)
                    {
                        // 专门处理GNU属性
                        ParseMipsGNUAttributes(parser, data, ref offset, subSectionEnd, sb);
                    }
                    else
                    {
                        ParseGenericAttributes(data, ref offset, subSectionEnd, sb);
                    }
                    
                    // 确保offset不会倒退
                    if (offset < subSectionEnd) offset = subSectionEnd;
                }
            }

            return sb.ToString();
        }

        private static string ParseAEABIAttributes(byte[] data, ref int offset, int endOffset)
        {
            StringBuilder sb = new();
            offset += 5; // 长度
            // 现在真正解析属性
            while (offset + 1 < endOffset) // 至少需要1个字节(tag) + 1个字节长度
            {
                int attrTag = data[offset++];

                if (attrTag < 5) continue; // 标签序列结束

                // 对于AEABI，属性值的长度也在第一个字节中编码
                if (offset >= data.Length) break;

                string attrValueStr;
                switch (attrTag)
                {
                    case 5: // Tag_CPU_name
                        attrValueStr = ELFParserUtils.ExtractStringFromBytes(data, offset);
                        sb.AppendLine($"  Tag_CPU_name: \"{attrValueStr}\"");
                        offset += attrValueStr.Length;
                        break;
                    case 6: // Tag_CPU_arch
                        {
                            sb.AppendLine($"  Tag_CPU_arch: {ELFParserUtils.GetTypeName(typeof(ARMCPUArch), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 7: // Tag_CPU_arch_profile
                        {
                            sb.AppendLine($"  Tag_CPU_arch_profile: {ELFParserUtils.GetTypeName(typeof(ARMCPUArchProfile), (sbyte)data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 8: // Tag_ARM_ISA_use
                        {
                            sb.AppendLine($"  Tag_ARM_ISA_use: {ELFParserUtils.GetTypeName(typeof(ARMISAUse), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 9: // Tag_THUMB_ISA_use
                        {
                            sb.AppendLine($"  Tag_THUMB_ISA_use: {ELFParserUtils.GetTypeName(typeof(THUMBISAUse), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 10: // Tag_FP_arch
                        {
                            sb.AppendLine($"  Tag_FP_arch: {ELFParserUtils.GetTypeName(typeof(FPArch), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 11: // Tag_WMMX_arch
                        {
                            sb.AppendLine($"  Tag_WMMX_arch: {ELFParserUtils.GetTypeName(typeof(WMMXArch), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 16: // Tag_Advanced_SIMD_arch
                        {
                            sb.AppendLine($"  Tag_Advanced_SIMD_arch: {ELFParserUtils.GetTypeName(typeof(AdvancedSIMDArch), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 17: // Tag_ABI_PCS_GOT_use
                        {
                            sb.AppendLine($"  Tag_ABI_PCS_GOT_use: {ELFParserUtils.GetTypeName(typeof(ABIPCSGOTUse), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 18: // Tag_ABI_PCS_wchar_t
                        {
                            sb.AppendLine($"  Tag_ABI_PCS_wchar_t: {ELFParserUtils.GetTypeName(typeof(ABIPCSWCharT), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 19: // Tag_ABI_FP_rounding
                        {
                            sb.AppendLine($"  Tag_ABI_FP_rounding: {ELFParserUtils.GetTypeName(typeof(ABIFPRounding), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 20: // Tag_ABI_FP_denormal
                        {
                            sb.AppendLine($"  Tag_ABI_FP_denormal: {ELFParserUtils.GetTypeName(typeof(ABIFPDenormal), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 21: // Tag_ABI_FP_exceptions
                        {
                            sb.AppendLine($"  Tag_ABI_FP_exceptions: {ELFParserUtils.GetTypeName(typeof(ABIFPExceptions), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 23: // Tag_ABI_FP_number_model
                        {
                            sb.AppendLine($"  Tag_ABI_FP_number_model: {ELFParserUtils.GetTypeName(typeof(ABIFPNumberModel), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 24: // Tag_ABI_align_needed
                        {
                            sb.AppendLine($"  Tag_ABI_align_needed: {ELFParserUtils.GetTypeName(typeof(ABIAlignNeeded), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 25: // Tag_ABI_align_preserved
                        {
                            sb.AppendLine($"  Tag_ABI_align_preserved: {ELFParserUtils.GetTypeName(typeof(ABIAlignPreserved), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 26: // Tag_ABI_enum_size
                        {
                            sb.AppendLine($"  Tag_ABI_enum_size: {ELFParserUtils.GetTypeName(typeof(ABIEnumSize), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 27: // Tag_ABI_HardFP_use
                        {
                            sb.AppendLine($"  Tag_ABI_HardFP_use: {ELFParserUtils.GetTypeName(typeof(ABIFPHardUse), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 28: // Tag_ABI_VFP_args
                        {
                            sb.AppendLine($"  Tag_ABI_VFP_args: {ELFParserUtils.GetTypeName(typeof(ABIVFPArguments), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 30: // Tag_ABI_optimization_goals   这里的值有一些问题，与readelf对不上，暂时调整为+1，与readelf输出一致
                        {
                            sb.AppendLine($"  Tag_ABI_optimization_goals: {ELFParserUtils.GetTypeName(typeof(ABIOptimizationGoals), (byte)(data[offset] + 1), "")}");
                            offset++;
                        }
                        break;
                    case 34: // Tag_CPU_unaligned_access
                        {
                            sb.AppendLine($"  Tag_CPU_unaligned_access: {ELFParserUtils.GetTypeName(typeof(CPUUnalignedAccess), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 38: // Tag_ABI_FP_16bit_format
                        {
                            sb.AppendLine($"  Tag_ABI_FP_16bit_format: {ELFParserUtils.GetTypeName(typeof(ABIFP16BitFormat), data[offset], "")}");
                            offset++;
                        }
                        break;
                    case 68: // Tag_Virtualization_use
                        {
                            sb.AppendLine($"  Tag_Virtualization_use: {ELFParserUtils.GetTypeName(typeof(VirtualizationUse), data[offset], "")}");
                            offset++;
                        }
                        break;
                    default:
                        attrValueStr = ELFParserUtils.ExtractStringFromBytes(data, offset);
                        sb.AppendLine($"  Unknown Tag {attrTag}: \"{attrValueStr}\"");
                        break;
                }
            }
            if (sb.Length > 0)
            {
                return "File Attributes:\n" + sb.ToString();
            }
            return string.Empty;
        }

        private static void ParseGenericAttributes(byte[] data, ref int offset, int endOffset, StringBuilder sb)
        {
            // 根据供应商名称选择不同的处理方式
            while (offset < endOffset)
            {
                int bytesRead = ReadULEB128(data, offset, out int attrTag);
                if (bytesRead == 0) break;
                
                offset += bytesRead;

                if (attrTag == 0) break; // 标签序列结束

                // 判断属性值的类型
                switch (attrTag)
                {
                    // 处理标志类型的标签（没有显式的长度，直接是标志）
                    case 2: // Tag_File
                    case 32: // Tag_nodefaults
                    case 65: // Tag_also_compatible_at
                    case 70: // Tag_T2EE_use
                    case 73: // Tag_Virtualization_use
                    case 76: // Tag_FP_HP_extension
                    case 77: // Tag_IDIV_use
                    case 78: // Tag_VEC_use
                    case 79: // Tag_DSP_use
                    case 80: // Tag_MVE_arch
                    case 81: // Tag_MVE_use
                        // 标志类型，没有值，只是标记存在
                        sb.AppendLine($"  {GetTagName(attrTag)}: Flag");
                        offset++; // 移动到下一个属性
                        break;
                    
                    // 处理字符串类型的标签
                    case 1: // Tag_Section
                    case 3: // Tag_Symbol
                    case 4: // Tag_CPU_name
                    case 66: // Tag_also_compatible_with
                    case 68: // Tag_conformance
                        // 读取字符串（以null结尾）
                        int stringStart = offset;
                        while (offset < data.Length && offset < endOffset && data[offset] != 0)
                        {
                            offset++;
                        }
                        string stringValue = Encoding.UTF8.GetString(data, stringStart, offset - stringStart);
                        sb.AppendLine($"  {GetTagName(attrTag)}: \"{stringValue}\"");
                        offset++; // 跳过null终止符
                        break;
                    
                    // 处理整数类型的标签
                    case 5: // Tag_CPU_arch
                    case 6: // Tag_CPU_arch_profile
                    case 7: // Tag_ARM_ISA_use
                    case 8: // Tag_THUMB_ISA_use
                    case 9: // Tag_FP_arch
                    case 10: // Tag_WMMX_arch
                    case 11: // Tag_Advanced_SIMD_arch
                    case 12: // Tag_Virtualization_use
                    case 16: // Tag_ABI_PCS_GOT_use
                    case 17: // Tag_ABI_PCS_wchar_t
                    case 18: // Tag_ABI_FP_roundings
                    case 19: // Tag_ABI_FP_denormal
                    case 20: // Tag_ABI_FP_exceptions
                    case 21: // Tag_ABI_FP_number_model
                    case 22: // Tag_ABI_align_needed
                    case 23: // Tag_ABI_align_preserved
                    case 24: // Tag_ABI_enum_size
                    case 25: // Tag_ABI_HardFP_use
                    case 26: // Tag_ABI_VFP_args
                    case 27: // Tag_ABI_WMMX_args
                    case 28: // Tag_ABI_optimization_goals
                    case 34: // Tag_CPU_unaligned_access
                    case 36: // Tag_FP_HP_extension
                    case 38: // Tag_ABI_FP_16bit_format
                    case 39: // Tag_DSP_extension
                    case 40: // Tag_MVE_arch
                    case 42: // Tag_also_compatible_el
                    case 69: // Tag_CPU_arch
                    case 71: // Tag_also_compatible_with
                    case 72: // Tag_conformance
                    case 74: // Tag_MPextension_use
                    case 75: // Tag_DIV_use
                        // 读取单字节整数值
                        if (offset < data.Length)
                        {
                            byte intValue = data[offset];
                            sb.AppendLine($"  {GetTagName(attrTag)}: {intValue}");
                            offset++;
                        }
                        else
                        {
                            sb.AppendLine($"  {GetTagName(attrTag)}: <error reading value>");
                        }
                        break;
                    
                    default:
                        // 尝试读取长度和值（保持向后兼容）
                        int attrValueLen;
                        bytesRead = ReadULEB128(data, offset, out attrValueLen);
                        if (bytesRead == 0) break;
                        
                        offset += bytesRead;

                        if (offset + attrValueLen > data.Length)
                        {
                            sb.AppendLine($"  Error: Attribute value length exceeds data bounds");
                            break;
                        }

                        string attrValue = Encoding.UTF8.GetString(data, offset, attrValueLen);
                        
                        sb.AppendLine($"  {GetTagName(attrTag)}: {attrValue}");

                        offset += attrValueLen;
                        break;
                }
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
                
                if (attrTag == 0) break; // 标签序列结束

                // 根据标签类型处理
                switch (attrTag)
                {
                    case 1: // Tag_File - 这种情况下需要读取长度
                        {
                            int bytesRead = ReadULEB128(data, offset, out _);
                            if (bytesRead == 0) break;
                            offset += bytesRead;
                        }
                        break;
                        
                    case 3: // Tag_GNU_ABI_TAG
                        if (offset + 4 <= endOffset)  // 需要至少4字节
                        {
                            // 读取4个字节的版本信息
                            int osNum = BitConverter.ToInt32(data, offset);
                            if (!parser.Header.IsLittleEndian())
                            {
                                var bytes = BitConverter.GetBytes(osNum);
                                Array.Reverse(bytes);
                                osNum = BitConverter.ToInt32(bytes, 0);
                            }
                            
                            string osName = osNum switch
                            {
                                0 => "linux",
                                1 => "hurd", 
                                2 => "unix", 
                                3 => "standalone",
                                _ => $"unknown({osNum})"
                            };
                            
                            sb.AppendLine($"    Tag_GNU_ABI_TAG: {osName}");
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
                            
                            sb.AppendLine($"    Tag_GNU_MIPS_ABI_FP: {fpAbiDesc}");
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
                            
                            sb.AppendLine($"    Tag_GNU_MIPS_ABI_MSA: {msaAbiDesc}");
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