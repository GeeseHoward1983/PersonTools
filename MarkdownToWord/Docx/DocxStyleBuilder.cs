using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PersonalTools.MarkdownToWord.Models;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 依据 <see cref="DocxStyleSettings"/> 生成 styles.xml：默认正文(Normal)、标题 1–4。
    /// 标题样式挂多级编号(numId)与大纲级别(outlineLvl)且强制无缩进；目录条目沿用 Word
    /// 「插入目录」时自动生成的内置 TOC 样式，不在此自定义（需求 4/7）。
    /// </summary>
    internal static class DocxStyleBuilder
    {
        public static void Build(StyleDefinitionsPart part, DocxStyleSettings settings)
        {
            ContentStyleRow body = settings.For(ContentCategory.Body);

            Styles styles = new();
            styles.Append(BuildDocDefaults(body));
            styles.Append(BuildNormalStyle(body));

            for (int level = 1; level <= 4; level++)
            {
                styles.Append(BuildHeadingStyle(level, settings.ForHeading(level)));
            }

            part.Styles = styles;
        }

        // 文档默认运行属性：让未套样式的零散内容也用正文字体
        private static DocDefaults BuildDocDefaults(ContentStyleRow body)
        {
            RunPropertiesBaseStyle baseRunProps = new();
            OoxmlStyleHelper.ApplyRunFormatting(baseRunProps, body);
            return new DocDefaults(
                new RunPropertiesDefault(baseRunProps),
                new ParagraphPropertiesDefault());
        }

        private static Style BuildNormalStyle(ContentStyleRow body)
        {
            Style style = new() { Type = StyleValues.Paragraph, StyleId = OoxmlIds.NormalStyleId, Default = true };
            style.Append(new StyleName { Val = "Normal" });
            style.Append(new PrimaryStyle());

            Indentation? indent = OoxmlStyleHelper.BuildFirstLineIndent(body);
            if (indent != null)
            {
                style.Append(new StyleParagraphProperties(indent));
            }

            StyleRunProperties runProps = new();
            OoxmlStyleHelper.ApplyRunFormatting(runProps, body);
            style.Append(runProps);
            return style;
        }

        private static Style BuildHeadingStyle(int level, ContentStyleRow row)
        {
            Style style = new() { Type = StyleValues.Paragraph, StyleId = OoxmlIds.HeadingStyleId(level) };
            style.Append(new StyleName { Val = "heading " + level });
            style.Append(new BasedOn { Val = OoxmlIds.NormalStyleId });
            style.Append(new NextParagraphStyle { Val = OoxmlIds.NormalStyleId });
            style.Append(new PrimaryStyle());

            // pPr 子元素顺序：keepNext → numPr → spacing → ind → outlineLvl
            StyleParagraphProperties pPr = new();
            pPr.Append(new KeepNext());
            pPr.Append(new NumberingProperties(new NumberingId { Val = OoxmlIds.HeadingNumId }));
            pPr.Append(new SpacingBetweenLines { Before = "240", After = "120" });
            pPr.Append(new Indentation { FirstLine = "0", FirstLineChars = 0, Left = "0", LeftChars = 0 }); // 标题不缩进（需求 7）
            pPr.Append(new OutlineLevel { Val = level - 1 });
            style.Append(pPr);

            StyleRunProperties runProps = new();
            OoxmlStyleHelper.ApplyRunFormatting(runProps, row);
            style.Append(runProps);
            return style;
        }
    }
}
