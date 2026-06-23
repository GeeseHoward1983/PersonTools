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
            e.Handled = true;
        }

        private PEInfo? currentPEInfo;

        /// <summary>拖入文件时回调宿主，由宿主按完整路径决定新建/覆盖 tab。</summary>
        public Action<IReadOnlyList<string>>? FilesDropped { get; set; }

        private void PEAnalyzer_Drop(object sender, DragEventArgs e)
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

        // IFileAnalyzerView：供宿主统一调用
        public void LoadFile(string filePath) => LoadPEFile(filePath);

        public void LoadPEFile(string filePath)
        {
            try
            {
                currentPEInfo = PEParser.ParsePEFile(filePath);
                if (currentPEInfo == null)
                {
                    return;
                }
                DisplayHeaderInfo();
                DisplayDependencies();
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

        private void DisplayDependencies()
        {
            if (currentPEInfo == null)
            {
                DependencyTree.ItemsSource = null;
                return;
            }

            // 根节点为已打开文件；预解析直接依赖并默认展开
            DependencyNode root = DependencyNode.CreateRoot(currentPEInfo);
            root.EnsureLoaded();
            root.IsExpanded = true;
            DependencyTree.ItemsSource = new[] { root };
        }

        // 惰性展开：首次展开某依赖节点时解析它，得到其下层依赖
        private void DependencyNode_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is DependencyNode node)
            {
                node.EnsureLoaded();
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

        private void DependencyTree_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DependencyTree.SelectedItem is DependencyNode node)
            {
                // 确保该依赖已解析，然后把导入/导出列表切换为它自身的数据
                node.EnsureLoaded();
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