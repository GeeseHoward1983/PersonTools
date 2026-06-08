using PersonalTools.PEAnalyzer.Parsers;
using System.IO;

namespace PersonalTools.PEAnalyzer.Resources
{
    /// <summary>
    /// VS_VERSIONINFO 子节点（VS_VERSION_INFO / StringFileInfo / StringTable / String /
    /// VarFileInfo / Var）共享的读取原语。
    /// 这些节点统一以 6 字节头(wLength/wValueLength/wType)开头、szKey 为 Unicode 串、
    /// 并按 4 字节对齐界定兄弟边界——此处单点封装，避免各解析器重复手写。
    /// </summary>
    internal static class VersionNodeReader
    {
        /// <summary>节点头大小（wLength + wValueLength + wType，各 2 字节）。</summary>
        public const int NodeHeaderSize = 6;

        /// <summary>
        /// 从当前位置读取节点头（6 字节）。数据不足返回 false 且不移动流位置；
        /// 成功时流位置前进 6 字节。
        /// </summary>
        public static bool TryReadNodeHeader(FileStream fs, BinaryReader reader, out ushort length, out ushort valueLength, out ushort type)
        {
            length = 0;
            valueLength = 0;
            type = 0;
            if (fs.Position + NodeHeaderSize > fs.Length)
            {
                return false;
            }

            length = reader.ReadUInt16();
            valueLength = reader.ReadUInt16();
            type = reader.ReadUInt16();
            return true;
        }

        /// <summary>
        /// 从当前位置读取节点的 szKey（Unicode），上界取 <paramref name="length"/> 与剩余文件长度的较小者。
        /// </summary>
        public static string ReadKey(FileStream fs, BinaryReader reader, ushort length)
        {
            int maxBytes = (int)Math.Min(length, (uint)(fs.Length - fs.Position));
            return Utilities.ReadUnicodeStringWithMaxBytes(reader, maxBytes);
        }

        /// <summary>
        /// 前进到对齐到 4 字节的下一个兄弟节点（位置 = AlignTo4(startPosition + length)）；
        /// 仅当目标落在 (当前位置, endPosition) 严格区间内时才移动。
        /// </summary>
        public static void AdvanceToSibling(FileStream fs, long startPosition, ushort length, long endPosition)
        {
            long nextPosition = Utilities.AlignTo4(startPosition + length);
            if (nextPosition < endPosition && nextPosition > fs.Position)
            {
                fs.Position = nextPosition;
            }
        }

        /// <summary>
        /// 进入当前节点的子块：以当前位置对齐到 4 字节作为子块起点，子块结束 = min(startPosition + length, endPosition)。
        /// 仅当起点落在文件内且小于子块结束时，定位到子块起点并以子块结束位置回调 <paramref name="parseChild"/>。
        /// </summary>
        public static void ParseChildBlock(FileStream fs, long startPosition, ushort length, long endPosition, Action<long> parseChild)
        {
            long childStart = Utilities.AlignTo4(fs.Position);
            long childEnd = Math.Min(startPosition + length, endPosition);
            if (childStart < fs.Length && childStart < childEnd)
            {
                fs.Position = childStart;
                parseChild(childEnd);
            }
        }
    }
}
