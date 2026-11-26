using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.Enums;

namespace PersonalTools.ELFAnalyzer.Core
{
    internal static class ELFInterpreterReader
    {
        internal static string GetInterpreterInfo(ELFParser parser)
        {
            return ReadInterpreter(parser, parser.ProgramHeaders?.FirstOrDefault(header => header.p_type == (uint)ProgramHeaderType.PT_INTERP) ?? default);
        }

        private static string ReadInterpreter(ELFParser parser, ELFProgramHeader header)
        {
            return header.p_type != 0 && header.p_offset < (ulong)parser.FileData.Length
                ? ELFParserUtils.ExtractStringFromBytes(parser.FileData, (int)header.p_offset, GetInterpreterLength(parser, header))
                : string.Empty;
        }

        private static int GetInterpreterLength(ELFParser parser, ELFProgramHeader header)
        {
            // 先夹紧 ulong 范围再转换 int，长度不得超过 PT_INTERP 的声明范围。
            return (int)Math.Min(header.p_filesz, (ulong)parser.FileData.Length - header.p_offset);
        }
    }
}
