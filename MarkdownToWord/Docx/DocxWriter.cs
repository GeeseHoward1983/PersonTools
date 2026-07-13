using System.IO;
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
            // 原子写：先写临时文件，全部成功后再替换目标，避免渲染/保存中途抛异常时留下损坏的 .docx
            // 覆盖掉用户既有的正确文件。临时文件放在目标同目录，保证 File.Move 为同卷原子重命名。
            string fullOutput = Path.GetFullPath(outputPath);
            string dir = Path.GetDirectoryName(fullOutput) ?? Directory.GetCurrentDirectory();
            string tempPath = Path.Combine(dir, "." + Path.GetFileName(fullOutput) + "." + Guid.NewGuid().ToString("N") + ".tmp");

            bool moved = false;
            try
            {
                using (WordprocessingDocument document = WordprocessingDocument.Create(tempPath, WordprocessingDocumentType.Document))
                {
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
                    // 告知标题渲染映射策略：有封面时正文标题上移一级（H2=章），无封面时按原级映射
                    ctx.HasCover = hasCover;
                    if (hasCover)
                    {
                        DocxCoverBuilder.RenderCover((HeadingBlock)blocks[0], body, ctx);
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

                    // 兜底：无封面/目录且 Markdown 为空时，正文无任何块级内容，Body 将仅含末尾 sectPr，
                    // 部分严格 OOXML 消费方/旧版 Word 会判定文档需修复。补一个空段落，确保 Body 至少有一个块级子元素。
                    if (!body.Elements<Paragraph>().Any() && !body.Elements<Table>().Any())
                    {
                        body.AppendChild(new Paragraph());
                    }

                    // 正文末尾节：页码从 1 开始（封面/目录不计页数）
                    body.AppendChild(DocxSectionBuilder.BuildBodySection(mainPart));
                    mainPart.Document.Save();
                }   // dispose 触发包写盘落地

                File.Move(tempPath, fullOutput, overwrite: true);
                moved = true;
            }
            finally
            {
                // 未成功替换（含中途异常）时清理临时文件，确保不残留半成品、且原目标文件保持不变
                if (!moved)
                {
                    TryDeleteTemp(tempPath);
                }
            }
        }

        private static void TryDeleteTemp(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // 临时文件清理失败无害：留待系统临时清理，不掩盖原始异常
            }
        }
    }
}
