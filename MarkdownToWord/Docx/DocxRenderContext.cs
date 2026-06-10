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

        private uint drawingId;
        private int bookmarkId;

        /// <summary>图片绘图对象的唯一 wp:docPr Id（从 1 开始）。</summary>
        public uint NextDrawingId() => ++drawingId;

        /// <summary>书签的唯一数值 Id（从 0 开始）。</summary>
        public int NextBookmarkId() => bookmarkId++;
    }
}
