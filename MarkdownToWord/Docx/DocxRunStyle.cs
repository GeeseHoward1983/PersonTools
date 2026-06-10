using PersonalTools.MarkdownToWord.Models;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 渲染单个 run 时的格式上下文：一个基准样式行（正文/表格/标题）叠加内联修饰
    /// （加粗/斜体/删除线/行内代码/超链接）。不可变，用 <c>with</c> 派生子状态。
    /// </summary>
    internal readonly struct DocxRunStyle
    {
        public required ContentStyleRow Base { get; init; }
        public bool Bold { get; init; }
        public bool Italic { get; init; }
        public bool Strike { get; init; }
        public bool Code { get; init; }
        public bool Hyperlink { get; init; }

        public static DocxRunStyle For(ContentStyleRow row) => new() { Base = row };

        public DocxRunStyle AsBold() => this with { Bold = true };
        public DocxRunStyle AsItalic() => this with { Italic = true };
        public DocxRunStyle AsStrike() => this with { Strike = true };
        public DocxRunStyle AsCode() => this with { Code = true };
        public DocxRunStyle AsHyperlink() => this with { Hyperlink = true };
    }
}
