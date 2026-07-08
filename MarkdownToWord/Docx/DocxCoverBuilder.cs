using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;
using PersonalTools.MarkdownToWord.Models;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 由 Markdown 一级标题生成 Word 封面：标题前空 8 行、初号、居中、固定样式（不可在界面配置）。
    /// 封面与正文之间的「插页」由 <see cref="DocxSectionBuilder"/> 的分节符完成（需求 1/5）。
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
            // 显式重置首行缩进，避免正文首行缩进>0 时封面标题右移失中（ind 须在 jc 前）
            Paragraph title = new(new ParagraphProperties(
                new Indentation { FirstLine = "0", FirstLineChars = 0 },
                new Justification { Val = JustificationValues.Center }));
            DocxInlineRenderer.RenderInlines(h1.Inline, title, DocxRunStyle.For(CoverStyle), ctx);
            container.AppendChild(title);
        }
    }
}
