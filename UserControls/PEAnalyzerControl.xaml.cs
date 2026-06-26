using PersonalTools.PEAnalyzer;
using PersonalTools.PEAnalyzer.Models;
using PersonalTools.PEAnalyzer.Parsers;
using PersonalTools.Utils;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// PEAnalyzerControl.xaml 的交互逻辑
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class PEAnalyzerControl : UserControl, IFileAnalyzerView
    {
        #pragma warning restore CA1515
        public PEAnalyzerControl()
        {
            InitializeComponent();
        }

        private void Grid_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            // 拖入文件时显示“复制”光标反馈，非文件拖放则禁止；与 FileTabHostControl/MarkdownToWordControl 行为一致
            e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
                ? System.Windows.DragDropEffects.Copy
                : System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        private PEInfo? currentPEInfo;

        /// <summary>拖入文件时回调宿主，由宿主按完整路径决定新建/覆盖 tab。</summary>
        public Action<IReadOnlyList<string>>? FilesDropped { get; set; }

        private void PEAnalyzer_Drop(object sender, DragEventArgs e)
        {
            // 某些拖放源声明了 FileDrop 格式但 GetData 返回 null 或非 string[]，直接强转会抛 NRE/InvalidCastException
            if (e.Data.GetDataPresent(DataFormats.FileDrop)
                && e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                e.Handled = true; // 阻止冒泡到宿主，避免重复处理
                FilesDropped?.Invoke(files);
            }
        }

        // IFileAnalyzerView：供宿主统一调用
        public void LoadFile(string filePath) => LoadPEFile(filePath);

        // async void：UI 事件式入口。PE 解析与依赖解析移到后台线程，UI 线程只做控件赋值，避免大文件/深依赖树卡界面
        public async void LoadPEFile(string filePath)
        {
            try
            {
                currentPEInfo = await Task.Run(() => PEParser.ParsePEFile(filePath)).ConfigureAwait(true);
                if (currentPEInfo == null)
                {
                    return;
                }
                DisplayHeaderInfo();
                await DisplayDependenciesAsync().ConfigureAwait(true);
                DisplayImportExportFunctions();
                DisplayAdditionalInfo();
                DisplayIcons();
            }
            catch (Exception ex) when (ex is FileNotFoundException or UnauthorizedAccessException or IOException or ArgumentException)
            {
                MessageHelper.ShowError($"加载文件时出错: {ex.Message}");
            }
        }

        private void DisplayHeaderInfo()
        {
            HeaderInfoPanel.Children.Clear();
            if (currentPEInfo == null)
            {
                return;
            }

            foreach ((string title, Dictionary<string, string> info) in PEHeaderPresenter.BuildHeaderSections(currentPEInfo))
            {
                AddHeaderInfo(title, info);
            }
        }

        private void DisplayAdditionalInfo()
        {
            AdditionalInfoPanel.Children.Clear();
            if (currentPEInfo == null)
            {
                return;
            }

            foreach ((string title, Dictionary<string, string> info) in PEHeaderPresenter.BuildAdditionalSections(currentPEInfo))
            {
                AddAdditionalInfo(title, info);
            }
        }

        private void DisplayIcons()
        {
            List<IconViewModel> iconViewModels = [];
            if (currentPEInfo?.Icons != null)
            {
                foreach (IconInfo icon in currentPEInfo.Icons)
                {
                    IconViewModel? viewModel = IconImageFactory.TryCreate(icon);
                    if (viewModel != null)
                    {
                        iconViewModels.Add(viewModel);
                    }
                }
            }

            IconsDataGrid.ItemsSource = iconViewModels;
        }

        private async Task DisplayDependenciesAsync()
        {
            if (currentPEInfo == null)
            {
                DependencyTree.ItemsSource = null;
                return;
            }

            // 根节点为已打开文件；预解析直接依赖并默认展开（根 PE 已解析，故仅做依赖路径解析）
            DependencyNode root = DependencyNode.CreateRoot(currentPEInfo);
            await root.EnsureLoadedAsync().ConfigureAwait(true);
            root.IsExpanded = true;
            DependencyTree.ItemsSource = new[] { root };
        }

        // 惰性展开：首次展开某依赖节点时解析它，得到其下层依赖
        private async void DependencyNode_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is DependencyNode node)
            {
                await node.EnsureLoadedAsync().ConfigureAwait(true);
            }
        }

        private void DisplayImportExportFunctions()
        {
            // 默认显示已打开文件的导入/导出
            ShowFunctions(currentPEInfo);
        }

        // 将指定 PEInfo 的导入/导出函数填入右侧两个表格
        private void ShowFunctions(PEInfo? info)
        {
            ImportFunctionsGrid.ItemsSource = info?.ImportFunctions;
            ExportFunctionsGrid.ItemsSource = info?.ExportFunctions;
        }

        private async void DependencyTree_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DependencyTree.SelectedItem is DependencyNode node)
            {
                // 确保该依赖已解析（后台线程解析），然后把导入/导出列表切换为它自身的数据
                await node.EnsureLoadedAsync().ConfigureAwait(true);
                ShowFunctions(node.Info);
            }
        }

        private void AddHeaderInfo(string title, System.Collections.Generic.Dictionary<string, string> info)
        {
            GroupBox groupBox = new() { Header = title, Margin = new Thickness(0, 5, 0, 5) };
            StackPanel panel = new() { Margin = new Thickness(5) };

            foreach (KeyValuePair<string, string> item in info)
            {
                TextBlock textBlock = new()
                {
                    Text = $"{item.Key}: {item.Value}",
                    Margin = new Thickness(0, 2, 0, 2),
                    TextWrapping = System.Windows.TextWrapping.Wrap
                };
                panel.Children.Add(textBlock);
            }

            groupBox.Content = panel;
            HeaderInfoPanel.Children.Add(groupBox);
        }

        private void AddAdditionalInfo(string title, System.Collections.Generic.Dictionary<string, string> info)
        {
            GroupBox groupBox = new() { Header = title, Margin = new Thickness(0, 5, 0, 5) };
            StackPanel panel = new() { Margin = new Thickness(5) };

            int itemsAdded = 0;
            foreach (KeyValuePair<string, string> item in info)
            {
                // 添加所有信息，即使是空的也显示出来，这样用户可以看到哪些信息不存在
                TextBlock textBlock = new()
                {
                    Text = $"{item.Key}: {item.Value ?? "无"}",
                    Margin = new Thickness(0, 2, 0, 2),
                    TextWrapping = System.Windows.TextWrapping.Wrap
                };
                panel.Children.Add(textBlock);
                itemsAdded++;
            }

            // 如果没有任何信息，添加提示文本
            if (itemsAdded == 0)
            {
                TextBlock textBlock = new()
                {
                    Text = "未找到相关信息",
                    Margin = new Thickness(0, 2, 0, 2),
                    FontStyle = FontStyles.Italic,
                    Foreground = System.Windows.Media.Brushes.Gray
                };
                panel.Children.Add(textBlock);
            }

            groupBox.Content = panel;
            AdditionalInfoPanel.Children.Add(groupBox);
        }

    }
}