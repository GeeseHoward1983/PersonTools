using DocumentFormat.OpenXml.Packaging;
using PersonalTools.MarkdownToWord.Models;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 一次导出过程中的共享状态：主文档部件（用于挂图片/超链接关系）、样式配置、
    /// Markdown 文件所在目录（解析本地图片相对路径），以及图片/书签的自增唯一 ID。
    /// </summary>
    internal sealed class DocxRenderContext
    {
        public DocxRenderContext(MainDocumentPart mainPart, DocxStyleSettings settings, string? baseDir)
        {
            MainPart = mainPart;
            Settings = settings;
            BaseDir = baseDir;
        }

        public MainDocumentPart MainPart { get; }
        public DocxStyleSettings Settings { get; }
        public string? BaseDir { get; }

        /// <summary>是否已渲染过首个一级标题（首章前不插分页，其后每章插分页，需求 3）。</summary>
        public bool FirstChapterRendered { get; set; }

        /// <summary>
        /// 本次导出已成功嵌入图片的累计字节数。单图有 50MB 上限，但多图叠加无总预算时仍可能
        /// 撑爆内存/产出超大文档；嵌入器据此对单次导出施加总字节预算，超限后续图片降级为占位文字。
        /// </summary>
        public long EmbeddedImageBytes { get; private set; }

        /// <summary>累加一次成功嵌入图片的字节数到本次导出的总预算计量。</summary>
        public void AddEmbeddedImageBytes(long bytes) => EmbeddedImageBytes += bytes;

        private uint drawingId;
        private int bookmarkId;

        /// <summary>图片绘图对象的唯一 wp:docPr Id（从 1 开始）。</summary>
        public uint NextDrawingId() => ++drawingId;

        /// <summary>书签的唯一数值 Id（从 0 开始）。</summary>
        public int NextBookmarkId() => bookmarkId++;
    }
}
