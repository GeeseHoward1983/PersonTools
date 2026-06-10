using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 生成多级标题自动编号定义（numbering.xml）：一个 4 级 <c>abstractNum</c>，
    /// 每级用 <c>w:pStyle</c> 关联 Heading1–4，编号文本为 1. / 1.1. / 1.1.1. / 1.1.1.1.（需求 6/7）。
    /// </summary>
    internal static class DocxNumberingBuilder
    {
        private static readonly string[] LevelTexts =
        [
            "%1.",
            "%1.%2.",
            "%1.%2.%3.",
            "%1.%2.%3.%4.",
        ];

        public static void Build(NumberingDefinitionsPart part)
        {
            AbstractNum abstractNum = new(
                new MultiLevelType { Val = MultiLevelValues.Multilevel },
                MakeLevel(0),
                MakeLevel(1),
                MakeLevel(2),
                MakeLevel(3))
            {
                AbstractNumberId = OoxmlIds.HeadingAbstractNumId,
            };

            NumberingInstance numbering = new(
                new AbstractNumId { Val = OoxmlIds.HeadingAbstractNumId })
            {
                NumberID = OoxmlIds.HeadingNumId,
            };

            part.Numbering = new Numbering(abstractNum, numbering);
        }

        // 子元素顺序遵循 CT_Lvl：start → numFmt → pStyle → suff → lvlText → lvlJc
        private static Level MakeLevel(int ilvl) => new(
            new StartNumberingValue { Val = 1 },
            new NumberingFormat { Val = NumberFormatValues.Decimal },
            new ParagraphStyleIdInLevel { Val = OoxmlIds.HeadingStyleId(ilvl + 1) },
            new LevelSuffix { Val = LevelSuffixValues.Space }, // 编号后用空格而非制表位
            new LevelText { Val = LevelTexts[ilvl] },
            new LevelJustification { Val = LevelJustificationValues.Left })
        {
            LevelIndex = ilvl,
        };
    }
}
