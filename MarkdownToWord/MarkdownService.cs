using System.IO;
using Markdig;
using Markdig.Syntax;

namespace PersonalTools.MarkdownToWord
{
    /// <summary>
    /// Markdown 解析与预览的统一入口：共用一个启用 GFM 扩展（含管道表格）的 Markdig 管道，
    /// 供导出 docx（取 AST）与右侧 WebView2 预览（取 HTML）复用。
    /// </summary>
    internal static class MarkdownService
    {
        // 共享管道：UseAdvancedExtensions 启用管道/网格表格、自动链接、任务列表等常见扩展
        private static readonly MarkdownPipeline Pipeline =
            new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        /// <summary>把 Markdown 文本解析为 Markdig AST，供 OOXML 生成层遍历。</summary>
        public static MarkdownDocument Parse(string markdown)
        {
            return Markdown.Parse(markdown ?? string.Empty, Pipeline);
        }

        /// <summary>
        /// 生成可在 WebView2 中显示的完整预览 HTML（标准 Markdown 渲染）。
        /// 当 <paramref name="baseDir"/> 非空时注入 <c>&lt;base href&gt;</c>，使相对图片路径
        /// 相对 Markdown 文件所在目录解析（预览以 file:// 临时文件方式加载，故本地图片可正常显示）。
        /// </summary>
        public static string BuildPreviewHtml(string markdown, string? baseDir)
        {
            string body = Markdown.ToHtml(markdown ?? string.Empty, Pipeline);
            string baseTag = BuildBaseTag(baseDir);

            return PreviewTemplate
                .Replace("%%BASE%%", baseTag, StringComparison.Ordinal)
                .Replace("%%BODY%%", body, StringComparison.Ordinal);
        }

        // 预览 HTML 模板（%%BASE%%/%%BODY%% 为占位符，避免与 CSS 大括号冲突）
        private const string PreviewTemplate = """
            <!DOCTYPE html>
            <html lang="zh-CN">
            <head>
            <meta charset="utf-8">
            %%BASE%%
            <style>
              body { font-family: "宋体", SimSun, serif; font-size: 16px; line-height: 1.7; margin: 24px; color: #222; }
              h1,h2,h3,h4,h5,h6 { font-family: "黑体", SimHei, sans-serif; line-height: 1.4; margin: 1.2em 0 .6em; }
              h1 { font-size: 2em; } h2 { font-size: 1.6em; } h3 { font-size: 1.3em; } h4 { font-size: 1.1em; }
              code { font-family: Consolas, "Courier New", monospace; background: #f3f3f3; padding: .1em .3em; border-radius: 3px; }
              pre { background: #f6f8fa; padding: 12px; border-radius: 6px; overflow: auto; }
              pre code { background: none; padding: 0; }
              table { border-collapse: collapse; margin: 1em 0; }
              th, td { border: 1px solid #bbb; padding: 6px 10px; }
              th { background: #f0f0f0; }
              blockquote { border-left: 4px solid #ddd; margin: 1em 0; padding: .2em 1em; color: #555; }
              img { max-width: 100%; }
            </style>
            </head>
            <body>
            %%BODY%%
            </body>
            </html>
            """;

        private static string BuildBaseTag(string? baseDir)
        {
            if (string.IsNullOrEmpty(baseDir))
            {
                return string.Empty;
            }

            try
            {
                // 末尾补分隔符，确保 base 作为目录解析相对路径
                string withSep = baseDir.EndsWith(Path.DirectorySeparatorChar) ? baseDir : baseDir + Path.DirectorySeparatorChar;
                return $"<base href=\"{new Uri(withSep).AbsoluteUri}\">";
            }
            catch (UriFormatException)
            {
                return string.Empty;
            }
        }
    }
}
