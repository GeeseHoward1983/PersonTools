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

                // 更新各控件的信息
                ELFHeaderInfoControl.SetELFHeaderInfo(analyzer.GetFormattedELFHeaderInfo());
                
                var interpreter = analyzer.GetInterpreterInfo();
                if (!string.IsNullOrEmpty(interpreter))
                {
                    ELFHeaderInfoControl.SetInterpreterInfo(interpreter);
                }

                // 显示程序头信息
                var programHeaders = analyzer.GetProgramHeaderInfoList();
                ELFProgramHeaderControl.SetProgramHeadersData(programHeaders);

                // 显示节头信息
                var sectionHeaders = analyzer.GetSectionHeaderInfoList();
                ELFSectionHeaderControl.SetSectionHeadersData(sectionHeaders);

                // 显示节到段映射信息
                ELFSectionToSegmentMappingControl.SetSectionToSegmentInfo(analyzer.GetSectionToSegmentMappingInfo());

                // 显示符号表信息
                var symbolTable = analyzer.GetSymbolTableInfoList(SectionType.SHT_SYMTAB);
                ELFSymbolTableControl.SetSymbolTableData(symbolTable);
                ELFSymbolTableTabItem.Visibility = symbolTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                // 显示动态符号表信息
                var dynsymTable = analyzer.GetSymbolTableInfoList(SectionType.SHT_DYNSYM);
                ELFDynsymControl.SetDynsymData(dynsymTable);
                ELFDynsymTabItem.Visibility = dynsymTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                // 显示动态段信息
                var dynamicSection = analyzer.GetDynamicSectionInfoList();
                ELFDynamicSectionControl.SetDynamicSectionData(dynamicSection);
                ELFDynamicSectionTabItem.Visibility = dynamicSection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                // 显示版本符号信息
                ELFVersionSymbolInfoControl.SetVersionSymbolInfo(analyzer.GetFormattedVersionSymbolInfo());

                // 显示版本依赖信息
                ELFVersionDependencyInfoControl.SetVersionDependencyInfo(analyzer.GetFormattedVersionDependencyInfo());

                // 显示重定位信息
                var relaDynTable = analyzer.GetRelocationInfoForSpecificSection(".rela.dyn");
                var relDynTable = analyzer.GetRelocationInfoForSpecificSection(".rel.dyn");
                relaDynTable.AddRange(relDynTable);
                ELFRelocationControl.SetRelaDynData(relaDynTable);
                ELFRelaDynTabItem.Visibility = relaDynTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                // 显示plt重定位信息
                var relaPltTable = analyzer.GetRelocationInfoForSpecificSection(".rela.plt");
                var relPltTable = analyzer.GetRelocationInfoForSpecificSection(".rel.plt");
                relaPltTable.AddRange(relPltTable);
                ELFPltRelocationControl.SetRelaPltData(relaPltTable);
                ELFRelaPltTabItem.Visibility = relaPltTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"分析ELF文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}