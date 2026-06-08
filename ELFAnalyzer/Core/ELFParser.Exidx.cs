using PersonalTools.ELFAnalyzer.Models;
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

                // 获取包含该地址的符号名（跳过 $a/$t/$d 等映射符号）
                string symbolName = FindContainingSymbolName(parser, (ulong)absAddr);
                string symbolDesc = string.IsNullOrEmpty(symbolName) ? $"0x{absAddr:x}" : $"0x{absAddr:x} <{symbolName}>";

                // 根据展开信息判断是索引还是标记
                if (unwindInfo == 1) // 特殊值表示无法展开
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"{symbolDesc}: @0x{unwindInfo:x} (cantunwind)");
                }
                else if ((unwindInfo & 0x80000000) != 0) // Compact展开表 (bit 31 set)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"{symbolDesc}: @0x{unwindInfo:x}");
                    // Compact model index 是高7位 (bits 30-24)
                    int compactIndex = (unwindInfo >> 24) & 0x7F;
                    int instruction = unwindInfo & 0x00FFFFFF; // 低24位是实际指令

                    sb.AppendLine(CultureInfo.InvariantCulture, $"  Compact model index: {compactIndex}");
                    byte[] bInstruction =
                    [
                        (byte)(instruction >> 16 & 0xFF),
                        (byte)(instruction >> 8 & 0xFF),
                        (byte)(instruction & 0xFF),
                    ];
                    // 解析实际的展开指令
                    ParseUnwindInstructions(bInstruction, 3, sb);
                }
                else
                {
                    // 展开表条目索引 - 指向 .ARM.extab 节，prel31 偏移相对该字地址
                    int extabRel = SignExtendPrel31(unwindInfo);
                    long extabVaddr = (long)exidxSection.sh_addr + offset + 4 + extabRel;
                    sb.AppendLine(CultureInfo.InvariantCulture, $"{symbolDesc}: @0x{extabVaddr:x}");

                    // 虚拟地址需转换为文件偏移再读取（vaddr 通常不等于文件偏移）
                    long extabFileOff = VaddrToFileOffset(parser, (ulong)extabVaddr);
                    if (extabFileOff < 0 || extabFileOff + 4 > parser.FileData.Length)
                    {
                        sb.AppendLine();
                        continue;
                    }
                    unwindInfo = ELFParserUtils.ReadInt32(parser.FileData, (int)extabFileOff, isLittleEndian);

                    if ((unwindInfo & 0x80000000) == 0)
                    {
                        // 通用模型：extab 首字 bit31==0，低31位为指向 personality routine 的 prel31 偏移
                        int per = SignExtendPrel31(unwindInfo);
                        long personalityAddr = extabVaddr + per;
                        string perName = FindContainingSymbolName(parser, (ulong)personalityAddr);
                        string perDesc = string.IsNullOrEmpty(perName) ? $"0x{personalityAddr:x}" : $"0x{personalityAddr:x} <{perName}>";
                        sb.AppendLine(CultureInfo.InvariantCulture, $"  Personality routine: {perDesc}");
                    }
                    else
                    {
                        int compactIndex = (unwindInfo >> 24) & 0x7F;

                        sb.AppendLine(CultureInfo.InvariantCulture, $"  Compact model index: {compactIndex}");
                        byte[] bInstruction;
                        if (compactIndex == 1)
                        {
                            int remainDWords = (unwindInfo >> 16) & 0xFF;
                            bInstruction = new byte[2 + remainDWords * 4];
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
                        }
                        else
                        {
                            bInstruction = new byte[3];
                            bInstruction[0] = (byte)(unwindInfo >> 16 & 0xFF);
                            bInstruction[1] = (byte)(unwindInfo >> 8 & 0xFF);
                            bInstruction[2] = (byte)(unwindInfo & 0xFF);
                        }
                        ParseUnwindInstructions(bInstruction, bInstruction.Length, sb);
                    }
                }

                sb.AppendLine(); // 添加空行分隔
            }

            return sb.ToString();
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
                    if (symbol.StShndx == 0)
                    {
                        continue; // 未定义符号
                    }

                    byte type = (byte)(symbol.StInfo & 0x0F);
                    if (type is not ((byte)SymbolType.STT_FUNC) and not ((byte)SymbolType.STT_OBJECT) and not ((byte)SymbolType.STT_NOTYPE))
                    {
                        continue;
                    }

                    if (symbol.StValue > address)
                    {
                        continue; // 符号在目标地址之后
                    }
                    if (symbol.StSize != 0 && address >= symbol.StValue + symbol.StSize)
                    {
                        continue; // 超出符号范围
                    }
                    if (found && symbol.StValue <= bestStart)
                    {
                        continue; // 已有更接近的符号
                    }

                    string name = SymbleName.GetSymbolName(parser, symbol, symbolList.Key, symbolIndex);
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
            if (cmd == 0x00) // nop
            {
                return true;
            }

            if (cmd <= 0x7F)
            {
                return true;
            }

            // 0x90-0xAF: pop {r4-rN, lr}
            if (cmd is >= 0x90 and <= 0xAF)
            {
                return true;
            }

            // 0x80-0xFF: 以1开头的字节通常是单字节指令
            if (cmd is 0xB0 or (>= 0xB4 and <= 0xBF))
            {
                return true;
            }

            if (cmd is >= 0xC0 and <= 0xC5)
            {
                return true;
            }

            if (cmd is >= 0xCA and <= 0xFF)
            {
                return true;
            }

            // 其他情况暂不认定为有效单字节指令
            return false;
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
                sb.AppendLine(CultureInfo.InvariantCulture, $"  pop {{{Utils.EnumerableToString(", ", regs)}}}");
            }
        }



        private static void ProcessDoubleByteInstruction(ushort cmd, StringBuilder sb)
        {
            // 检查是否是pop {register_list}指令 (0x8000-0x8FFF)
            if (cmd == 0x8000)
            {
                sb.AppendLine("  Refuse to unwind");
            }
            else if (cmd is >= 0x8001 and <= 0x8FFF)
            {
                int regMask = cmd & 0xFFF;
                List<string> regs = [];

                for (int i = 0; i < 12; i++)
                {
                    if ((regMask & (1 << i)) != 0)
                    {
                        regs.Add($"r{i + 4}");
                    }
                }

                AppendPopRegs(sb, regs);
            }
            else if (cmd is 0xB100 or (>= 0xB110 and <= 0xB1FF) or 0xC700 or (>= 0xC710 and <= 0xC7FF))
            {
                sb.AppendLine("  Spare");
            }
            else if (cmd is >= 0xB101 and <= 0xB10F)
            {
                List<string> regs = [];
                int regMask = cmd & 0x0F;
                for (int i = 0; regMask != 0; i++)
                {
                    if ((regMask & 1) == 1)
                    {
                        regs.Add($"r{i}");
                    }

                    regMask >>= 1;
                }
                AppendPopRegs(sb, regs);
            }
            else if (cmd is >= 0xB200 and <= 0xB2FF)
            {
                int uleb128 = cmd & 0xFF;
                int vsp = 0x204 + (uleb128 << 2);
                sb.AppendLine(CultureInfo.InvariantCulture, $" vsp = vsp + {vsp}");
            }
            else if (cmd is (>= 0xB300 and <= 0xB3FF) or (>= 0xC900 and <= 0xC9FF) or (>= 0xC800 and <= 0xC8FF))
            {
                int idx = 0;
                if (cmd is >= 0xC800 and <= 0xC8FF)
                {
                    idx = 16;
                }
                int ssss = (cmd >> 4) & 0x0F;
                int cccc = cmd & 0x0F;
                List<string> regs = [];
                for (int i = 0; i <= cccc; i++)
                {
                    regs.Add($"D{ssss + i + idx}");
                }
                AppendPopRegs(sb, regs);
            }
            else if (cmd is >= 0xC600 and <= 0xC6FF)
            {
                int ssss = (cmd >> 4) & 0x0F;
                int cccc = cmd & 0x0F;
                List<string> regs = [];
                for (int i = 0; i <= ssss + cccc; i++)
                {
                    regs.Add($"wR{ssss + i}");
                }
                AppendPopRegs(sb, regs);
            }
            else if (cmd is >= 0xC701 and <= 0xC70F)
            {
                List<string> regs = [];
                int regMask = cmd & 0x0F;
                for (int i = 0; regMask != 0; i++)
                {
                    if ((regMask & 1) == 1)
                    {
                        regs.Add($"wCGR{i}");
                    }

                    regMask >>= 1;
                }
                AppendPopRegs(sb, regs);
            }
            else
            {
                sb.AppendLine($"  unknown double-byte command");
            }
        }
    }
}