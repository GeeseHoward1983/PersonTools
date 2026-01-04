using Microsoft.Win32;
using PersonalTools.ELFAnalyzer;
using PersonalTools.ELFAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Text;
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

        private void Grid_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
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
                var analyzer = new PersonalTools.ELFAnalyzer.ELFAnalyzer(filePath);

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
                var symbolTable = analyzer.GetSymbolTableInfoList(SectionType.SHT_SYMTAB);
                ELFSymbolTableDataGrid.ItemsSource = symbolTable;

                // 显示动态符号表信息 - 使用DataGrid
                var dynsymTable = analyzer.GetSymbolTableInfoList(SectionType.SHT_DYNSYM);
                ELFDynsymDataGrid.ItemsSource = dynsymTable;


                // 显示动态段信息 - 使用DataGrid
                var dynamicSection = analyzer.GetDynamicSectionInfoList();
                ELFDynamicSectionDataGrid.ItemsSource = dynamicSection;
                
                // 显示版本符号信息
                ELFVersionSymbolInfoTextBlock.Text = analyzer.GetFormattedVersionSymbolInfo();
                
                // 显示版本依赖信息
                ELFVersionDependencyInfoTextBlock.Text = analyzer.GetFormattedVersionDependencyInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"分析ELF文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}