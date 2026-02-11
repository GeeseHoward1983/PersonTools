using Microsoft.Win32;
using PersonalTools.ELFAnalyzer.Models;
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
            OpenFileDialog openFileDialog = new()
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
                ELFAnalyzer.ELFAnalyzer analyzer = new(filePath);

                // 更新各控件的信息
                ELFHeaderInfoControl.SetELFHeaderInfo(ELFHeaderHelper.GetFormattedELFHeaderInfo(analyzer.Parser));

                string interpreter = ProgrameHeaderHelper.GetInterpreterInfo(analyzer.Parser);
                if (!string.IsNullOrEmpty(interpreter))
                {
                    ELFHeaderInfoControl.SetInterpreterInfo(interpreter);
                }

                // 显示程序头信息
                List<ProgramHeaderInfo> programHeaders = ProgrameHeaderHelper.GetProgramHeaderInfoList(analyzer.Parser);
                ELFProgramHeaderControl.SetProgramHeadersData(programHeaders);

                // 显示节头信息
                List<ELFSectionHeaderInfo> sectionHeaders = SectionHeaderHelper.GetSectionHeaderInfoList(analyzer.Parser);
                ELFSectionHeaderControl.SetSectionHeadersData(sectionHeaders);

                // 显示节到段映射信息
                ELFSectionToSegmentMappingControl.SetSectionToSegmentInfo(ProgrameHeaderHelper.GetSectionToSegmentMappingInfo(analyzer.Parser));

                // 显示符号表信息
                List<ELFSymbolTableInfo> symbolTable = SymbolTableHelper.GetSymbolTableInfoList(analyzer.Parser, SectionType.SHT_SYMTAB);
                ELFSymbolTableControl.SetSymbolTableData(symbolTable);
                ELFSymbolTableTabItem.Visibility = symbolTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                // 显示动态符号表信息
                List<ELFSymbolTableInfo> dynsymTable = SymbolTableHelper.GetSymbolTableInfoList(analyzer.Parser, SectionType.SHT_DYNSYM);
                ELFDynsymControl.SetDynsymData(dynsymTable);
                ELFDynsymTabItem.Visibility = dynsymTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                // 显示动态段信息
                List<ELFDynamicSectionInfo> dynamicSection = DynamicHelper.GetDynamicSectionInfoList(analyzer.Parser);
                ELFDynamicSectionControl.SetDynamicSectionData(dynamicSection);
                ELFDynamicSectionTabItem.Visibility = dynamicSection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                // 显示版本符号信息
                ELFVersionSymbolInfoControl.SetVersionSymbolInfo(analyzer.GetFormattedVersionSymbolInfo());

                // 显示版本依赖信息
                ELFVersionDependencyInfoControl.SetVersionDependencyInfo(analyzer.GetFormattedVersionDefinitionInfo() + "\n\n" + analyzer.GetFormattedVersionDependencyInfo());

                // 显示重定位信息
                List<ELFRelocationInfo> relaDynTable = RelocationHelper.GetRelocationInfoForSpecificSection(analyzer.Parser, ".rela.dyn");
                List<ELFRelocationInfo> relDynTable = RelocationHelper.GetRelocationInfoForSpecificSection(analyzer.Parser, ".rel.dyn");
                relaDynTable.AddRange(relDynTable);
                ELFRelocationControl.SetRelaDynData(relaDynTable);
                ELFRelaDynTabItem.Visibility = relaDynTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                // 显示plt重定位信息
                List<ELFRelocationInfo> relaPltTable = RelocationHelper.GetRelocationInfoForSpecificSection(analyzer.Parser, ".rela.plt");
                List<ELFRelocationInfo> relPltTable = RelocationHelper.GetRelocationInfoForSpecificSection(analyzer.Parser, ".rel.plt");
                relaPltTable.AddRange(relPltTable);
                ELFPltRelocationControl.SetRelaPltData(relaPltTable);
                ELFRelaPltTabItem.Visibility = relaPltTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                // 显示note信息
                string noteInfo = analyzer.GetFormattedNotesInfo();
                ELFNoteInfoControl.SetNoteInfo(noteInfo);

                // 显示属性信息
                string attributeInfo = AttributesHelper.GetAttributeInfo(analyzer.Parser);
                ELFAttributeInfoControl.SetAttributeInfo(attributeInfo);
                ELFAttributeInfoTabItem.Visibility = attributeInfo.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

                // 显示Exidx信息
                string exidxInfo = ExidxInfoHelper.GetExidxInfo(analyzer.Parser);
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