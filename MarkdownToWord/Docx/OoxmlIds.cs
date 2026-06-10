namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// OOXML 中跨部件引用的固定标识（样式 ID、编号 ID），集中定义避免各处魔法字符串漂移。
    /// </summary>
    internal static class OoxmlIds
    {
        public const string NormalStyleId = "Normal";

        // 标题用 Word 内置样式 ID（"Heading1".."Heading4"），以便 TOC 域与「插入交叉引用」能正确识别。
        // 目录条目沿用 Word 自动生成的内置 TOC 样式，本工程不自定义。
        public static string HeadingStyleId(int level) => "Heading" + level;

        // 多级标题编号：numbering.xml 中 abstractNum 的 ID 与 num 实例 ID
        public const int HeadingAbstractNumId = 0;
        public const int HeadingNumId = 1;
    }
}
