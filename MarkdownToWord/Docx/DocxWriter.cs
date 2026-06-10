using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;
using PersonalTools.MarkdownToWord.Models;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 导出编排器：建 .docx 包 → 写 styles/numbering/settings 部件 → （可选）插目录 → 遍历块渲染正文 → 保存。
    /// 是 Markdown→Word 的唯一对外入口。
    /// </summary>
    internal static class DocxWriter
    {
        public static void Write(MarkdownDocument ast, DocxStyleSettings settings, string outputPath, string? baseDir)
        {
            using WordprocessingDocument document = WordprocessingDocument.Create(outputPath, WordprocessingDocumentType.Document);

            MainDocumentPart mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = mainPart.Document.AppendChild(new Body());

            StyleDefinitionsPart stylePart = mainPart.AddNewPart<StyleDefinitionsPart>();
            DocxStyleBuilder.Build(stylePart, settings);

            NumberingDefinitionsPart numberingPart = mainPart.AddNewPart<NumberingDefinitionsPart>();
            DocxNumberingBuilder.Build(numberingPart);

            DocxRenderContext ctx = new(mainPart, settings, baseDir);

            // Markdown 一级标题 → 封面，置于目录之前；其余块按序渲染
            List<Block> blocks = [.. ast];
            int coverIndex = blocks.FindIndex(b => b is HeadingBlock { Level: 1 });
            if (coverIndex >= 0)
            {
                DocxCoverBuilder.RenderCover((HeadingBlock)blocks[coverIndex], body, ctx);
                blocks.RemoveAt(coverIndex);
            }

            if (settings.GenerateToc)
            {
                DocxTocBuilder.EnsureUpdateFieldsOnOpen(mainPart);
                DocxTocBuilder.AppendToc(body, settings);
            }

            foreach (Block block in blocks)
            {
                DocxBlockRenderer.RenderBlock(block, body, ctx, 0);
            }

            body.AppendChild(BuildSectionProperties());
            mainPart.Document.Save();
        }

        // A4 页面 + 1 英寸页边距，作为 body 末尾的节属性
        private static SectionProperties BuildSectionProperties()
        {
            return new SectionProperties(
                new PageSize { Width = 11906U, Height = 16838U },
                new PageMargin
                {
                    Top = 1440,
                    Bottom = 1440,
                    Left = 1440U,
                    Right = 1440U,
                    Header = 720U,
                    Footer = 720U,
                    Gutter = 0U,
                });
        }
    }
}
