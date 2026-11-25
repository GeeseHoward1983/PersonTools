using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Parsers;
using PersonalTools.Utils;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// PE资源解析器版本信息变量解析辅助模块。
    /// </summary>
    internal static class PEResourceParserVersionVar
    {
        /// <summary>解析 VarFileInfo 部分，保留已经成功读取的版本信息。</summary>
        /// <param name="fs">文件流。</param>
        /// <param name="reader">二进制读取器。</param>
        /// <param name="peInfo">PE文件信息。</param>
        /// <param name="endPosition">版本信息结构的结束位置。</param>
        internal static void ParseVarFileInfo(FileStream fs, BinaryReader reader, PEInfo peInfo, long endPosition)
        {
            ParseSafely(new VarInfoReader(fs, reader, peInfo, endPosition).ParseFileInfo, "VarFileInfo");
        }

        /// <summary>循环解析 VarFileInfo 的 Var 子项。</summary>
        /// <param name="fs">文件流。</param>
        /// <param name="reader">二进制读取器。</param>
        /// <param name="peInfo">PE文件信息。</param>
        /// <param name="endPosition">Var 子块的结束位置。</param>
        internal static void ParseVar(FileStream fs, BinaryReader reader, PEInfo peInfo, long endPosition)
        {
            ParseSafely(new VarInfoReader(fs, reader, peInfo, endPosition).ParseVariables, "Var");
        }

        /// <summary>只记录可恢复的读取异常，不将异常细节写入展示字段。</summary>
        private static void ParseSafely(Action parse, string context)
        {
            try
            {
                parse();
            }
            catch (Exception ex) when (IsRecoverableReadError(ex))
            {
                AppLogger.Log($"{context}解析错误: {ex.Message}");
            }
        }

        private static bool IsRecoverableReadError(Exception exception)
        {
            return exception is IOException or UnauthorizedAccessException or ArgumentOutOfRangeException;
        }

        /// <summary>
        /// 单次解析的流、父块边界及当前节点头。子块使用独立读取器，避免覆盖父节点状态。
        /// </summary>
        private sealed class VarInfoReader(FileStream stream, BinaryReader reader, PEInfo peInfo, long endPosition)
        {
            private long startPosition;
            private ushort length;
            private ushort valueLength;

            public void ParseFileInfo()
            {
                if (TryReadFileInfoHeader())
                {
                    CompleteFileInfo();
                }
            }

            private void CompleteFileInfo()
            {
                ParseFileInfoChildren();
                VersionNodeReader.AdvanceToSibling(stream, startPosition, length, endPosition);
            }

            private bool LengthIsValid()
            {
                return length >= VersionNodeReader.NodeHeaderSize && startPosition + length <= stream.Length;
            }

            private bool TryReadFileInfoHeader()
            {
                return TryReadHeader() && LengthIsValid();
            }

            private bool TryReadHeader()
            {
                startPosition = stream.Position;
                return VersionNodeReader.TryReadNodeHeader(stream, reader, out length, out valueLength, out _);
            }

            private void ParseFileInfoChildren()
            {
                if (VersionNodeReader.ReadKey(stream, reader, length).Equals("VarFileInfo", StringComparison.OrdinalIgnoreCase))
                {
                    VersionNodeReader.ParseChildBlock(stream, startPosition, length, endPosition,
                        ParseChildVariables);
                }
            }

            private void ParseChildVariables(long childEnd)
            {
                ParseVar(stream, reader, peInfo, childEnd);
            }

            public void ParseVariables()
            {
                while (stream.Position < endPosition && TryParseNextVar())
                {
                }
            }

            private bool TryParseNextVar()
            {
                // 每项必须推进流位置；短路顺序同时保留头部截断及零长度节点的停止行为。
                return TryReadHeader() && HasNonzeroLength() && ParseSingleVar() && stream.Position > startPosition;
            }

            private bool HasNonzeroLength()
            {
                return length != 0 || StopAtEmptyNode();
            }

            private bool StopAtEmptyNode()
            {
                MoveToEmptyNodeBoundary(PEParserUtils.AlignTo4(startPosition));
                return false;
            }

            private void MoveToEmptyNodeBoundary(long position)
            {
                if (position < stream.Length)
                {
                    stream.Position = position;
                }
            }

            private bool ParseSingleVar()
            {
                // 必须先读取键名，再对齐值；内联键名读取会改变短路表达式的求值顺序。
                string name = VersionNodeReader.ReadKey(stream, reader, length);
                return TryMoveToValue() && CompleteVar(name);
            }

            private bool TryMoveToValue()
            {
                // 使用读取键名后的实际位置，避免截断或内嵌 NUL 导致值偏移错误。
                long position = PEParserUtils.AlignTo4(stream.Position);
                return IsPositionInBounds(position) && MoveTo(position);
            }

            private bool IsPositionInBounds(long position)
            {
                return position < Math.Min(stream.Length, endPosition);
            }

            private bool MoveTo(long position)
            {
                stream.Position = position;
                return true;
            }

            private bool CompleteVar(string name)
            {
                ApplyTranslationIfPresent(name);
                return TryMoveToSibling();
            }

            private bool PositionIsValid(long position)
            {
                return position > stream.Position && MoveTo(position);
            }

            private bool PositionCheck(long position)
            {
                return IsPositionInBounds(position) && PositionIsValid(position);
            }

            private bool TryMoveToSibling()
            {
                return PositionCheck(PEParserUtils.AlignTo4(startPosition + length));
            }

            private void ApplyTranslationIfPresent(string name)
            {
                if (IsTranslationValue(name))
                {
                    ApplyTranslation(reader.ReadBytes(valueLength));
                }
            }

            private bool TranslationValueCheck()
            {
                return valueLength >= sizeof(uint) && stream.Position + valueLength <= Math.Min(stream.Length, endPosition);
            }

            private bool IsTranslationValue(string name)
            {
                return name.Equals("Translation", StringComparison.OrdinalIgnoreCase) && TranslationValueCheck();
            }

            private void ApplyTranslation(byte[] bytes)
            {
                if (bytes.Length >= sizeof(uint))
                {
                    SetTranslation(BitConverter.ToUInt32(bytes, 0));
                }
            }

            private void SetTranslation(uint translation)
            {
                // 保持只展示第一组语言/代码页、但消耗完整声明值长度的行为。
                peInfo.AdditionalInfo.TranslationInfo = PEResourceParserVersionLanguage.GetReadableTranslationInfo(
                    translation & 0xFFFF, (translation >> 16) & 0xFFFF);
            }
        }
    }
}
