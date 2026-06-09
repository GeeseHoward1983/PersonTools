using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Utils;
using PersonalTools.Enums;
using System.Globalization;
using System.Text;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFExidxInfo
    {
        internal static string GetFormattedExidxInfo(ELFParser parser)
        {
            StringBuilder sb = new();

            if (parser.SectionHeaders != null)
            {
                for (int i = 0; i < parser.SectionHeaders.Count; i++)
                {
                    if (parser.SectionHeaders[i].sh_type == (uint)SectionType.SHT_ARM_EXIDX)
                    {
                        string exidxInfo = ParseExidxSection(parser, (Models.ELFSectionHeader)parser.SectionHeaders[i]);
                        if (!string.IsNullOrEmpty(exidxInfo))
                        {
                            sb.AppendLine(exidxInfo);
                        }
                    }
                }
            }

            if (sb.Length == 0)
            {
                sb.AppendLine("There are no exception index entries in this file.");
            }

            return sb.ToString();
        }

        private static string ParseExidxSection(ELFParser parser, Models.ELFSectionHeader exidxSection)
        {
            StringBuilder sb = new();

            // 读取异常索引表的数据
            byte[] data = parser.CopySectionData(in exidxSection);
            bool isLittleEndian = parser.Header.IsLittleEndian();

            // ARM异常索引表由8字节(2个字)的条目组成
            int entryCount = (int)(exidxSection.sh_size / 8); // 8字节每条目

            for (int idx = 0; idx < entryCount; idx++)
            {
                int offset = idx * 8; // 每个条目占8字节

                // 第一个32位：相对该字地址的 prel31 函数偏移；第二个32位：展开信息
                int addrOffset = SignExtendPrel31(ELFParserUtils.ReadInt32(data, offset, isLittleEndian));
                int unwindInfo = ELFParserUtils.ReadInt32(data, offset + 4, isLittleEndian);

                // 函数绝对地址 = 该条目字所在虚拟地址 + prel31 偏移
                long absAddr = (long)exidxSection.sh_addr + offset + addrOffset;

                // 获取包含该地址的符号名（跳过 $a/$t/$d 等映射符号），格式化为 "0x... <name>"
                string symbolDesc = DescribeAddress(parser, (ulong)absAddr);

                // 根据展开信息判断是索引还是标记
                if (unwindInfo == 1) // 特殊值表示无法展开
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"{symbolDesc}: @0x{unwindInfo:x} (cantunwind)");
                }
                else if ((unwindInfo & 0x80000000) != 0) // Compact展开表 (bit 31 set)，指令内联在条目内
                {
                    AppendInlineCompact(unwindInfo, symbolDesc, sb);
                }
                else // 指向 .ARM.extab 节
                {
                    AppendExtabEntry(parser, exidxSection, offset, unwindInfo, symbolDesc, isLittleEndian, sb);
                }

                sb.AppendLine(); // 添加空行分隔
            }

            return sb.ToString();
        }

        // 内联 Compact 模型（条目 bit31==1）：高7位为模型索引，低24位为展开指令
        private static void AppendInlineCompact(int unwindInfo, string symbolDesc, StringBuilder sb)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"{symbolDesc}: @0x{unwindInfo:x}");
            int compactIndex = (unwindInfo >> 24) & 0x7F; // bits 30-24
            int instruction = unwindInfo & 0x00FFFFFF;     // 低24位是实际指令

            sb.AppendLine(CultureInfo.InvariantCulture, $"  Compact model index: {compactIndex}");
            byte[] bInstruction =
            [
                (byte)(instruction >> 16 & 0xFF),
                (byte)(instruction >> 8 & 0xFF),
                (byte)(instruction & 0xFF),
            ];
            ParseUnwindInstructions(bInstruction, 3, sb);
        }

        // 指向 .ARM.extab 的条目：通用模型(personality routine) 或 extab 内的 Compact 模型
        private static void AppendExtabEntry(ELFParser parser, Models.ELFSectionHeader exidxSection, int offset, int unwindInfo, string symbolDesc, bool isLittleEndian, StringBuilder sb)
        {
            // prel31 偏移相对该字地址
            int extabRel = SignExtendPrel31(unwindInfo);
            long extabVaddr = (long)exidxSection.sh_addr + offset + 4 + extabRel;
            sb.AppendLine(CultureInfo.InvariantCulture, $"{symbolDesc}: @0x{extabVaddr:x}");

            // 虚拟地址需转换为文件偏移再读取（vaddr 通常不等于文件偏移）；分隔空行由调用方统一追加
            long extabFileOff = VaddrToFileOffset(parser, (ulong)extabVaddr);
            if (extabFileOff < 0 || extabFileOff + 4 > parser.FileData.Length)
            {
                return;
            }
            unwindInfo = ELFParserUtils.ReadInt32(parser.FileData, (int)extabFileOff, isLittleEndian);

            if ((unwindInfo & 0x80000000) == 0)
            {
                // 通用模型：extab 首字 bit31==0，低31位为指向 personality routine 的 prel31 偏移
                int per = SignExtendPrel31(unwindInfo);
                long personalityAddr = extabVaddr + per;
                string perDesc = DescribeAddress(parser, (ulong)personalityAddr);
                sb.AppendLine(CultureInfo.InvariantCulture, $"  Personality routine: {perDesc}");
                return;
            }

            int compactIndex = (unwindInfo >> 24) & 0x7F;
            sb.AppendLine(CultureInfo.InvariantCulture, $"  Compact model index: {compactIndex}");

            byte[] bInstruction = BuildCompactInstructions(parser, unwindInfo, compactIndex, extabFileOff);
            ParseUnwindInstructions(bInstruction, bInstruction.Length, sb);
        }

        // Compact 模型：由 unwindInfo 及（compactIndex==1 时）后续 extab 字节装配出解码用指令序列
        private static byte[] BuildCompactInstructions(ELFParser parser, int unwindInfo, int compactIndex, long extabFileOff)
        {
            if (compactIndex != 1)
            {
                return
                [
                    (byte)(unwindInfo >> 16 & 0xFF),
                    (byte)(unwindInfo >> 8 & 0xFF),
                    (byte)(unwindInfo & 0xFF),
                ];
            }

            int remainDWords = (unwindInfo >> 16) & 0xFF;
            byte[] bInstruction = new byte[2 + remainDWords * 4];
            bInstruction[0] = (byte)(unwindInfo >> 8 & 0xFF);
            bInstruction[1] = (byte)(unwindInfo & 0xFF);

            for (int i = 0; i < remainDWords; i++)
            {
                extabFileOff += 4;
                if (extabFileOff + 4 > parser.FileData.Length)
                {
                    break;
                }
                Array.Copy(parser.FileData, extabFileOff, bInstruction, 2 + i * 4, 4);
                if (parser.Header.IsLittleEndian())
                {
                    Array.Reverse(bInstruction, 2 + i * 4, 4);
                }
            }

            return bInstruction;
        }

        // prel31：31 位有符号偏移，按 bit30 符号扩展为完整 int
        private static int SignExtendPrel31(int value)
        {
            value &= 0x7FFFFFFF;
            if ((value & 0x40000000) != 0)
            {
                value |= unchecked((int)0x80000000);
            }
            return value;
        }

        // 虚拟地址 → 文件偏移（遍历节头，跳过 NOBITS/无地址节）
        private static long VaddrToFileOffset(ELFParser parser, ulong vaddr)
        {
            if (parser.SectionHeaders == null)
            {
                return -1;
            }

            foreach (Models.ELFSectionHeader section in parser.SectionHeaders)
            {
                if (section.sh_addr == 0 || section.sh_size == 0 || section.sh_type == (uint)SectionType.SHT_NOBITS)
                {
                    continue;
                }
                if (vaddr >= section.sh_addr && vaddr < section.sh_addr + section.sh_size)
                {
                    return (long)(section.sh_offset + (vaddr - section.sh_addr));
                }
            }
            return -1;
        }

        // 地址 → "0x..." 或 "0x... <符号名>"（按包含该地址的命名符号解析）
        private static string DescribeAddress(ELFParser parser, ulong address)
        {
            string name = FindContainingSymbolName(parser, address);
            return string.IsNullOrEmpty(name) ? $"0x{address:x}" : $"0x{address:x} <{name}>";
        }

        // 查找包含/最接近某地址的命名符号（用于 personality routine 显示 <name+0xoff>）
        private static string FindContainingSymbolName(ELFParser parser, ulong address)
        {
            if (parser.Symbols == null)
            {
                return string.Empty;
            }

            bool found = false;
            ulong bestStart = 0;
            string bestName = string.Empty;

            foreach (KeyValuePair<SectionType, List<ELFSymbol>> symbolList in parser.Symbols)
            {
                for (int symbolIndex = 0; symbolIndex < symbolList.Value.Count; symbolIndex++)
                {
                    ELFSymbol symbol = symbolList.Value[symbolIndex];
                    if (!IsAddressInSymbol(symbol, address) || (found && symbol.StValue <= bestStart))
                    {
                        continue;
                    }

                    string name = ELFSymbolNameResolver.GetSymbolName(parser, symbol, symbolList.Key, symbolIndex);
                    // 跳过 ARM 映射符号（$a/$t/$d/$x 等），与 readelf 一致
                    if (string.IsNullOrEmpty(name) || name[0] == '$')
                    {
                        continue;
                    }

                    bestStart = symbol.StValue;
                    bestName = name;
                    found = true;
                }
            }

            if (!found)
            {
                return string.Empty;
            }

            ulong delta = address - bestStart;
            return delta == 0 ? bestName : $"{bestName}+0x{delta:x}";
        }

        // 判断地址是否落在该符号范围内（已定义、FUNC/OBJECT/NOTYPE 类型、StValue<=addr<StValue+StSize）
        private static bool IsAddressInSymbol(ELFSymbol symbol, ulong address)
        {
            if (symbol.StShndx == 0)
            {
                return false; // 未定义符号
            }

            byte type = (byte)(symbol.StInfo & 0x0F);
            if (type is not ((byte)SymbolType.STT_FUNC) and not ((byte)SymbolType.STT_OBJECT) and not ((byte)SymbolType.STT_NOTYPE))
            {
                return false;
            }

            if (symbol.StValue > address)
            {
                return false; // 符号在目标地址之后
            }

            if (symbol.StSize != 0 && address >= symbol.StValue + symbol.StSize)
            {
                return false; // 超出符号范围
            }

            return true;
        }


        private static void ParseUnwindInstructions(byte[] instruction, int instructionLength, StringBuilder sb)
        {
            // 根据 ARM Exception Handling ABI (EHABI) 规范解析展开指令
            // 指令存储在低24位中
            int offset = 0;

            // 循环处理指令直到完成
            while (offset < instructionLength)
            {
                // 检查是否还有更多指令
                byte cmd = instruction[offset];

                // 检查是否为有效的单字节指令
                // 如果不是有效的单字节指令，尝试作为双字节指令处理
                if (IsValidSingleByteInstruction(cmd) || offset + 2 > instructionLength)
                {
                    sb.Append(CultureInfo.InvariantCulture, $"  0x{cmd:x2}");

                    // 处理具体的单字节指令
                    ProcessSingleByteInstruction(cmd, sb);

                    offset += 1; // 移动8位
                }
                else
                {
                    // 尝试作为双字节指令处理
                    ushort doubleCmd = (ushort)(instruction[offset] << 8 | instruction[offset + 1]);
                    sb.Append(CultureInfo.InvariantCulture, $"  0x{instruction[offset]:x2} 0x{instruction[offset + 1]:x2}");

                    // 处理双字节指令
                    ProcessDoubleByteInstruction(doubleCmd, sb);

                    offset += 2; // 移动16位
                }
            }
        }

        private static bool IsValidSingleByteInstruction(byte cmd)
        {
            return cmd switch
            {
                <= 0x7F => true,                        // 含 0x00 (nop) 及 0x01-0x7F
                >= 0x90 and <= 0xAF => true,            // pop {r4-rN, lr}
                0xB0 or (>= 0xB4 and <= 0xBF) => true,  // 0x80-0xFF: 以1开头的字节通常是单字节指令
                >= 0xC0 and <= 0xC5 => true,
                >= 0xCA and <= 0xFF => true,
                _ => false,                             // 其他情况暂不认定为有效单字节指令
            };
        }

        private static int CalcVsp(byte cmd)
        {
            int imm = cmd & 0x3F;
            return (imm + 1) << 2;
        }

        private static void ProcessSingleByteInstruction(byte cmd, StringBuilder sb)
        {
            if (cmd == 0xB0) // BX r14 (finish)
            {
                sb.AppendLine("  finish");
            }
            else if (cmd <= 0x3F) // vsp = vsp + (imm6 << 2) + 4
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"  vsp = vsp + {CalcVsp(cmd)}");
            }
            else if (cmd is >= 0x40 and <= 0x7F) // vsp = vsp - (imm6 << 2) - 4
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"  vsp = vsp - {CalcVsp(cmd)}");
            }
            else if ((cmd & 0x90) == 0x90)
            {
                int imm = cmd & 0x0F;
                if (imm is 0xC or 0xF)
                {
                    sb.AppendLine("  reserved");
                }
                else
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  vsp = r{imm}");
                }
            }
            else if (cmd is >= 0xA0 and <= 0xAF) // pop {r4-rN[, r14]}
            {
                AppendPopList(sb, cmd, "r", 4, includeLr: true);
            }
            else if (cmd == 0xB4)
            {
                sb.AppendLine("  pop");
            }
            else if (cmd == 0xB5)
            {
                sb.AppendLine("  pop vsp");
            }
            else if (cmd is 0xB6 or 0xB7 or (>= 0xCA and <= 0xCF) or (>= 0xD8 and <= 0xFF))
            {
                sb.AppendLine("  Spare");
            }
            else if (cmd is (>= 0xB8 and <= 0xBF) or (>= 0xD0 and <= 0xD7)) // pop VFP D8-DN
            {
                AppendPopList(sb, cmd, "D", 8, includeLr: false);
            }
            else if (cmd is >= 0xC0 and <= 0xC5) // pop wR10-wRN
            {
                AppendPopList(sb, cmd, "wR", 10, includeLr: false);
            }
            else
            {
                sb.AppendLine("  unknown command");
            }
        }

        // 追加一条 "pop {prefix<base>..prefix<base+n>[, r14]}" 指令（regMask = cmd 低3位）
        private static void AppendPopList(StringBuilder sb, byte cmd, string prefix, int baseReg, bool includeLr)
        {
            List<string> regs = [];
            int regMask = cmd & 0x07;
            for (int i = 0; i <= regMask; i++)
            {
                regs.Add($"{prefix}{i + baseReg}");
            }
            if (includeLr && (cmd & 0x08) == 0x08)
            {
                regs.Add("r14"); // LR
            }

            AppendPopRegs(sb, regs);
        }

        // 寄存器列表非空时追加 "pop {...}"（双字节指令解码中重复使用）
        private static void AppendPopRegs(StringBuilder sb, List<string> regs)
        {
            if (regs.Count > 0)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"  pop {{{ConvertUtils.EnumerableToString(", ", regs)}}}");
            }
        }



        private static void ProcessDoubleByteInstruction(ushort cmd, StringBuilder sb)
        {
            if (cmd == 0x8000)
            {
                sb.AppendLine("  Refuse to unwind");
            }
            else if (cmd is >= 0x8001 and <= 0x8FFF) // pop {r4-r15} 位掩码
            {
                AppendMaskedRegs(sb, cmd & 0xFFF, 12, "r", 4);
            }
            else if (cmd is 0xB100 or (>= 0xB110 and <= 0xB1FF) or 0xC700 or (>= 0xC710 and <= 0xC7FF))
            {
                sb.AppendLine("  Spare");
            }
            else if (cmd is >= 0xB101 and <= 0xB10F) // pop {r0-r3} 位掩码
            {
                AppendMaskedRegs(sb, cmd & 0x0F, 4, "r", 0);
            }
            else if (cmd is >= 0xB200 and <= 0xB2FF)
            {
                int uleb128 = cmd & 0xFF;
                int vsp = 0x204 + (uleb128 << 2);
                sb.AppendLine(CultureInfo.InvariantCulture, $" vsp = vsp + {vsp}");
            }
            else if (cmd is (>= 0xB300 and <= 0xB3FF) or (>= 0xC900 and <= 0xC9FF) or (>= 0xC800 and <= 0xC8FF)) // pop VFP D 范围
            {
                int idx = cmd is >= 0xC800 and <= 0xC8FF ? 16 : 0;
                int ssss = (cmd >> 4) & 0x0F;
                int cccc = cmd & 0x0F;
                AppendRegRange(sb, "D", ssss + idx, cccc);
            }
            else if (cmd is >= 0xC600 and <= 0xC6FF) // pop wR 范围
            {
                int ssss = (cmd >> 4) & 0x0F;
                int cccc = cmd & 0x0F;
                AppendRegRange(sb, "wR", ssss, ssss + cccc);
            }
            else if (cmd is >= 0xC701 and <= 0xC70F) // pop wCGR 位掩码
            {
                AppendMaskedRegs(sb, cmd & 0x0F, 4, "wCGR", 0);
            }
            else
            {
                sb.AppendLine("  unknown double-byte command");
            }
        }

        // 按位掩码追加寄存器：bit i 置位 → "prefix{i+baseReg}"
        private static void AppendMaskedRegs(StringBuilder sb, int mask, int bitCount, string prefix, int baseReg)
        {
            List<string> regs = [];
            for (int i = 0; i < bitCount; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    regs.Add($"{prefix}{i + baseReg}");
                }
            }
            AppendPopRegs(sb, regs);
        }

        // 追加连续寄存器范围："prefix{baseReg+0}".."prefix{baseReg+count}"
        private static void AppendRegRange(StringBuilder sb, string prefix, int baseReg, int count)
        {
            List<string> regs = [];
            for (int i = 0; i <= count; i++)
            {
                regs.Add($"{prefix}{baseReg + i}");
            }
            AppendPopRegs(sb, regs);
        }
    }
}