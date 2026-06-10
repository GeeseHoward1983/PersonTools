using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;
using PersonalTools.MarkdownToWord.Models;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 由 Markdown 一级标题生成 Word 封面：标题前空 8 行、初号、居中、固定样式（不可在界面配置），
    /// 标题后插页（分页符），使正文从新页开始（需求 1）。
    /// </summary>
    internal static class DocxCoverBuilder
    {
        private const int BlankLinesBeforeTitle = 8;

        // 固定封面样式：初号(42pt)、黑体、加粗、居中
        private static readonly ContentStyleRow CoverStyle = new(ContentCategory.Heading1)
        {
            ChineseFont = "黑体",
            WesternFont = "Times New Roman",
            FontSizeName = "初号",
            Bold = true,
        };

        public static void RenderCover(HeadingBlock h1, OpenXmlElement container, DocxRenderContext ctx)
        {
            for (int i = 0; i < BlankLinesBeforeTitle; i++)
            {
                container.AppendChild(new Paragraph());
            }

            DocxBlockRenderer.StripLeadingNumber(h1.Inline);
            Paragraph title = new(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
            DocxInlineRenderer.RenderInlines(h1.Inline, title, DocxRunStyle.For(CoverStyle), ctx);
            container.AppendChild(title);

            container.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
        }
    }
}
