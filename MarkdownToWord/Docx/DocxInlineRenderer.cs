using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Extensions.Mathematics;
using Markdig.Syntax.Inlines;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 把 Markdig 内联 AST（文本/粗斜/删除线/行内代码/链接/图片/换行）渲染为 OOXML run，
    /// 追加到给定容器（段落或超链接）。每个 run 按 <see cref="DocxRunStyle"/> 套中西文分槽字体。
    /// </summary>
    internal static class DocxInlineRenderer
    {
        private const string HyperlinkColor = "0563C1";
        private const string CodeFont = "Consolas";

        /// <summary>渲染容器内联的所有子节点到 <paramref name="parent"/>（Paragraph 或 Hyperlink）。</summary>
        public static void RenderInlines(ContainerInline? container, OpenXmlElement parent, DocxRunStyle style, DocxRenderContext ctx)
        {
            if (container == null)
            {
                return;
            }

            foreach (Inline inline in container)
            {
                RenderInline(inline, parent, style, ctx);
            }
        }

        private static void RenderInline(Inline inline, OpenXmlElement parent, DocxRunStyle style, DocxRenderContext ctx)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    AppendText(parent, literal.Content.ToString(), style);
                    break;
                case HtmlEntityInline htmlEntity:
                    // HTML 实体(&copy;/&nbsp;/&#169; 等)解析为 HtmlEntityInline，之前落入 default 被静默丢弃 → 字符消失
                    AppendText(parent, htmlEntity.Transcoded.ToString(), style);
                    break;
                case MathInline math:
                    // 行内数学 $...$（UseMathematics 已启用）：以原始 $内容$ 文本兜底渲染，避免被 default 静默丢弃
                    AppendText(parent, $"${math.Content.ToString()}$", style.AsCode());
                    break;
                case EmphasisInline emphasis:
                    RenderInlines(emphasis, parent, ResolveEmphasis(emphasis, style), ctx);
                    break;
                case CodeInline code:
                    AppendText(parent, code.Content, style.AsCode());
                    break;
                case LineBreakInline lineBreak:
                    if (lineBreak.IsHard)
                    {
                        parent.AppendChild(new Run(new Break()));
                    }
                    else
                    {
                        AppendText(parent, " ", style);
                    }

                    break;
                case LinkInline link:
                    RenderLink(link, parent, style, ctx);
                    break;
                case AutolinkInline autolink:
                    RenderAutolink(autolink, parent, style, ctx);
                    break;
                case ContainerInline container:
                    RenderInlines(container, parent, style, ctx); // 其它容器型内联兜底递归
                    break;
                default:
                    break;
            }
        }

        private static DocxRunStyle ResolveEmphasis(EmphasisInline emphasis, DocxRunStyle style)
        {
            // Markdig EmphasisExtras：按定界符精确分派，避免把下标/上标/插入/高亮误当删除线/斜体/加粗
            return emphasis.DelimiterChar switch
            {
                '~' => emphasis.DelimiterCount >= 2 ? style.AsStrike() : style.AsSubscript(), // ~~删除线~~ / ~下标~
                '^' => style.AsSuperscript(),   // ^上标^
                '+' => style.AsInserted(),      // ++插入++（以下划线表示）
                '=' => style.AsHighlight(),     // ==高亮==
                _ => emphasis.DelimiterCount >= 2 ? style.AsBold() : style.AsItalic(), // **粗** / *斜*
            };
        }

        private static void RenderLink(LinkInline link, OpenXmlElement parent, DocxRunStyle style, DocxRenderContext ctx)
        {
            if (link.IsImage)
            {
                DocxImageEmbedder.AppendInlineImage(parent, link, style, ctx);
                return;
            }

            Hyperlink? hyperlink = TryCreateHyperlink(link.Url, ctx);
            if (hyperlink == null)
            {
                RenderInlines(link, parent, style, ctx); // 相对/锚点链接：仅渲染文字
                return;
            }

            RenderInlines(link, hyperlink, style.AsHyperlink(), ctx);
            parent.AppendChild(hyperlink);
        }

        private static void RenderAutolink(AutolinkInline autolink, OpenXmlElement parent, DocxRunStyle style, DocxRenderContext ctx)
        {
            Hyperlink? hyperlink = TryCreateHyperlink(autolink.Url, ctx);
            if (hyperlink == null)
            {
                AppendText(parent, autolink.Url, style);
                return;
            }

            AppendText(hyperlink, autolink.Url, style.AsHyperlink());
            parent.AppendChild(hyperlink);
        }

        private static Hyperlink? TryCreateHyperlink(string? url, DocxRenderContext ctx)
        {
            if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return null;
            }

            // 仅登记安全 scheme 的外部超链接；javascript:/file:/UNC 等一律降级为纯文字（调用方回退渲染文本），
            // 避免不受信 Markdown 借 [x](javascript:…) / [x](file://\\host\share) 生成可点击的危险链接
            if (uri.Scheme is not ("http" or "https" or "mailto"))
            {
                return null;
            }

            try
            {
                string id = ctx.MainPart.AddHyperlinkRelationship(uri, true).Id;
                return new Hyperlink { Id = id };
            }
            catch (UriFormatException)
            {
                return null;
            }
        }

        /// <summary>构造一个文本 run 并追加到容器；空文本忽略。</summary>
        public static void AppendText(OpenXmlElement parent, string? text, DocxRunStyle style)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Run run = new(BuildRunProperties(style),
                new Text(SanitizeXmlText(text)) { Space = SpaceProcessingModeValues.Preserve });
            parent.AppendChild(run);
        }

        /// <summary>
        /// 剥离 XML 1.0 非法字符：仅保留 #x9/#xA/#xD、[#x20-#xD7FF]、[#xE000-#xFFFD]、[#x10000-#x10FFFF]。
        /// 非法字符（如从终端粘贴的 ESC/换页等 C0 控制符、未配对代理项、U+FFFE/FFFF）会使写入 w:t 后的
        /// .docx 无法打开，或在 Save 时抛异常中止整篇导出。无非法字符时原样返回（不额外分配）。
        /// </summary>
        private static string SanitizeXmlText(string text)
        {
            StringBuilder? sb = null; // 惰性分配：绝大多数文本无需清洗
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // 合法代理对(U+10000..U+10FFFF)：整体保留
                if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
                {
                    sb?.Append(c);
                    sb?.Append(text[i + 1]);
                    i++;
                    continue;
                }

                bool legal = c is '\t' or '\n' or '\r'
                    || (c >= '\u0020' && c <= '\uD7FF')
                    || (c >= '\uE000' && c <= '\uFFFD');

                if (legal)
                {
                    sb?.Append(c);
                }
                else if (sb == null)
                {
                    // 首次遇到非法字符：把已扫描的合法前缀拷入 sb，其后逐字符决定保留/丢弃
                    sb = new StringBuilder(text.Length);
                    sb.Append(text, 0, i);
                }
                // 非法字符（含未配对代理项）直接丢弃
            }

            return sb?.ToString() ?? text;
        }

        // 按 OOXML 架构顺序构造 run 属性：rFonts → b → i → strike → color → sz/szCs → highlight → u → vertAlign
        private static RunProperties BuildRunProperties(DocxRunStyle style)
        {
            RunProperties rpr = new();
            rpr.AppendChild(
                style.Code
                    ? new RunFonts { Ascii = CodeFont, HighAnsi = CodeFont, ComplexScript = CodeFont, EastAsia = style.Base.ChineseFont }
                    : OoxmlStyleHelper.BuildFonts(style.Base)
            );

            if (style.Base.Bold || style.Bold)
            {
                rpr.AppendChild(new Bold());
                rpr.AppendChild(new BoldComplexScript());
            }

            if (style.Italic)
            {
                rpr.AppendChild(new Italic());
                rpr.AppendChild(new ItalicComplexScript());
            }

            if (style.Strike)
            {
                rpr.AppendChild(new Strike());
            }

            if (style.Hyperlink)
            {
                rpr.AppendChild(new Color { Val = HyperlinkColor });
            }

            string sz = style.Base.HalfPoint.ToString(CultureInfo.InvariantCulture);
            rpr.AppendChild(new FontSize { Val = sz });
            rpr.AppendChild(new FontSizeComplexScript { Val = sz });

            if (style.Highlight) // ==高亮==（w:highlight 须在 w:u 之前）
            {
                rpr.AppendChild(new Highlight { Val = HighlightColorValues.Yellow });
            }

            if (style.Base.Underline || style.Hyperlink || style.Inserted) // ++插入++ 以下划线表示
            {
                rpr.AppendChild(new Underline { Val = UnderlineValues.Single });
            }

            if (style.Subscript || style.Superscript) // ~下标~ / ^上标^（w:vertAlign 须在 w:u 之后）
            {
                rpr.AppendChild(new VerticalTextAlignment
                {
                    Val = style.Superscript ? VerticalPositionValues.Superscript : VerticalPositionValues.Subscript
                });
            }

            return rpr;
        }
    }
}
