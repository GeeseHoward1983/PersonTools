namespace PersonalTools.MarkdownToWord.Models
{
    /// <summary>
    /// 导出样式的可选项与「中文字号 → OOXML 半磅值」映射（数据驱动，供 UI 下拉框与 OOXML 生成共用）。
    /// OOXML 的 <c>w:sz</c>/<c>w:szCs</c> 以半磅为单位（值 = 磅 × 2）。
    /// </summary>
    internal static class DocxStyleOptions
    {
        /// <summary>UI 下拉可选的中文字体。</summary>
        public static IReadOnlyList<string> ChineseFonts { get; } =
            ["宋体", "黑体", "微软雅黑", "楷体", "仿宋", "等线", "华文中宋"];

        /// <summary>UI 下拉可选的西文字体。</summary>
        public static IReadOnlyList<string> WesternFonts { get; } =
            ["Times New Roman", "Arial", "Calibri", "Cambria", "Consolas", "Georgia"];

        /// <summary>UI 下拉可选的中文字号名（按从大到小排列）。</summary>
        public static IReadOnlyList<string> FontSizeNames { get; } =
            ["二号", "三号", "四号", "小四", "五号", "小五"];

        // 中文字号 → 半磅值：初号=42pt(封面专用)、二号=22pt、三号=16pt、四号=14pt、小四=12pt、五号=10.5pt、小五=9pt。
        private static readonly Dictionary<string, int> NameToHalfPoint = new(StringComparer.Ordinal)
        {
            ["初号"] = 84,
            ["二号"] = 44,
            ["三号"] = 32,
            ["四号"] = 28,
            ["小四"] = 24,
            ["五号"] = 21,
            ["小五"] = 18,
        };

        private const int DefaultHalfPoint = 21; // 五号

        /// <summary>把中文字号名解析为 OOXML 半磅值；未知名回退五号(21)。</summary>
        public static int HalfPoint(string? fontSizeName)
        {
            return fontSizeName != null && NameToHalfPoint.TryGetValue(fontSizeName, out int v) ? v : DefaultHalfPoint;
        }
    }
}
