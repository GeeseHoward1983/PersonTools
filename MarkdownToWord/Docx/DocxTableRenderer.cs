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
        public static void Render(MTable mdTable, OpenXmlElement container, DocxRenderContext ctx)
        {
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
                        row.AppendChild(BuildCell(mdCell, cellStyle, mdRow.IsHeader, ctx));
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
            string columnWidth = (contentWidthTwips / columns).ToString(CultureInfo.InvariantCulture);

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

        private static TableCell BuildCell(MTableCell mdCell, DocxRunStyle style, bool isHeader, DocxRenderContext ctx)
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
                    DocxBlockRenderer.RenderBlock(block, cell, ctx, 0);
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
