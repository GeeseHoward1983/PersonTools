namespace PersonalTools.MarkdownToWord.Models
{
    /// <summary>
    /// 可独立配置导出样式的内容类别。标题 1–4（对应 Word 输出的一~四级标题）、正文、表格各一行。
    /// 封面（由 Markdown 一级标题生成）与目录采用固定样式，不在此配置。
    /// </summary>
    internal enum ContentCategory
    {
        Heading1,
        Heading2,
        Heading3,
        Heading4,
        Body,
        Table,
    }
}
