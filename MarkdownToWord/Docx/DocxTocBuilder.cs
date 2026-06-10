using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using PersonalTools.MarkdownToWord.Models;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 生成 Word 原生「插入目录」结构：用 SDT(内容控件) 包裹 TOC 域（与 Word「引用→目录」一致），
    /// 使更新目录时不破坏其后的分节符/页码；目录条目沿用 Word 内置 TOC 样式（需求 2/4）。
    /// </summary>
    internal static class DocxTocBuilder
    {
        // 固定「目录」标题样式：三号、黑体、加粗、居中（不可配置）
        private static readonly ContentStyleRow TocTitleStyle = new(ContentCategory.Heading1)
        {
            ChineseFont = "黑体",
            WesternFont = "Times New Roman",
            FontSizeName = "三号",
            Bold = true,
        };

        /// <summary>构造目录 SDT；<paramref name="pageBreakBeforeTitle"/> 为真时目录另起一页（封面之后）。</summary>
        public static SdtBlock BuildToc(DocxStyleSettings settings, bool pageBreakBeforeTitle)
        {
            ParagraphProperties titlePr = new();
            if (pageBreakBeforeTitle)
            {
                titlePr.AppendChild(new PageBreakBefore());
            }

            titlePr.AppendChild(new Justification { Val = JustificationValues.Center });
            Paragraph title = new(titlePr);
            DocxInlineRenderer.AppendText(title, "目录", DocxRunStyle.For(TocTitleStyle));

            Paragraph field = new();
            field.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }));
            field.AppendChild(new Run(new FieldCode(" TOC \\o \"1-4\" \\h \\z \\u ")
            {
                Space = SpaceProcessingModeValues.Preserve,
            }));
            field.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }));
            DocxInlineRenderer.AppendText(field, "右键此处选择“更新域”可生成目录。", DocxRunStyle.For(settings.For(ContentCategory.Body)));
            field.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.End }));

            return new SdtBlock(
                new SdtProperties(new SdtContentDocPartObject(
                    new DocPartGallery { Val = "Table of Contents" },
                    new DocPartUnique())),
                new SdtContentBlock(title, field));
        }
    }
}
