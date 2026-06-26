using PersonalTools.ELFAnalyzer.Models;
using PersonalTools.ELFAnalyzer.UIHelper;
using PersonalTools.Enums;
using PersonalTools.Utils;
using System.IO;
using System.Threading.Tasks;
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
            // 拖入文件时显示“复制”光标反馈，非文件拖放则禁止；与 FileTabHostControl/MarkdownToWordControl 行为一致
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>拖入文件时回调宿主，由宿主按完整路径决定新建/覆盖 tab。</summary>
        public Action<IReadOnlyList<string>>? FilesDropped { get; set; }

        private void ELFAnalyzerTab_Drop(object sender, DragEventArgs e)
        {
            // 某些拖放源声明了 FileDrop 格式但 GetData 返回 null 或非 string[]，直接强转会抛 NRE/InvalidCastException
            if (e.Data.GetDataPresent(DataFormats.FileDrop)
                && e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                e.Handled = true; // 阻止冒泡到宿主，避免重复处理
                FilesDropped?.Invoke(files);
            }
        }

        // 后台线程一次性算出的全部展示数据；UI 线程仅据此赋值控件，避免解析/格式化在 UI 线程卡死，
        // 也避免逐步刷新时中途异常造成"新旧文件混合视图"（全部算完才整体应用）。
        private sealed record ElfDisplayData(
            string HeaderInfo,
            string Interpreter,
            List<ProgramHeaderInfo> ProgramHeaders,
            List<ELFSectionHeaderInfo> SectionHeaders,
            string SectionToSegment,
            List<ELFSymbolTableInfo> SymbolTable,
            List<ELFSymbolTableInfo> DynsymTable,
            List<ELFDynamicSectionInfo> DynamicSection,
            string VersionSymbolInfo,
            string VersionDependencyInfo,
            List<ELFRelocationInfo> RelaDyn,
            List<ELFRelocationInfo> RelaPlt,
            List<ELFGotInfo> GotPlt,
            List<ELFGotInfo> Got,
            string NoteInfo,
            string AttributeInfo,
            string ExidxInfo);

        // 纯计算：全部在后台线程执行，不触碰任何 WPF 控件
        private static ElfDisplayData ComputeDisplayData(ELFAnalyzer.ELFAnalyzer analyzer)
        {
            List<ELFRelocationInfo> relaDyn = RelocationHelper.GetRelocationInfoForSpecificSection(analyzer.Parser, ".rela.dyn");
            relaDyn.AddRange(RelocationHelper.GetRelocationInfoForSpecificSection(analyzer.Parser, ".rel.dyn"));

            List<ELFRelocationInfo> relaPlt = RelocationHelper.GetRelocationInfoForSpecificSection(analyzer.Parser, ".rela.plt");
            relaPlt.AddRange(RelocationHelper.GetRelocationInfoForSpecificSection(analyzer.Parser, ".rel.plt"));

            return new ElfDisplayData(
                ELFHeaderHelper.GetFormattedELFHeaderInfo(analyzer.Parser),
                ProgramHeaderHelper.GetInterpreterInfo(analyzer.Parser),
                ProgramHeaderHelper.GetProgramHeaderInfoList(analyzer.Parser),
                SectionHeaderHelper.GetSectionHeaderInfoList(analyzer.Parser),
                ProgramHeaderHelper.GetSectionToSegmentMappingInfo(analyzer.Parser),
                SymbolTableHelper.GetSymbolTableInfoList(analyzer.Parser, SectionType.SHT_SYMTAB),
                SymbolTableHelper.GetSymbolTableInfoList(analyzer.Parser, SectionType.SHT_DYNSYM),
                DynamicHelper.GetDynamicSectionInfoList(analyzer.Parser),
                analyzer.GetFormattedVersionSymbolInfo(),
                analyzer.GetFormattedVersionDependencyInfo(),
                relaDyn,
                relaPlt,
                GotHelper.GetGotInfoList(analyzer.Parser, ".got.plt"),
                GotHelper.GetGotInfoList(analyzer.Parser, ".got"),
                analyzer.GetFormattedNotesInfo(),
                AttributesHelper.GetAttributeInfo(analyzer.Parser),
                ExidxInfoHelper.GetExidxInfo(analyzer.Parser));
        }

        // UI 线程：仅赋值控件，几乎不会抛业务异常，从而保证视图整体一致
        private void ApplyDisplayData(ElfDisplayData data)
        {
            ELFHeaderInfoControl.SetELFHeaderInfo(data.HeaderInfo);
            if (!string.IsNullOrEmpty(data.Interpreter))
            {
                ELFHeaderInfoControl.SetInterpreterInfo(data.Interpreter);
            }
            ELFProgramHeaderControl.SetProgramHeadersData(data.ProgramHeaders);
            ELFSectionHeaderControl.SetSectionHeadersData(data.SectionHeaders);
            ELFSectionToSegmentMappingControl.SetSectionToSegmentInfo(data.SectionToSegment);

            ELFSymbolTableControl.SetSymbolTableData(data.SymbolTable);
            ELFSymbolTableTabItem.Visibility = data.SymbolTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            ELFDynsymControl.SetDynsymData(data.DynsymTable);
            ELFDynsymTabItem.Visibility = data.DynsymTable.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            ELFDynamicSectionControl.SetDynamicSectionData(data.DynamicSection);
            ELFDynamicSectionTabItem.Visibility = data.DynamicSection.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            ELFVersionSymbolInfoControl.SetVersionSymbolInfo(data.VersionSymbolInfo);
            ELFVersionDependencyInfoControl.SetVersionDependencyInfo(data.VersionDependencyInfo);

            ELFRelocationControl.SetRelaDynData(data.RelaDyn);
            ELFRelaDynTabItem.Visibility = data.RelaDyn.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            ELFPltRelocationControl.SetRelaPltData(data.RelaPlt);
            ELFRelaPltTabItem.Visibility = data.RelaPlt.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            ELFGotPltControl.SetGotData(data.GotPlt, "GOT.PLT 表 (.got.plt)");
            ELFGotPltTabItem.Visibility = data.GotPlt.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            ELFGotControl.SetGotData(data.Got, "GOT 表 (.got)");
            ELFGotTabItem.Visibility = data.Got.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            ELFNoteInfoControl.SetNoteInfo(data.NoteInfo);
            ELFNoteInfoTabItem.Visibility = data.NoteInfo.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

            ELFAttributeInfoControl.SetAttributeInfo(data.AttributeInfo);
            ELFAttributeInfoTabItem.Visibility = data.AttributeInfo.Length > 0 ? Visibility.Visible : Visibility.Collapsed;

            ELFExidxInfoControl.SetExidxInfo(data.ExidxInfo);
            ELFExidxInfoTabItem.Visibility = !data.ExidxInfo.Contains("There are no exception index entries", StringComparison.CurrentCulture) ? Visibility.Visible : Visibility.Collapsed;
        }

        // IFileAnalyzerView：供宿主统一调用
        public void LoadFile(string filePath) => AnalyzeELFFile(filePath);

        // async void：UI 事件式入口。解析+格式化在后台线程，UI 线程只做控件赋值，避免畸形/大文件卡死界面
        public async void AnalyzeELFFile(string filePath)
        {
            try
            {
                ElfDisplayData data = await Task.Run(() =>
                {
                    ELFAnalyzer.ELFAnalyzer analyzer = new(filePath);
                    return ComputeDisplayData(analyzer);
                }).ConfigureAwait(true);

                ApplyDisplayData(data);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or ArgumentException
                or IndexOutOfRangeException or EndOfStreamException or IOException
                or OverflowException or DivideByZeroException or InvalidDataException or FormatException)
            {
                MessageHelper.ShowError($"分析ELF文件时出错: {ex.Message}");
            }
        }
    }
}