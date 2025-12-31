using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MyTool
{
    public partial class MainWindow : Window
    {
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
                var analyzer = new MyTool.ELFAnalyzer.ELFAnalyzer(filePath);

                // 显示ELF头信息
                ELFHeaderInfoTextBlock.Text = analyzer.GetFormattedELFHeaderInfo();

                // 显示程序头信息 - 使用DataGrid
                var programHeaders = analyzer.GetProgramHeaderInfoList();
                ELFProgramHeaderDataGrid.ItemsSource = programHeaders;

                // 显示节头信息 - 使用DataGrid
                var sectionHeaders = analyzer.GetSectionHeaderInfoList();
                ELFSectionHeaderDataGrid.ItemsSource = sectionHeaders;

                // 只显示Section to Segment mapping信息
                ELFSectionToSegmentInfoTextBlock.Text = analyzer.GetSectionToSegmentMappingInfo();

                // 显示符号表信息 - 使用DataGrid
                var symbolTable = analyzer.GetSymbolTableInfoList();
                ELFSymbolTableDataGrid.ItemsSource = symbolTable;

                // 显示动态段信息 - 使用DataGrid
                var dynamicSection = analyzer.GetDynamicSectionInfoList();
                ELFDynamicSectionDataGrid.ItemsSource = dynamicSection;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"分析ELF文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
