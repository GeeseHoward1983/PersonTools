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

            // 封面与目录同属第 1 节（不显示页码）；二者之间靠「目录段前分页」过渡，避免多余空段落。
            // 仅当文档首个块即为一级标题时才视作封面：否则把正文中间的一级标题抽出当封面，
            // 会让它从原位置消失、出现在文首，与正文渲染时的标题判定不一致。
            List<Block> blocks = [.. ast];
            bool hasCover = blocks.Count > 0 && blocks[0] is HeadingBlock { Level: 1 };
            if (hasCover)
            {
                DocxCoverBuilder.RenderCover((HeadingBlock)blocks[0], body, ctx);
                ctx.CoverRendered = true;
                blocks.RemoveAt(0);
            }

            if (settings.GenerateToc)
            {
                body.AppendChild(DocxTocBuilder.BuildToc(settings, pageBreakBeforeTitle: hasCover));
            }

            // 封面/目录与正文之间唯一的分节符（下一页），正文从此另起一节并重排页码
            if (hasCover || settings.GenerateToc)
            {
                body.AppendChild(DocxSectionBuilder.BuildSectionBreak());
            }

            foreach (Block block in blocks)
            {
                DocxBlockRenderer.RenderBlock(block, body, ctx, 0);
            }

            // 正文末尾节：页码从 1 开始（封面/目录不计页数）
            body.AppendChild(DocxSectionBuilder.BuildBodySection(mainPart));
            mainPart.Document.Save();
        }
    }
}
