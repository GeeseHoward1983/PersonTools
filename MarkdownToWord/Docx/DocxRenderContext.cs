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

        /// <summary>是否已渲染过封面（Markdown 首个一级标题）。封面只渲染一次，其余一级标题降级为 Word 一级标题，避免重复整页封面。</summary>
        public bool CoverRendered { get; set; }

        /// <summary>是否已渲染过首个一级标题（首章前不插分页，其后每章插分页，需求 3）。</summary>
        public bool FirstChapterRendered { get; set; }

        private uint drawingId;
        private int bookmarkId;

        /// <summary>图片绘图对象的唯一 wp:docPr Id（从 1 开始）。</summary>
        public uint NextDrawingId() => ++drawingId;

        /// <summary>书签的唯一数值 Id（从 0 开始）。</summary>
        public int NextBookmarkId() => bookmarkId++;
    }
}
