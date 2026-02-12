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
            byte[] data = new byte[exidxSection.sh_size];
            Array.Copy(parser.FileData, (long)exidxSection.sh_offset, data, 0, (int)exidxSection.sh_size);

            // ARM异常索引表由8字节(2个字)的条目组成
            int entryCount = (int)(exidxSection.sh_size / 8); // 8字节每条目

            for (int idx = 0; idx < entryCount; idx++)
            {
                int offset = idx * 8; // 每个条目占8字节

                // 每个条目包含两个32位值：
                // 第一个32位：相对于节起始地址的偏移
                // 第二个32位：展开信息(可能是索引或标记)
                int addrOffset;
                int unwindInfo;

                if (!parser.Header.IsLittleEndian())
                {
                    Array.Reverse(data, offset, 4);
                    Array.Reverse(data, offset + 4, 4);
                }
                addrOffset = BitConverter.ToInt32(data, offset);
                unwindInfo = BitConverter.ToInt32(data, offset + 4);

                // 计算绝对地址
                int absAddr = (int)exidxSection.sh_addr + addrOffset * 2 / 2 + offset;

                // 获取可能的符号名称
                string symbolName = FindNearestSymbolName(parser, (ulong)absAddr, false);
                if (string.IsNullOrEmpty(symbolName))
                {
                    symbolName = FindNearestSymbolName(parser, (ulong)absAddr, true);
                }
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
                    // 展开表条目索引 - 指向 .ARM.extab 节的偏移
                    int extabOffset = (int)exidxSection.sh_addr + (int)((unwindInfo + 4) * 2) / 2 + offset;
                    sb.AppendLine(CultureInfo.InvariantCulture, $"{symbolDesc}: @0x{extabOffset:x}");
                    if (!parser.Header.IsLittleEndian())
                    {
                        Array.Reverse(parser.FileData, extabOffset, 4);
                    }
                    unwindInfo = BitConverter.ToInt32(parser.FileData, extabOffset);

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
                            extabOffset += 4;
                            Array.Copy(parser.FileData, extabOffset, bInstruction, 2 + i * 4, 4);
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

                sb.AppendLine(); // 添加空行分隔
            }

            return sb.ToString();
        }

        private static string FindNearestSymbolName(ELFParser parser, ulong address, bool containstSize)
        {
            if (parser.Symbols == null)
            {
                return string.Empty;
            }

            // 遍历符号表，寻找最接近的符号
            foreach (KeyValuePair<SectionType, List<ELFSymbol>> symbolList in parser.Symbols)
            {
                foreach (ELFSymbol symbol in symbolList.Value)
                {
                    ulong pos = symbol.StValue;
                    if (containstSize)
                    {
                        pos += symbol.StSize;
                    }
                    // 查找地址最接近且不大于目标地址的符号
                    if ((pos == address || pos == address + 1) &&
                        symbol.StInfo != 0 && // 非NULL符号
                        symbol.StShndx != 0) // 非未定义符号
                    {
                        string name = SymbleName.GetSymbolName(parser, symbol, symbolList.Key);
                        if (containstSize && !string.IsNullOrEmpty(name))
                        {
                            name += $"+0x{symbol.StSize:x}";
                        }

                        if (!string.IsNullOrEmpty(name))
                        {
                            return name;
                        }
                    }
                }
            }

            return string.Empty;
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
            switch (cmd)
            {
                case 0xB0: // BX r14 (finish)
                    sb.AppendLine("  finish");
                    break;
                default:
                    // vsp = vsp - (imm4 + 1) << 2指令 (0x00-0x3F)
                    if (cmd <= 0x3F)
                    {
                        sb.AppendLine(CultureInfo.InvariantCulture, $"  vsp = vsp + {CalcVsp(cmd)}");
                    }
                    // vsp = vsp - (imm4 - 1) << 2指令 (0x40-0x7F)
                    else if (cmd is >= 0x40 and <= 0x7F)
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
                    // 检查是否是pop {r4-rN, lr}指令 (0xA0-0xAF)
                    else if (cmd is >= 0xA0 and <= 0xAF)
                    {
                        List<string> regs = [];
                        int regMask = cmd & 0x07;
                        for (int i = 0; i <= regMask; i++)
                        {
                            regs.Add($"r{i + 4}");
                        }
                        if ((cmd & 0x08) == 0x08)
                        {
                            regs.Add("r14"); // LR register
                        }

                        if (regs.Count > 0)
                        {
                            sb.AppendLine(CultureInfo.InvariantCulture, $"  pop {{{Utils.EnumerableToString(", ", regs)}}}");
                        }
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
                    else if (cmd is (>= 0xB8 and <= 0xBF) or (>= 0xD0 and <= 0xD7))
                    {
                        List<string> regs = [];
                        int regMask = cmd & 0x07;
                        for (int i = 0; i <= regMask; i++)
                        {
                            regs.Add($"D{i + 8}");
                        }
                        if (regs.Count > 0)
                        {
                            sb.AppendLine(CultureInfo.InvariantCulture, $"  pop {{{Utils.EnumerableToString(", ", regs)}}}");
                        }
                    }
                    else if (cmd is >= 0xC0 and <= 0xC5)
                    {
                        List<string> regs = [];
                        int regMask = cmd & 0x07;
                        for (int i = 0; i <= regMask; i++)
                        {
                            regs.Add($"wR{i + 10}");
                        }
                        if (regs.Count > 0)
                        {
                            sb.AppendLine(CultureInfo.InvariantCulture, $"  pop {{{Utils.EnumerableToString(", ", regs)}}}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"  unknown command");
                    }
                    break;
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

                if (regs.Count > 0)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  pop {{{Utils.EnumerableToString(", ", regs)}}}");
                }
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
                if (regs.Count > 0)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  pop {{{Utils.EnumerableToString(", ", regs)}}}");
                }
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
                if (regs.Count > 0)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  pop {{{Utils.EnumerableToString(", ", regs)}}}");
                }
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
                if (regs.Count > 0)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  pop {{{Utils.EnumerableToString(", ", regs)}}}");
                }
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
                if (regs.Count > 0)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  pop {{{Utils.EnumerableToString(", ", regs)}}}");
                }
            }
            else
            {
                sb.AppendLine($"  unknown double-byte command");
            }
        }
    }
}