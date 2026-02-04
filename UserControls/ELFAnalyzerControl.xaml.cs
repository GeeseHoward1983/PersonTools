using Microsoft.Win32;
using PersonalTools.ELFAnalyzer.UIHelper;
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
                ELFHeaderInfoControl.SetELFHeaderInfo(ELFHeaderHelper.GetFormattedELFHeaderInfo(analyzer._parser));
                
                var interpreter = ProgrameHeaderHelper.GetInterpreterInfo(analyzer._parser);
                if (!string.IsNullOrEmpty(interpreter))
                {
                    ELFHeaderInfoControl.SetInterpreterInfo(interpreter);
                }

                // 显示程序头信息
                var programHeaders = ProgrameHeaderHelper.GetProgramHeaderInfoList(analyzer._parser);
                ELFProgramHeaderControl.SetProgramHeadersData(programHeaders);

                // 显示节头信息
                var sectionHeaders = SectionHeaderHelper.GetSectionHeaderInfoList(analyzer._parser);
                ELFSectionHeaderControl.SetSectionHeadersData(sectionHeaders);

                // 显示节到段映射信息
                ELFSectionToSegmentMappingControl.SetSectionToSegmentInfo(ProgrameHeaderHelper.GetSectionToSegmentMappingInfo(analyzer._parser));

                // 显示符号表信息
                var symbolTable = SymbolTableHelper.GetSymbolTableInfoList(analyzer._parser, SectionType.SHT_SYMTAB);
                ELFSymbolTableControl.SetSymbolTableData(symbolTable);
                ELFSymbolTableTabItem.Visibility = symbolTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                // 显示动态符号表信息
                var dynsymTable = SymbolTableHelper.GetSymbolTableInfoList(analyzer._parser, SectionType.SHT_DYNSYM);
                ELFDynsymControl.SetDynsymData(dynsymTable);
                ELFDynsymTabItem.Visibility = dynsymTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                // 显示动态段信息
                var dynamicSection = DynamicHelper.GetDynamicSectionInfoList(analyzer._parser);
                ELFDynamicSectionControl.SetDynamicSectionData(dynamicSection);
                ELFDynamicSectionTabItem.Visibility = dynamicSection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                // 显示版本符号信息
                ELFVersionSymbolInfoControl.SetVersionSymbolInfo(analyzer.GetFormattedVersionSymbolInfo());

                // 显示版本依赖信息
                ELFVersionDependencyInfoControl.SetVersionDependencyInfo(analyzer.GetFormattedVersionDefinitionInfo() + "\n\n" + analyzer.GetFormattedVersionDependencyInfo());

                // 显示重定位信息
                var relaDynTable = RelocationHelper.GetRelocationInfoForSpecificSection(analyzer._parser, ".rela.dyn");
                var relDynTable = RelocationHelper.GetRelocationInfoForSpecificSection(analyzer._parser, ".rel.dyn");
                relaDynTable.AddRange(relDynTable);
                ELFRelocationControl.SetRelaDynData(relaDynTable);
                ELFRelaDynTabItem.Visibility = relaDynTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                // 显示plt重定位信息
                var relaPltTable = RelocationHelper.GetRelocationInfoForSpecificSection(analyzer._parser, ".rela.plt");
                var relPltTable = RelocationHelper.GetRelocationInfoForSpecificSection(analyzer._parser, ".rel.plt");
                relaPltTable.AddRange(relPltTable);
                ELFPltRelocationControl.SetRelaPltData(relaPltTable);
                ELFRelaPltTabItem.Visibility = relaPltTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                
                // 显示note信息
                var noteInfo = analyzer.GetFormattedNotesInfo();
                ELFNoteInfoControl.SetNoteInfo(noteInfo);
                
                // 显示属性信息
                var attributeInfo = AttributesHelper.GetAttributeInfo(analyzer._parser);
                ELFAttributeInfoControl.SetAttributeInfo(attributeInfo);
                ELFAttributeInfoTabItem.Visibility = attributeInfo.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
                
                // 显示Exidx信息
                var exidxInfo = ExidxInfoHelper.GetExidxInfo(analyzer._parser);
                ELFExidxInfoControl.SetExidxInfo(exidxInfo);
                ELFExidxInfoTabItem.Visibility = !exidxInfo.Contains("There are no exception index entries") ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"分析ELF文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}