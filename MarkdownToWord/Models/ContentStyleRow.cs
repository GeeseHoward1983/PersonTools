using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PersonalTools.MarkdownToWord.Models
{
    /// <summary>
    /// 单个内容类别的可编辑导出样式行：供 <c>DataGrid</c> 双向绑定，并被 OOXML 生成层读取。
    /// 中西文分槽（<see cref="ChineseFont"/>/<see cref="WesternFont"/>）对应同一 run 的 eastAsia/ascii 字体。
    /// </summary>
    internal sealed class ContentStyleRow : INotifyPropertyChanged
    {
        public ContentStyleRow(ContentCategory category)
        {
            Category = category;
        }

        /// <summary>所属内容类别（不参与编辑）。</summary>
        public ContentCategory Category { get; }

        /// <summary>类别中文显示名（供表格首列展示）。</summary>
        public string DisplayName => GetDisplayName(Category);

        private string chineseFont = "宋体";
        public string ChineseFont
        {
            get => chineseFont;
            set => SetField(ref chineseFont, value);
        }

        private string westernFont = "Times New Roman";
        public string WesternFont
        {
            get => westernFont;
            set => SetField(ref westernFont, value);
        }

        private string fontSizeName = "五号";
        public string FontSizeName
        {
            get => fontSizeName;
            set => SetField(ref fontSizeName, value);
        }

        private bool bold;
        public bool Bold
        {
            get => bold;
            set => SetField(ref bold, value);
        }

        private bool underline;
        public bool Underline
        {
            get => underline;
            set => SetField(ref underline, value);
        }

        // 首行缩进字数（汉字数）。0 表示不缩进；正文默认 2。
        private double firstLineIndentChars;
        public double FirstLineIndentChars
        {
            get => firstLineIndentChars;
            set => SetField(ref firstLineIndentChars, value);
        }

        /// <summary>当前字号名对应的 OOXML 半磅值。</summary>
        public int HalfPoint => DocxStyleOptions.HalfPoint(FontSizeName);

        private static string GetDisplayName(ContentCategory category) => category switch
        {
            ContentCategory.Heading1 => "一级标题",
            ContentCategory.Heading2 => "二级标题",
            ContentCategory.Heading3 => "三级标题",
            ContentCategory.Heading4 => "四级标题",
            ContentCategory.Body => "正文",
            ContentCategory.Table => "表格",
            _ => category.ToString(),
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
