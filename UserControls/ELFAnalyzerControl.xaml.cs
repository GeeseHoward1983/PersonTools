using Microsoft.Win32;
using PersonalTools.Enums;
using System.Windows;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    public partial class ELFAnalyzerControl : UserControl
    {
        public ELFAnalyzerControl()
        {
            InitializeComponent();
        }

        private void Grid_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void OpenELFFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Executable and Linkable Format files (*.elf)|*.elf|All files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                AnalyzeELFFile(openFileDialog.FileName);
            }
        }

        private void ELFAnalyzerTab_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    AnalyzeELFFile(files[0]);
                }
            }
        }

        private void AnalyzeELFFile(string filePath)
        {
            try
            {
                var analyzer = new ELFAnalyzer.ELFAnalyzer(filePath);

                // 显示ELF头信息
                ELFHeaderInfoTextBlock.Text = analyzer.GetFormattedELFHeaderInfo();

                // 获取并显示解释器信息（如果存在）
                var interpreter = analyzer.GetInterpreterInfo();
                if (!string.IsNullOrEmpty(interpreter))
                {
                    ELFHeaderInfoTextBlock.Text += $"\n\nInterpreter:\n{interpreter}\n";
                }

                // 显示程序头信息 - 使用DataGrid
                var programHeaders = analyzer.GetProgramHeaderInfoList();
                ELFProgramHeaderDataGrid.ItemsSource = programHeaders;

                // 显示节头信息 - 使用DataGrid
                var sectionHeaders = analyzer.GetSectionHeaderInfoList();
                ELFSectionHeaderDataGrid.ItemsSource = sectionHeaders;

                // 只显示Section to Segment mapping信息
                ELFSectionToSegmentInfoTextBlock.Text = analyzer.GetSectionToSegmentMappingInfo();

                // 显示符号表信息 - 使用DataGrid
                var symbolTable = analyzer.GetSymbolTableInfoList(SectionType.SHT_SYMTAB);
                ELFSymbolTableDataGrid.ItemsSource = symbolTable;
                if(symbolTable.Count > 0)
                {
                    ELFSymbolTableTabItem.Visibility = Visibility.Visible;
                }
                else
                {
                    ELFSymbolTableTabItem.Visibility = Visibility.Collapsed;
                }
                
                // 显示动态符号表信息 - 使用DataGrid
                var dynsymTable = analyzer.GetSymbolTableInfoList(SectionType.SHT_DYNSYM);
                ELFDynsymDataGrid.ItemsSource = dynsymTable;
                if(dynsymTable.Count > 0)
                {
                    ELFDynsymTabItem.Visibility = Visibility.Visible;
                }
                else
                {
                    ELFDynsymTabItem.Visibility = Visibility.Collapsed;
                }

                // 显示动态段信息 - 使用DataGrid
                var dynamicSection = analyzer.GetDynamicSectionInfoList();
                ELFDynamicSectionDataGrid.ItemsSource = dynamicSection;
                if(dynamicSection.Count > 0)
                {
                    ELFDynamicSectionTabItem.Visibility = Visibility.Visible;
                }
                else
                {
                    ELFDynamicSectionTabItem.Visibility = Visibility.Collapsed;
                }
                
                // 显示版本符号信息
                ELFVersionSymbolInfoTextBlock.Text = analyzer.GetFormattedVersionSymbolInfo();
                
                // 显示版本依赖信息
                ELFVersionDependencyInfoTextBlock.Text = analyzer.GetFormattedVersionDependencyInfo();
                
                // 显示重定位信息
                var relaDynTable = analyzer.GetRelocationInfoForSpecificSection(".rela.dyn");
                ELFRelaDynDataGrid.ItemsSource = relaDynTable;
                var relDynTable = analyzer.GetRelocationInfoForSpecificSection(".rel.dyn");
                relaDynTable.AddRange(relDynTable);
                if (relaDynTable.Count > 0)
                    ELFRelaDynTabItem.Visibility = Visibility.Visible;
                else
                    ELFRelaDynTabItem.Visibility = Visibility.Collapsed;

                // 显示plt重定位信息
                var relaPltTable = analyzer.GetRelocationInfoForSpecificSection(".rela.plt");
                ELFRelaPltDataGrid.ItemsSource = relaPltTable;
                var relPltTable = analyzer.GetRelocationInfoForSpecificSection(".rel.plt");
                relaPltTable.AddRange(relPltTable);
                if (relaPltTable.Count > 0)
                    ELFRelaPltTabItem.Visibility = Visibility.Visible;
                else
                    ELFRelaPltTabItem.Visibility = Visibility.Collapsed;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"分析ELF文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}