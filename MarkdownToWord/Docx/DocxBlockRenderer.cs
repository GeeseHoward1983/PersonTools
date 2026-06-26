using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using PersonalTools.MarkdownToWord.Models;
using MTable = Markdig.Extensions.Tables.Table;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 遍历 Markdig 块级 AST，渲染为 OOXML 段落/表格。标题套 Heading1–4 样式（>4 级降为加粗正文），
    /// 正文套 Normal（含首行缩进），列表/引用按层级左缩进，代码块加底纹，纯图片段落生成图题注。
    /// </summary>
    internal static partial class DocxBlockRenderer
    {
        private const int IndentTwips = 420;  // 每级左缩进 ~2 字
        private const int IndentChars = 200;

        // 块级递归最大深度：RenderBlock↔RenderQuote/RenderList/RenderListItem 互相递归，深度由不可信
        // Markdown 的嵌套层数决定。超千层的嵌套引用/列表会触发不可捕获的 StackOverflowException 直接崩进程，
        // 故对递归深度设上限（远超任何正常文档），超限即停止下钻而非崩溃。
        private const int MaxNestingDepth = 64;

        // 匹配标题文本开头的编号前缀（如 "1 " / "1. " / "1.1 " / "1.1.1 "），含全角空格
        [GeneratedRegex(@"^\s*\d+(?:\.\d+)*\.?[ \t　]+")]
        private static partial Regex HeadingNumberPrefix();

        internal static void RenderBlock(Block block, OpenXmlElement container, DocxRenderContext ctx, int indentLevel)
        {
            // indentLevel 随每层 quote/list 递增，直接用作深度计量；超限丢弃更深内容，防递归爆栈。
            if (indentLevel > MaxNestingDepth)
            {
                return;
            }

            switch (block)
            {
                case HeadingBlock heading:
                    RenderHeading(heading, container, ctx);
                    break;
                case MTable table:
                    DocxTableRenderer.Render(table, container, ctx);
                    break;
                case ListBlock list:
                    RenderList(list, container, ctx, indentLevel);
                    break;
                case QuoteBlock quote:
                    RenderQuote(quote, container, ctx, indentLevel);
                    break;
                case FencedCodeBlock fenced:
                    RenderCode(fenced, container, ctx);
                    break;
                case CodeBlock code:
                    RenderCode(code, container, ctx);
                    break;
                case ThematicBreakBlock:
                    RenderThematicBreak(container);
                    break;
                case ParagraphBlock paragraph:
                    RenderParagraph(paragraph, container, ctx, indentLevel);
                    break;
                case HtmlBlock:
                    break; // 跳过原始 HTML 块
                case ContainerBlock nested:
                    foreach (Block child in nested)
                    {
                        RenderBlock(child, container, ctx, indentLevel);
                    }

                    break;
                default:
                    break;
            }
        }

        private static void RenderHeading(HeadingBlock heading, OpenXmlElement container, DocxRenderContext ctx)
        {
            // 封面仅由 DocxWriter 对「文档首块即一级标题」的情形生成(并已从块列表移除)；此处不再把正文中
            // 任何一级标题当封面——否则文档不以 H1 开头时，正文中部的首个 H1 会被抽成整页伪封面、从原位置消失。
            // 走到这里的一级标题(含文首非 H1 时的后续 H1)统一降级为 Word 一级标题(wordLevel=1)。
            int wordLevel = Math.Max(1, heading.Level - 1);
            bool styled = wordLevel is >= 1 and <= 4;

            // 去掉 Markdown 标题文本里的编号前缀，改由 Word 多级编号自动生成（需求 3）
            StripLeadingNumber(heading.Inline);

            ParagraphProperties pPr = new(
                new ParagraphStyleId { Val = styled ? OoxmlIds.HeadingStyleId(wordLevel) : OoxmlIds.NormalStyleId });

            // 每个一级标题另起一页：首章前不插分页，其后每章在前面插入分页（需求 3）
            if (wordLevel == 1)
            {
                if (ctx.FirstChapterRendered)
                {
                    pPr.AppendChild(new PageBreakBefore());
                }
                else
                {
                    ctx.FirstChapterRendered = true;
                }
            }

            Paragraph paragraph = new(pPr);

            ContentStyleRow row = styled ? ctx.Settings.ForHeading(wordLevel) : ctx.Settings.For(ContentCategory.Body);
            DocxRunStyle style = DocxRunStyle.For(row);
            if (!styled)
            {
                style = style.AsBold(); // 超 4 级（Markdown 6 级以上）：降级为加粗正文
            }

            DocxInlineRenderer.RenderInlines(heading.Inline, paragraph, style, ctx);
            container.AppendChild(paragraph);
        }

        /// <summary>去掉标题首个文本节点开头的「1 / 1. / 1.1 / 1.1.1」等编号前缀（Word 会自动编号）。</summary>
        internal static void StripLeadingNumber(ContainerInline? inline)
        {
            if (inline?.FirstChild is LiteralInline literal)
            {
                string text = literal.Content.ToString();
                string stripped = HeadingNumberPrefix().Replace(text, string.Empty);
                if (stripped.Length != text.Length)
                {
                    literal.Content = new StringSlice(stripped);
                }
            }
        }

        private static void RenderParagraph(ParagraphBlock paragraph, OpenXmlElement container, DocxRenderContext ctx, int indentLevel)
        {
            LinkInline? image = AsPureImage(paragraph);
            if (image != null)
            {
                RenderFigure(image, container, ctx);
                return;
            }

            Paragraph wordParagraph = indentLevel > 0 ? NewIndentedParagraph(indentLevel) : NewBodyParagraph();
            DocxInlineRenderer.RenderInlines(paragraph.Inline, wordParagraph, DocxRunStyle.For(ctx.Settings.For(ContentCategory.Body)), ctx);
            container.AppendChild(wordParagraph);
        }

        private static void RenderFigure(LinkInline image, OpenXmlElement container, DocxRenderContext ctx)
        {
            DocxRunStyle bodyStyle = DocxRunStyle.For(ctx.Settings.For(ContentCategory.Body));
            // KeepNext：让图片段与紧随其下的图题注保持同页，避免图在页底时被与题注拆到两页
            Paragraph imageParagraph = new(new ParagraphProperties(
                new KeepNext(),
                new Justification { Val = JustificationValues.Center }));
            bool embedded = DocxImageEmbedder.AppendInlineImage(imageParagraph, image, bodyStyle, ctx);
            container.AppendChild(imageParagraph);

            if (embedded)
            {
                container.AppendChild(DocxCaptionBuilder.BuildFigureCaption(DocxImageEmbedder.ExtractAltText(image), ctx));
            }
        }

        private static void RenderList(ListBlock list, OpenXmlElement container, DocxRenderContext ctx, int indentLevel)
        {
            long start;
            long number = list.IsOrdered switch
            {
                true when long.TryParse(list.OrderedStart, out start) => start,
                _ => 1,
            };
            foreach (Block itemObj in list)
            {
                if (itemObj is not ListItemBlock item)
                {
                    continue;
                }

                string marker = list.IsOrdered
                    ? number.ToString(CultureInfo.InvariantCulture) + ". "
                    : "• ";
                RenderListItem(item, container, ctx, indentLevel + 1, marker);
                number++;
            }
        }

        private static void RenderListItem(ListItemBlock item, OpenXmlElement container, DocxRenderContext ctx, int indentLevel, string marker)
        {
            // 嵌套列表经 RenderList↔RenderListItem 互递归，绕过 RenderBlock 入口的深度守卫；
            // 在此对已递增的 indentLevel 施加同一上限，防深层嵌套列表触发不可捕获的 StackOverflow（见 MaxNestingDepth）。
            if (indentLevel > MaxNestingDepth)
            {
                return;
            }

            bool first = true;
            DocxRunStyle style = DocxRunStyle.For(ctx.Settings.For(ContentCategory.Body));
            foreach (Block child in item)
            {
                if (child is ParagraphBlock paragraph)
                {
                    Paragraph wordParagraph = NewIndentedParagraph(indentLevel);
                    if (first)
                    {
                        DocxInlineRenderer.AppendText(wordParagraph, marker, style);
                    }

                    DocxInlineRenderer.RenderInlines(paragraph.Inline, wordParagraph, style, ctx);
                    container.AppendChild(wordParagraph);
                    first = false;
                }
                else if (child is ListBlock nested)
                {
                    RenderList(nested, container, ctx, indentLevel);
                }
                else
                {
                    RenderBlock(child, container, ctx, indentLevel);
                }
            }
        }

        private static void RenderQuote(QuoteBlock quote, OpenXmlElement container, DocxRenderContext ctx, int indentLevel)
        {
            foreach (Block child in quote)
            {
                RenderBlock(child, container, ctx, indentLevel + 1);
            }
        }

        private static void RenderCode(CodeBlock code, OpenXmlElement container, DocxRenderContext ctx)
        {
            Paragraph paragraph = new(new ParagraphProperties(
                new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "F6F8FA" },
                new SpacingBetweenLines { Before = "60", After = "60" }));

            DocxRunStyle style = DocxRunStyle.For(ctx.Settings.For(ContentCategory.Body)).AsCode();
            int count = code.Lines.Count;
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    paragraph.AppendChild(new Run(new Break()));
                }

                DocxInlineRenderer.AppendText(paragraph, code.Lines.Lines[i].Slice.ToString(), style);
            }

            container.AppendChild(paragraph);
        }

        private static void RenderThematicBreak(OpenXmlElement container)
        {
            container.AppendChild(new Paragraph(new ParagraphProperties(new ParagraphBorders(
                new BottomBorder { Val = BorderValues.Single, Size = 6, Space = 1, Color = "AAAAAA" }))));
        }

        private static Paragraph NewBodyParagraph() =>
            new(new ParagraphProperties(new ParagraphStyleId { Val = OoxmlIds.NormalStyleId }));

        private static Paragraph NewIndentedParagraph(int level) =>
            new(new ParagraphProperties(new Indentation
            {
                Left = (level * IndentTwips).ToString(CultureInfo.InvariantCulture),
                LeftChars = level * IndentChars,
                FirstLine = "0",
                FirstLineChars = 0,
            }));

        // 判断段落是否「仅含一张图片」（忽略空白），是则作为带题注的图处理
        private static LinkInline? AsPureImage(ParagraphBlock paragraph)
        {
            if (paragraph.Inline == null)
            {
                return null;
            }

            LinkInline? image = null;
            int imageCount = 0;
            foreach (Inline inline in paragraph.Inline)
            {
                switch (inline)
                {
                    case LinkInline { IsImage: true } candidate:
                        image = candidate;
                        imageCount++;
                        break;
                    case LiteralInline literal when string.IsNullOrWhiteSpace(literal.Content.ToString()):
                    case LineBreakInline:
                        break;
                    default:
                        return null;
                }
            }

            return imageCount == 1 ? image : null;
        }
    }
}
