using System.Collections.ObjectModel;

namespace PersonalTools.MarkdownToWord.Models
{
    /// <summary>
    /// 导出 Word 的全部样式配置：每个内容类别一行 <see cref="ContentStyleRow"/>，外加全局选项。
    /// 由 UI 双向绑定编辑，由 OOXML 生成层读取，经 <see cref="DocxStyleSettingsStore"/> 持久化。
    /// </summary>
    internal sealed class DocxStyleSettings
    {
        /// <summary>各内容类别的样式行（顺序即 UI 展示顺序）。</summary>
        public ObservableCollection<ContentStyleRow> Rows { get; } = [];

        /// <summary>是否在文档开头生成目录（需求 5，默认开启）。</summary>
        public bool GenerateToc { get; set; } = true;

        /// <summary>取指定类别的样式行；缺失时回退正文行。</summary>
        public ContentStyleRow For(ContentCategory category)
        {
            foreach (ContentStyleRow row in Rows)
            {
                if (row.Category == category)
                {
                    return row;
                }
            }

            // 兜底：理论上 CreateDefault 已覆盖所有类别
            return Rows.Count > 0 ? Rows[0] : new ContentStyleRow(category);
        }

        /// <summary>取某级标题（1–4）的样式行。</summary>
        public ContentStyleRow ForHeading(int level)
        {
            int clamped = Math.Clamp(level, 1, 4);
            return For((ContentCategory)(clamped - 1)); // Heading1==0 … Heading4==3
        }

        /// <summary>
        /// 构造符合需求 3/4/8/9 的默认样式：标题黑体加粗、正文宋体/Times New Roman（首行不缩进，
        /// 可在样式设置中调整）、各级字号二/三/四/五号、表格随正文且小五、目录字体随标题。
        /// </summary>
        public static DocxStyleSettings CreateDefault()
        {
            DocxStyleSettings settings = new();

            settings.Rows.Add(MakeHeading(ContentCategory.Heading1, "二号"));
            settings.Rows.Add(MakeHeading(ContentCategory.Heading2, "三号"));
            settings.Rows.Add(MakeHeading(ContentCategory.Heading3, "四号"));
            settings.Rows.Add(MakeHeading(ContentCategory.Heading4, "五号"));

            settings.Rows.Add(new ContentStyleRow(ContentCategory.Body)
            {
                ChineseFont = "宋体",
                WesternFont = "Times New Roman",
                FontSizeName = "五号",
                Bold = false,
                Underline = false,
                FirstLineIndentChars = 0,
            });

            settings.Rows.Add(new ContentStyleRow(ContentCategory.Table)
            {
                ChineseFont = "宋体",
                WesternFont = "Times New Roman",
                FontSizeName = "小五",
                Bold = false,
                Underline = false,
                FirstLineIndentChars = 0,
            });

            return settings;
        }

        private static ContentStyleRow MakeHeading(ContentCategory category, string sizeName) => new(category)
        {
            ChineseFont = "黑体",
            WesternFont = "Times New Roman",
            FontSizeName = sizeName,
            Bold = true,
            Underline = false,
            FirstLineIndentChars = 0,
        };
    }
}
