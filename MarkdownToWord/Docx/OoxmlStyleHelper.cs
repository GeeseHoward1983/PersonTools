using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using PersonalTools.MarkdownToWord.Models;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 把 <see cref="ContentStyleRow"/> 翻译成 OOXML 运行属性/段落缩进的共享原语。
    /// 中西文分槽（ascii/hAnsi=西文、eastAsia=中文）让同一 run 内中英文各用各的字体；
    /// 字号写半磅值（sz/szCs）；首行缩进用字符单位（firstLineChars）并附 twips 回退。
    /// </summary>
    internal static class OoxmlStyleHelper
    {
        /// <summary>构造中西文分槽的字体定义。</summary>
        public static RunFonts BuildFonts(ContentStyleRow row) => new()
        {
            Ascii = row.WesternFont,
            HighAnsi = row.WesternFont,
            EastAsia = row.ChineseFont,
            ComplexScript = row.WesternFont,
        };

        /// <summary>
        /// 按 OOXML 架构顺序（rFonts → b → bCs → sz → szCs → u）把字体/字号/加粗/下划线
        /// 追加到运行属性容器。<paramref name="runProps"/> 可为内联 <see cref="RunProperties"/>
        /// 或样式定义中的 <see cref="StyleRunProperties"/>，二者接受相同子元素。
        /// </summary>
        public static void ApplyRunFormatting(OpenXmlCompositeElement runProps, ContentStyleRow row)
        {
            runProps.AppendChild(BuildFonts(row));

            if (row.Bold)
            {
                runProps.AppendChild(new Bold());
                runProps.AppendChild(new BoldComplexScript());
            }

            string sz = row.HalfPoint.ToString(CultureInfo.InvariantCulture);
            runProps.AppendChild(new FontSize { Val = sz });
            runProps.AppendChild(new FontSizeComplexScript { Val = sz });

            if (row.Underline)
            {
                runProps.AppendChild(new Underline { Val = UnderlineValues.Single });
            }
        }

        /// <summary>
        /// 构造首行缩进（按字符）；字数 ≤ 0 返回 null。firstLineChars 单位为 1/100 字，
        /// 同时给 firstLine(twips ≈ 字数 × 字号磅 × 20) 作非 Word 阅读器的回退。
        /// </summary>
        public static Indentation? BuildFirstLineIndent(ContentStyleRow row)
        {
            double chars = row.FirstLineIndentChars;
            // FirstLineIndentChars 取自可编辑/可损坏的 JSON：NaN(<=0 比较为 false 会漏过)与极大值
            // 会让 *100 / Math.Round 溢出成垃圾缩进。先排除非有限值，再夹到合理上限(99 字)。
            if (double.IsNaN(chars) || double.IsInfinity(chars) || chars <= 0)
            {
                return null;
            }

            chars = Math.Min(chars, 99);
            return new Indentation
            {
                FirstLineChars = (int)Math.Round(chars * 100),
                FirstLine = ((int)Math.Round(chars * row.HalfPoint * 10)).ToString(CultureInfo.InvariantCulture),
            };
        }
    }
}
