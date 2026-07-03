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
            int totalColumns = GetColumnCount(mdTable);
            // 逐行重建网格几何：跟踪每个网格列上尚未结束的纵向合并(RowSpan)，为其在后续行补 vMerge=Continue 单元格；
            // 单元格 ColumnSpan>1 出 gridSpan。Markdig 的行/列跨在续行是稀疏的（被跨越的列在续行缺席），故须按占用推进列号，
            // 否则每个源单元格只出一个无 gridSpan 的 w:tc，合并单元格丢失、行列错位。
            int[] rowSpanRemaining = new int[totalColumns];
            int[] rowSpanWidth = new int[totalColumns];
            foreach (object rowObj in mdTable)
            {
                if (rowObj is not MTableRow mdRow)
                {
                    continue;
                }

                DocxRunStyle cellStyle = mdRow.IsHeader ? DocxRunStyle.For(tableStyle).AsBold() : DocxRunStyle.For(tableStyle);
                List<MTableCell> mdCells = [];
                foreach (object cellObj in mdRow)
                {
                    if (cellObj is MTableCell cell)
                    {
                        mdCells.Add(cell);
                    }
                }

                TableRow row = new();
                int col = 0;
                int cellIdx = 0;
                while (col < totalColumns)
                {
                    if (rowSpanRemaining[col] > 0)
                    {
                        // 该列有来自上方的纵向合并延续：补一个 vMerge=Continue 单元格（含相同 gridSpan 宽度）
                        int span = Math.Clamp(rowSpanWidth[col], 1, totalColumns - col);
                        row.AppendChild(BuildContinuationCell(span, mdRow.IsHeader));
                        rowSpanRemaining[col]--;
                        col += span;
                        continue;
                    }

                    if (cellIdx >= mdCells.Count)
                    {
                        // 源单元格用尽但网格还有列（稀疏行）：补空单元格填满该行，保持列对齐
                        row.AppendChild(BuildEmptyCell(mdRow.IsHeader));
                        col++;
                        continue;
                    }

                    MTableCell mdCell = mdCells[cellIdx++];
                    int columnSpan = Math.Clamp(mdCell.ColumnSpan, 1, totalColumns - col);
                    int rowSpan = Math.Max(1, mdCell.RowSpan);
                    row.AppendChild(BuildCell(mdCell, cellStyle, mdRow.IsHeader, ctx, indentLevel, columnSpan, rowSpan > 1));
                    if (rowSpan > 1)
                    {
                        rowSpanRemaining[col] = rowSpan - 1;
                        rowSpanWidth[col] = columnSpan;
                    }

                    col += columnSpan;
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
            int columns = GetColumnCount(mdTable);

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

        // 网格总列数：grid table 以 ColumnDefinitions 为准（已含列跨布局），管道表回退按行内单元格数取最大值。
        // 供 BuildTableGrid 的列定义与 Render 的合并单元格几何重建共用同一列数，避免 gridSpan/vMerge 与 tblGrid 不一致。
        private static int GetColumnCount(MTable mdTable)
        {
            return mdTable.ColumnDefinitions.Count switch
            {
                <= 0 => CountColumns(mdTable),
                _ => mdTable.ColumnDefinitions.Count,
            };
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

        private static TableCell BuildCell(MTableCell mdCell, DocxRunStyle style, bool isHeader, DocxRenderContext ctx, int indentLevel, int columnSpan, bool verticalMergeRestart)
        {
            TableCell cell = new();

            // tcPr 子元素须按 OOXML 架构顺序：gridSpan → vMerge → shd → vAlign
            TableCellProperties props = new();
            if (columnSpan > 1)
            {
                props.AppendChild(new GridSpan { Val = columnSpan });
            }

            if (verticalMergeRestart)
            {
                props.AppendChild(new VerticalMerge { Val = MergedCellValues.Restart });
            }

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

        // 纵向合并延续单元格（vMerge=Continue）：内容由合并起始单元格显示，此处仅占位保持列对齐
        private static TableCell BuildContinuationCell(int columnSpan, bool isHeader)
        {
            TableCellProperties props = new();
            if (columnSpan > 1)
            {
                props.AppendChild(new GridSpan { Val = columnSpan });
            }

            props.AppendChild(new VerticalMerge { Val = MergedCellValues.Continue });
            if (isHeader)
            {
                props.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "F0F0F0" });
            }

            props.AppendChild(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
            return new TableCell(props, new Paragraph());
        }

        // 稀疏行的填充空单元格：源单元格用尽但网格仍有列时补齐，保持整表列对齐
        private static TableCell BuildEmptyCell(bool isHeader)
        {
            TableCellProperties props = new();
            if (isHeader)
            {
                props.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "F0F0F0" });
            }

            props.AppendChild(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center });
            return new TableCell(props, new Paragraph());
        }
    }
}
