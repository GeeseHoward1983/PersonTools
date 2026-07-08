using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Extensions.DefinitionLists;
using Markdig.Extensions.Figures;
using Markdig.Extensions.Footnotes;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using PersonalTools.MarkdownToWord.Models;
using MTable = Markdig.Extensions.Tables.Table;
using MdFootnote = Markdig.Extensions.Footnotes.Footnote; // 与 DocumentFormat.OpenXml.Wordprocessing.Footnote 区分

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
        // internal：表格单元格也是递归入口（grid table 单元格可含嵌套表格），DocxTableRenderer 需引用同一上限作单一来源，勿复制字面量。
        internal const int MaxNestingDepth = 64;

        // 匹配标题文本开头的「章节编号」前缀（如 "1. " / "1.1 " / "1.1.1 " / "1.1. "），含全角空格。
        // 要求编号内至少含一个点号，从而只剥离明确的章节号，不误删以纯数字开头的合法标题
        // （如 "2024 年度报告"、"3 个要点"、"1 Introduction" 这类无点号的前缀不再被当作编号剥掉）。
        // 写法须无回溯歧义：旧写法 (?:\d+\.?)* 中 \.? 可不消费字符，纯数字长串整体失配时会灾难性回溯（ReDoS）；
        // 现每段由字面点号锚定（\d+\. 开头、(?:\d+\.)* 续段、\d* 收尾），语言不变但匹配路径唯一。
        [GeneratedRegex(@"^\s*\d+\.(?:\d+\.)*\d*[ \t　]+")]
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
                    // 表格也是递归入口（grid table 单元格可含块级内容乃至嵌套表格），须把当前深度沿调用链传入，
                    // 与 RenderBlock 共用 MaxNestingDepth 守卫，防深层嵌套 grid table 绕过守卫栈溢出。
                    DocxTableRenderer.Render(table, container, ctx, indentLevel);
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
                // 注意：Markdig 的 MathBlock 继承自 FencedCodeBlock，故块级 $$...$$ 已由上面的
                // FencedCodeBlock/CodeBlock 分支按等宽代码块渲染，无需(也不能)再单列 case。
                case CodeBlock code:
                    RenderCode(code, container, ctx);
                    break;
                case ThematicBreakBlock:
                    RenderThematicBreak(container);
                    break;
                case ParagraphBlock paragraph:
                    RenderParagraph(paragraph, container, ctx, indentLevel);
                    break;
                case DefinitionTerm term:
                    // 定义列表术语行（LeafBlock）：之前落 default 使术语整行消失、只剩定义正文；渲染为加粗段落。
                    RenderDefinitionTerm(term, container, ctx, indentLevel);
                    break;
                case FigureCaption caption:
                    // 图（Figure 扩展）题注（LeafBlock）：之前落 default 使题注消失；渲染为居中斜体段落。
                    RenderFigureCaption(caption, container, ctx);
                    break;
                case FootnoteGroup footnotes:
                    // 脚注区（ContainerBlock）：须在 ContainerBlock 泛化分支前拦截，带编号成区渲染，
                    // 否则脚注正文被当普通段落无编号地堆在文末。
                    RenderFootnoteGroup(footnotes, container, ctx, indentLevel);
                    break;
                case HtmlBlock:
                    break; // 跳过原始 HTML 块
                case ContainerBlock nested:
                    // 泛化容器同样是递归入口，深度必须 +1 纳入 MaxNestingDepth 守卫，
                    // 否则深层嵌套的自定义容器可绕过守卫递归爆栈
                    foreach (Block child in nested)
                    {
                        RenderBlock(child, container, ctx, indentLevel + 1);
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
                new Indentation { FirstLine = "0", FirstLineChars = 0 }, // 重置首行缩进防居中图片右移
                new Justification { Val = JustificationValues.Center }));
            bool embedded = DocxImageEmbedder.AppendInlineImage(imageParagraph, image, bodyStyle, ctx);
            container.AppendChild(imageParagraph);

            if (embedded)
            {
                container.AppendChild(DocxCaptionBuilder.BuildFigureCaption(DocxImageEmbedder.ExtractAltText(image), ctx));
            }
        }

        // 定义列表术语：加粗段落（其定义正文仍由后续 ParagraphBlock 分支渲染）
        private static void RenderDefinitionTerm(DefinitionTerm term, OpenXmlElement container, DocxRenderContext ctx, int indentLevel)
        {
            Paragraph paragraph = indentLevel > 0 ? NewIndentedParagraph(indentLevel) : NewBodyParagraph();
            DocxRunStyle style = DocxRunStyle.For(ctx.Settings.For(ContentCategory.Body)).AsBold();
            DocxInlineRenderer.RenderInlines(term.Inline, paragraph, style, ctx);
            container.AppendChild(paragraph);
        }

        // 图（Figure 扩展）题注：居中斜体段落
        private static void RenderFigureCaption(FigureCaption caption, OpenXmlElement container, DocxRenderContext ctx)
        {
            Paragraph paragraph = new(new ParagraphProperties(
                new Indentation { FirstLine = "0", FirstLineChars = 0 }, // 重置首行缩进防居中题注右移
                new Justification { Val = JustificationValues.Center }));
            DocxRunStyle style = DocxRunStyle.For(ctx.Settings.For(ContentCategory.Body)).AsItalic();
            DocxInlineRenderer.RenderInlines(caption.Inline, paragraph, style, ctx);
            container.AppendChild(paragraph);
        }

        // 脚注区：与正文间插一条分隔线，每条脚注以「编号. 」前缀渲染，编号与行内 FootnoteLink 上标一致
        private static void RenderFootnoteGroup(FootnoteGroup group, OpenXmlElement container, DocxRenderContext ctx, int indentLevel)
        {
            RenderThematicBreak(container);
            DocxRunStyle style = DocxRunStyle.For(ctx.Settings.For(ContentCategory.Body));
            foreach (Block child in group)
            {
                if (child is not MdFootnote footnote)
                {
                    continue;
                }

                string number = footnote.Order.ToString(CultureInfo.InvariantCulture) + ". ";
                bool numberRendered = false;
                foreach (Block content in footnote)
                {
                    if (content is ParagraphBlock paragraph)
                    {
                        Paragraph wordParagraph = NewBodyParagraph();
                        if (!numberRendered)
                        {
                            DocxInlineRenderer.AppendText(wordParagraph, number, style.AsBold());
                            numberRendered = true;
                        }

                        DocxInlineRenderer.RenderInlines(paragraph.Inline, wordParagraph, style, ctx);
                        container.AppendChild(wordParagraph);
                    }
                    else
                    {
                        // 首个内容块不是段落（脚注以列表/代码块开头）：先单独输出编号成段，
                        // 保证脚注编号在文末始终出现、与行内上标 [^n] 对得上，而非因首块类型而丢失编号。
                        if (!numberRendered)
                        {
                            AppendFootnoteNumber(container, number, style);
                            numberRendered = true;
                        }

                        RenderBlock(content, container, ctx, indentLevel + 1);
                    }
                }

                // 脚注无任何内容块时仍输出编号，避免编号完全缺失
                if (!numberRendered)
                {
                    AppendFootnoteNumber(container, number, style);
                }
            }
        }

        // 将脚注编号作为独立正文段落输出（用于脚注首个内容块非段落或脚注为空的情形）
        private static void AppendFootnoteNumber(OpenXmlElement container, string number, DocxRunStyle style)
        {
            Paragraph numberParagraph = NewBodyParagraph();
            DocxInlineRenderer.AppendText(numberParagraph, number, style.AsBold());
            container.AppendChild(numberParagraph);
        }

        private static void RenderList(ListBlock list, OpenXmlElement container, DocxRenderContext ctx, int indentLevel)
        {
            long start;
            // 起始序号以源 Markdown 的 OrderedStart 为准（如 "3." 开头则从 3 起），解析失败回退 1
            long fallback = list.IsOrdered switch
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

                // CommonMark：仅首项序号决定起始（OrderedStart），后续各项的源字面序号一律忽略、顺序递增；
                // 若逐项采用源 Order，"1./1./1." 会导出成 "1. 1. 1."，且与预览 <ol> 渲染(1.2.3.)不一致
                string marker;
                if (list.IsOrdered)
                {
                    marker = fallback.ToString(CultureInfo.InvariantCulture) + ". ";
                    fallback++;
                }
                else
                {
                    marker = "• ";
                }

                RenderListItem(item, container, ctx, indentLevel + 1, marker);
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
            => RenderMonospaceLines(code.Lines, container, ctx);

        // 以等宽+底纹段落逐行渲染一组文本行（代码块与块级数学公式共用）
        private static void RenderMonospaceLines(StringLineGroup lines, OpenXmlElement container, DocxRenderContext ctx)
        {
            Paragraph paragraph = new(new ParagraphProperties(
                new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "F6F8FA" },
                new SpacingBetweenLines { Before = "60", After = "60" }));

            DocxRunStyle style = DocxRunStyle.For(ctx.Settings.For(ContentCategory.Body)).AsCode();
            int count = lines.Count;
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    paragraph.AppendChild(new Run(new Break()));
                }

                DocxInlineRenderer.AppendText(paragraph, lines.Lines[i].Slice.ToString(), style);
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
