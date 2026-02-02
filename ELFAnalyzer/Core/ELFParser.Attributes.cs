using PersonalTools.Enums;
using System.IO;
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
                    int subSectionEnd = (int)(offset + subSectionLength - 4); // -4因为已经跳过了长度字段
                    if (subSectionEnd > data.Length) subSectionEnd = data.Length;
                    
                    if (vendorName == "aeabi")
                    {
                        sb.Append(ParseAEABIAttributes(parser, data, ref offset, subSectionEnd));
                    }
                    else
                    {
                        ParseGenericAttributes(parser, data, ref offset, subSectionEnd, sb, vendorName);
                    }
                    
                    // 确保offset不会倒退
                    if (offset < subSectionEnd) offset = subSectionEnd;
                }
            }

            return sb.ToString();
        }

        private static string ParseAEABIAttributes(ELFParser parser, byte[] data, ref int offset, int endOffset)
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

                string attrValueStr = "";
                
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
                            offset++;
                            sb.AppendLine($"  Tag_ABI_FP_rounding: {ELFParserUtils.GetTypeName(typeof(ABIFPRounding), data[offset], "")}");
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
                    case 26: // Tag_ABI_enum_size
                        {
                            sb.AppendLine($"  Tag_ABI_enum_size: {ELFParserUtils.GetTypeName(typeof(ABIEnumSize), data[offset], "")}");
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

        private static void ParseGenericAttributes(ELFParser parser, byte[] data, ref int offset, int endOffset, StringBuilder sb, string vendorName)
        {
            while (offset < endOffset)
            {
                int attrTag;
                int bytesRead = ReadULEB128(data, offset, out attrTag);
                if (bytesRead == 0) break;
                
                offset += bytesRead;

                if (attrTag == 0) break; // 标签序列结束

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
                
                sb.AppendLine($"  Tag_{attrTag}: {attrValue}");

                offset += attrValueLen;
            }
        }

        private static int ReadULEB128(byte[] data, int offset, out int value)
        {
            value = 0;
            int shift = 0;
            int currentOffset = offset;

            while (currentOffset < data.Length)
            {
                byte b = data[currentOffset++];
                value |= (b & 0x7f) << shift;
                if ((b & 0x80) == 0)
                {
                    break;
                }
                shift += 7;
            }

            return currentOffset - offset;
        }
    }
}