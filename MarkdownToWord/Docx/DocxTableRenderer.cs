using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax;
using PersonalTools.MarkdownToWord.Models;
using MTable = Markdig.Extensions.Tables.Table;
using MTableCell = Markdig.Extensions.Tables.TableCell;
using MTableRow = Markdig.Extensions.Tables.TableRow;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 把 Markdig 表格渲染为 OOXML 表格：表前插入「表 N」题注，单元格字体随「表格」类别样式
    /// （默认正文字体、小五），表头行加粗并浅底纹（需求 9/10）。
    /// </summary>
    internal static class DocxTableRenderer
    {
        public static void Render(MTable mdTable, OpenXmlElement container, DocxRenderContext ctx, int indentLevel)
        {
            // 表格是递归入口：单元格可含块级内容乃至嵌套 grid table。深度由调用链沿 indentLevel 传入，
            // 与 DocxBlockRenderer 共用同一 MaxNestingDepth 守卫；超限直接返回，防嵌套表格栈溢出。
            if (indentLevel > DocxBlockRenderer.MaxNestingDepth)
            {
                return;
            }

            container.AppendChild(DocxCaptionBuilder.BuildTableCaption(ctx));

            Table table = new();
            table.AppendChild(BuildTableProperties());
            table.AppendChild(BuildTableGrid(mdTable));

            ContentStyleRow tableStyle = ctx.Settings.For(ContentCategory.Table);
            foreach (object rowObj in mdTable)
            {
                if (rowObj is not MTableRow mdRow)
                {
                    continue;
                }

                DocxRunStyle cellStyle = mdRow.IsHeader ? DocxRunStyle.For(tableStyle).AsBold() : DocxRunStyle.For(tableStyle);
                TableRow row = new();
                foreach (object cellObj in mdRow)
                {
                    if (cellObj is MTableCell mdCell)
                    {
                        row.AppendChild(BuildCell(mdCell, cellStyle, mdRow.IsHeader, ctx, indentLevel));
                    }
                }

                table.AppendChild(row);
            }

            container.AppendChild(table);
            container.AppendChild(new Paragraph()); // OOXML 规则：表格后须有段落
        }

        private static TableProperties BuildTableProperties()
        {            return new TableProperties(
                new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
                    new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
                    new RightBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "999999" },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "999999" }),
                new TableLayout { Type = TableLayoutValues.Autofit });
        }

        // tblGrid（列定义）是 OOXML 表格 tblPr 之后、行之前的必需元素；按列数平分正文宽度
        private static TableGrid BuildTableGrid(MTable mdTable)
        {
            int columns = mdTable.ColumnDefinitions.Count switch
            {
                <= 0 => CountColumns(mdTable),
                _ => mdTable.ColumnDefinitions.Count,
            };

            const int contentWidthTwips = 9026; // A4 正文宽度（页宽 - 左右边距）
            // 各列等分正文宽度，列宽之和恒 ≤ contentWidthTwips，避免超多列时溢出页面右边距。
            // 不再对单列宽设过大下限（旧 minColumnTwips=240），否则列数过多时 240*列数 会超出正文宽度致表格右溢；
            // 仅以 1 twip 兜底防 0 宽列塌陷（Autofit 布局下 Word 仍会按内容回弹，不会真退化为 1 twip）。
            int perColumn = Math.Max(1, contentWidthTwips / columns);
            string columnWidth = perColumn.ToString(CultureInfo.InvariantCulture);

            TableGrid grid = new();
            for (int i = 0; i < columns; i++)
            {
                grid.AppendChild(new GridColumn { Width = columnWidth });
            }

            return grid;
        }

        private static int CountColumns(MTable mdTable)
        {
            int max = 0;
            foreach (object rowObj in mdTable)
            {
                if (rowObj is not MTableRow row)
                {
                    continue;
                }

                int cells = 0;
                foreach (object cell in row)
                {
                    if (cell is MTableCell)
                    {
                        cells++;
                    }
                }

                max = Math.Max(max, cells);
            }

            return max switch
            {
                0 => 1,
                _ => max
            };
        }

        private static TableCell BuildCell(MTableCell mdCell, DocxRunStyle style, bool isHeader, DocxRenderContext ctx, int indentLevel)
        {
            TableCell cell = new();

            TableCellProperties props = new();
            if (isHeader)
            {
                props.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "F0F0F0" });
            }

            props.AppendChild(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
            cell.AppendChild(props);

            foreach (Block block in mdCell)
            {
                if (block is ParagraphBlock paragraph)
                {
                    Paragraph cellParagraph = new(new ParagraphProperties(
                        new SpacingBetweenLines { After = "0" },
                        new Indentation { FirstLine = "0", FirstLineChars = 0, Left = "0", LeftChars = 0 }));
                    DocxInlineRenderer.RenderInlines(paragraph.Inline, cellParagraph, style, ctx);
                    cell.AppendChild(cellParagraph);
                }
                else
                {
                    // 单元格内的非段落块（含嵌套表格/列表/引用）须把深度沿调用链递增传入，不再重置为 0，
                    // 否则每层单元格都重置深度会让 MaxNestingDepth 守卫失效 → 嵌套 grid table 栈溢出。
                    DocxBlockRenderer.RenderBlock(block, cell, ctx, indentLevel + 1);
                }
            }

            // 单元格至少要有一个段落，且最后一个块级元素须为段落
            if (cell.LastChild is not Paragraph)
            {
                cell.AppendChild(new Paragraph());
            }

            return cell;
        }
    }
}
