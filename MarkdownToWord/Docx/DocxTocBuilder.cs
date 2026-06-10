using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PersonalTools.MarkdownToWord.Models;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 在文档开头插入目录：固定样式的「目录」标题 + TOC 域（收集 1–4 级标题），并设置「打开时刷新域」。
    /// 目录条目字体/缩进沿用 Word「插入目录」时自动生成的内置 TOC 样式，不自定义（需求 4）。
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

        public static void AppendToc(Body body, DocxStyleSettings settings)
        {
            Paragraph title = new(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
            DocxInlineRenderer.AppendText(title, "目录", DocxRunStyle.For(TocTitleStyle));
            body.AppendChild(title);

            Paragraph field = new();
            field.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.Begin }));
            field.AppendChild(new Run(new FieldCode(" TOC \\o \"1-4\" \\h \\z \\u ")
            {
                Space = SpaceProcessingModeValues.Preserve,
            }));
            field.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.Separate }));
            DocxInlineRenderer.AppendText(field, "右键此处选择“更新域”可生成目录。", DocxRunStyle.For(settings.For(ContentCategory.Body)));
            field.AppendChild(new Run(new FieldChar { FieldCharType = FieldCharValues.End }));
            body.AppendChild(field);

            // 目录后分页，正文从新页开始
            body.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
        }

        /// <summary>设置「打开时刷新域」，保证 Word 打开即刷新目录/题注编号。</summary>
        public static void EnsureUpdateFieldsOnOpen(MainDocumentPart mainPart)
        {
            DocumentSettingsPart settingsPart = mainPart.AddNewPart<DocumentSettingsPart>();
            settingsPart.Settings = new Settings(new UpdateFieldsOnOpen { Val = true });
        }
    }
}
