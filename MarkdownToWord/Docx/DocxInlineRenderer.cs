using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
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
            if (emphasis.DelimiterChar is '~')
            {
                return style.AsStrike();
            }

            return emphasis.DelimiterCount >= 2 ? style.AsBold() : style.AsItalic();
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
                new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            parent.AppendChild(run);
        }

        // 按 OOXML 架构顺序构造 run 属性：rFonts → b → i → strike → color → sz → u
        private static RunProperties BuildRunProperties(DocxRunStyle style)
        {
            RunProperties rpr = new();
            rpr.AppendChild(
                style.Code switch
                {
                    true => new RunFonts { Ascii = CodeFont, HighAnsi = CodeFont, ComplexScript = CodeFont, EastAsia = style.Base.ChineseFont },
                    _ => OoxmlStyleHelper.BuildFonts(style.Base)
                }
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

            if (style.Base.Underline || style.Hyperlink)
            {
                rpr.AppendChild(new Underline { Val = UnderlineValues.Single });
            }

            return rpr;
        }
    }
}
