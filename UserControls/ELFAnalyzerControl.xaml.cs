using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.ELFAnalyzer.UIHelper;
using PersonalTools.Enums;
using System.Windows;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
#pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class ELFAnalyzerControl : UserControl, IFileAnalyzerView
    {
#pragma warning restore CA1515
        public ELFAnalyzerControl()
        {
            InitializeComponent();
        }

        private void Grid_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>拖入文件时回调宿主，由宿主按完整路径决定新建/覆盖 tab。</summary>
        public Action<IReadOnlyList<string>>? FilesDropped { get; set; }

        private void ELFAnalyzerTab_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    e.Handled = true; // 阻止冒泡到宿主，避免重复处理
                    FilesDropped?.Invoke(files);
                }
            }
        }

        private void SetELFHeaderInfo(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            ELFHeaderInfoControl.SetELFHeaderInfo(ELFHeaderHelper.GetFormattedELFHeaderInfo(analyzer.Parser));
        }

        private void SetInterpreterInfo(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            string interpreter = ProgrameHeaderHelper.GetInterpreterInfo(analyzer.Parser);
            if (!string.IsNullOrEmpty(interpreter))
            {
                ELFHeaderInfoControl.SetInterpreterInfo(interpreter);
            }
        }

        private void SetProgramHeadersData(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            List<ProgramHeaderInfo> programHeaders = ProgrameHeaderHelper.GetProgramHeaderInfoList(analyzer.Parser);
            ELFProgramHeaderControl.SetProgramHeadersData(programHeaders);
        }

        private void SetSectionHeadersData(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            List<ELFSectionHeaderInfo> sectionHeaders = SectionHeaderHelper.GetSectionHeaderInfoList(analyzer.Parser);
            ELFSectionHeaderControl.SetSectionHeadersData(sectionHeaders);
        }

        private void SetSectionToSegmentInfo(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            ELFSectionToSegmentMappingControl.SetSectionToSegmentInfo(ProgrameHeaderHelper.GetSectionToSegmentMappingInfo(analyzer.Parser));
        }

        private void SetSymbolTableData(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            List<ELFSymbolTableInfo> symbolTable = SymbolTableHelper.GetSymbolTableInfoList(analyzer.Parser, SectionType.SHT_SYMTAB);
            ELFSymbolTableControl.SetSymbolTableData(symbolTable);
            ELFSymbolTableTabItem.Visibility = symbolTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetDynamicSymbolTableData(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            List<ELFSymbolTableInfo> dynsymTable = SymbolTableHelper.GetSymbolTableInfoList(analyzer.Parser, SectionType.SHT_DYNSYM);
            ELFDynsymControl.SetDynsymData(dynsymTable);
            ELFDynsymTabItem.Visibility = dynsymTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetDynamicSectionData(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            List<ELFDynamicSectionInfo> dynamicSection = DynamicHelper.GetDynamicSectionInfoList(analyzer.Parser);
            ELFDynamicSectionControl.SetDynamicSectionData(dynamicSection);
            ELFDynamicSectionTabItem.Visibility = dynamicSection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetVersionSymbolInfo(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            ELFVersionSymbolInfoControl.SetVersionSymbolInfo(analyzer.GetFormattedVersionSymbolInfo());
        }

        private void SetVersionDependencyInfo(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            ELFVersionDependencyInfoControl.SetVersionDependencyInfo(analyzer.GetFormattedVersionDependencyInfo());
        }

        private void SetRelaDynData(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            List<ELFRelocationInfo> relaDynTable = RelocationHelper.GetRelocationInfoForSpecificSection(analyzer.Parser, ".rela.dyn");
            List<ELFRelocationInfo> relDynTable = RelocationHelper.GetRelocationInfoForSpecificSection(analyzer.Parser, ".rel.dyn");
            relaDynTable.AddRange(relDynTable);
            ELFRelocationControl.SetRelaDynData(relaDynTable);
            ELFRelaDynTabItem.Visibility = relaDynTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetRelaPltData(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            List<ELFRelocationInfo> relaPltTable = RelocationHelper.GetRelocationInfoForSpecificSection(analyzer.Parser, ".rela.plt");
            List<ELFRelocationInfo> relPltTable = RelocationHelper.GetRelocationInfoForSpecificSection(analyzer.Parser, ".rel.plt");
            relaPltTable.AddRange(relPltTable);
            ELFPltRelocationControl.SetRelaPltData(relaPltTable);
            ELFRelaPltTabItem.Visibility = relaPltTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetGotData(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            List<ELFGotInfo> gotPltTable = GotHelper.GetGotInfoList(analyzer.Parser, ".got.plt");
            ELFGotPltControl.SetGotData(gotPltTable, "GOT.PLT 表 (.got.plt)");
            ELFGotPltTabItem.Visibility = gotPltTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            List<ELFGotInfo> gotTable = GotHelper.GetGotInfoList(analyzer.Parser, ".got");
            ELFGotControl.SetGotData(gotTable, "GOT 表 (.got)");
            ELFGotTabItem.Visibility = gotTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetNoteInfo(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            string noteInfo = analyzer.GetFormattedNotesInfo();
            ELFNoteInfoControl.SetNoteInfo(noteInfo);
            ELFNoteInfoTabItem.Visibility = noteInfo.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetAttributeInfo(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            string attributeInfo = AttributesHelper.GetAttributeInfo(analyzer.Parser);
            ELFAttributeInfoControl.SetAttributeInfo(attributeInfo);
            ELFAttributeInfoTabItem.Visibility = attributeInfo.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetExidxInfo(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            string exidxInfo = ExidxInfoHelper.GetExidxInfo(analyzer.Parser);
            ELFExidxInfoControl.SetExidxInfo(exidxInfo);
            ELFExidxInfoTabItem.Visibility = !exidxInfo.Contains("There are no exception index entries", StringComparison.CurrentCulture) ? Visibility.Visible : Visibility.Collapsed;
        }

        // IFileAnalyzerView：供宿主统一调用
        public void LoadFile(string filePath) => AnalyzeELFFile(filePath);

        public void AnalyzeELFFile(string filePath)
        {
            try
            {
                ELFAnalyzer.ELFAnalyzer analyzer = new(filePath);

                // 更新各控件的信息
                SetELFHeaderInfo(analyzer);
                SetInterpreterInfo(analyzer);
                // 显示程序头信息
                SetProgramHeadersData(analyzer);
                // 显示节头信息
                SetSectionHeadersData(analyzer);
                // 显示节到段映射信息
                SetSectionToSegmentInfo(analyzer);
                // 显示符号表信息
                SetSymbolTableData(analyzer);
                // 显示动态符号表信息
                SetDynamicSymbolTableData(analyzer);
                // 显示动态段信息
                SetDynamicSectionData(analyzer);
                // 显示版本符号信息
                SetVersionSymbolInfo(analyzer);
                // 显示版本依赖信息
                SetVersionDependencyInfo(analyzer);
                // 显示重定位信息
                SetRelaDynData(analyzer);
                // 显示plt重定位信息
                SetRelaPltData(analyzer);
                // 显示GOT信息
                SetGotData(analyzer);
                // 显示note信息
                SetNoteInfo(analyzer);
                // 显示属性信息
                SetAttributeInfo(analyzer);
                // 显示Exidx信息
                SetExidxInfo(analyzer);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"分析ELF文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"分析ELF文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}