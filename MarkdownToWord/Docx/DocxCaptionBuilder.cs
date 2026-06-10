using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using PersonalTools.MarkdownToWord.Models;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 生成图/表题注段落「图 N」「表 N」：用 Word <c>SEQ</c> 域自动编号，外包书签以便在 Word 中交叉引用（需求 10）。
    /// </summary>
    internal static class DocxCaptionBuilder
    {
        /// <summary>图题注（居中，位于图下方）。</summary>
        public static Paragraph BuildFigureCaption(string text, DocxRenderContext ctx) =>
            BuildCaption("Figure", "图", text, JustificationValues.Center, ctx);

        /// <summary>表题注（居中，位于表上方）。</summary>
        public static Paragraph BuildTableCaption(string text, DocxRenderContext ctx) =>
            BuildCaption("Table", "表", text, JustificationValues.Center, ctx);

        private static Paragraph BuildCaption(string seqName, string label, string text, JustificationValues justification, DocxRenderContext ctx)
        {
            ContentStyleRow body = ctx.Settings.For(ContentCategory.Body);
            DocxRunStyle style = DocxRunStyle.For(body);

            Paragraph paragraph = new(new ParagraphProperties(
                new KeepNext(),
                new Justification { Val = justification }));

            DocxInlineRenderer.AppendText(paragraph, label + " ", style);

            int bookmarkId = ctx.NextBookmarkId();
            string idText = bookmarkId.ToString(CultureInfo.InvariantCulture);
            paragraph.AppendChild(new BookmarkStart { Id = idText, Name = $"_Ref_{seqName}_{bookmarkId}" });
            AppendSeqField(paragraph, seqName, style);
            paragraph.AppendChild(new BookmarkEnd { Id = idText });

            if (!string.IsNullOrEmpty(text))
            {
                DocxInlineRenderer.AppendText(paragraph, " " + text, style);
            }

            return paragraph;
        }

        // SEQ 域：begin → instrText(" SEQ Figure \* ARABIC ") → separate → 占位"1" → end
        private static void AppendSeqField(OpenXmlElement parent, string seqName, DocxRunStyle style)
        {
            parent.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }));
            parent.AppendChild(new Run(new FieldCode($" SEQ {seqName} \\* ARABIC ")
            {
                Space = SpaceProcessingModeValues.Preserve,
            }));
            parent.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }));
            DocxInlineRenderer.AppendText(parent, "1", style);
            parent.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
        }
    }
}
