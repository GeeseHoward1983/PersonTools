using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 分节与页码：封面、目录、正文各为一节（下一页分节符，避免手动分页符造成的多余空白页，需求 4）。
    /// 仅正文节带页脚页码且从 1 开始；封面/目录节无页脚，故不显示页码（需求 5）。
    /// </summary>
    internal static class DocxSectionBuilder
    {
        /// <summary>
        /// 封面/目录节的「下一页分节符」段落（无页脚、无页码）。段落标记设为隐藏(vanish)+1pt+行高1缇，
        /// 使其折叠为 0 高度——即便目录更新后正好占满整页，这个空段落也不会被挤到单独一页，
        /// 从而彻底消除目录后的空白页（分节属性不受标记隐藏影响，正文仍从下一页开始）。
        /// </summary>
        public static Paragraph BuildSectionBreak()
        {
            SectionProperties sectPr = new(
                new SectionType { Val = SectionMarkValues.NextPage },
                BuildPageSize(),
                BuildPageMargin());

            ParagraphProperties pPr = new(
                new SpacingBetweenLines { Line = "1", LineRule = LineSpacingRuleValues.Exact },
                new ParagraphMarkRunProperties(
                    new Vanish(),
                    new FontSize { Val = "2" },
                    new FontSizeComplexScript { Val = "2" }),
                sectPr);
            return new Paragraph(pPr);
        }

        /// <summary>正文末尾节：下一页起、页码从 1 开始、居中页脚显示页码。</summary>
        public static SectionProperties BuildBodySection(MainDocumentPart mainPart)
        {
            string footerId = AddPageNumberFooter(mainPart);
            return new SectionProperties(
                new FooterReference { Type = HeaderFooterValues.Default, Id = footerId },
                new SectionType { Val = SectionMarkValues.NextPage },
                BuildPageSize(),
                BuildPageMargin(),
                new PageNumberType { Start = 1 });
        }

        private static string AddPageNumberFooter(MainDocumentPart mainPart)
        {
            FooterPart footerPart = mainPart.AddNewPart<FooterPart>();

            Paragraph paragraph = new(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
            paragraph.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }));
            paragraph.AppendChild(new Run(new FieldCode(" PAGE ") { Space = SpaceProcessingModeValues.Preserve }));
            paragraph.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }));
            paragraph.AppendChild(new Run(new Text("1")));
            paragraph.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.End }));

            footerPart.Footer = new Footer(paragraph);
            return mainPart.GetIdOfPart(footerPart);
        }

        // A4 页面 + 1 英寸页边距，各节共用
        private static PageSize BuildPageSize() => new() { Width = 11906U, Height = 16838U };

        private static PageMargin BuildPageMargin() => new()
        {
            Top = 1440,
            Bottom = 1440,
            Left = 1440U,
            Right = 1440U,
            Header = 720U,
            Footer = 720U,
            Gutter = 0U,
        };
    }
}
