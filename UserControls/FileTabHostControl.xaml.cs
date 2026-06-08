using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// FileTabHostControl.xaml 的交互逻辑。
    /// 与分析器无关的多文件宿主：按完整路径管理 tab——同路径覆盖、新路径新建。
    /// 由宿主方设置 <see cref="AnalyzerFactory"/>/<see cref="FileFilter"/>/<see cref="EmptyHintText"/> 决定承载哪种分析视图。
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class FileTabHostControl : UserControl
    {
        #pragma warning restore CA1515
        public FileTabHostControl()
        {
            InitializeComponent();
            UpdateEmptyHint();
        }

        /// <summary>创建一个文件分析视图（PE/ELF 等）。由宿主方在构造后设置。</summary>
        internal Func<IFileAnalyzerView>? AnalyzerFactory { get; set; }

        /// <summary>打开对话框使用的文件过滤器。</summary>
        public string FileFilter { get; set; } = "All files (*.*)|*.*";

        /// <summary>空状态提示文案。</summary>
        public string EmptyHintText
        {
            get => EmptyHint.Text;
            set => EmptyHint.Text = value;
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new() { Filter = FileFilter, Multiselect = true };
            if (dlg.ShowDialog() == true)
            {
                OpenOrUpdateFiles(dlg.FileNames);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Host_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Host_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                OpenOrUpdateFiles((string[])e.Data.GetData(DataFormats.FileDrop));
            }
        }

        // 由 tab 内分析视图拖入时上抛，统一走按路径建/覆盖逻辑
        private void OnInnerFilesDropped(IReadOnlyList<string> files)
        {
            OpenOrUpdateFiles(files);
        }

        private void OpenOrUpdateFiles(IReadOnlyList<string> files)
        {
            foreach (string file in files)
            {
                OpenOrUpdate(file);
            }
        }

        private void OpenOrUpdate(string path)
        {
            if (AnalyzerFactory == null)
            {
                return;
            }

            string full;
            try
            {
                full = Path.GetFullPath(path);
            }
            catch (ArgumentException)
            {
                return; // 非法路径
            }

            // 已存在同完整路径的 tab → 覆盖其内容并选中
            foreach (object item in FileTabs.Items)
            {
                if (item is TabItem existing && existing.Tag is string tag
                    && string.Equals(tag, full, StringComparison.OrdinalIgnoreCase)
                    && existing.Content is IFileAnalyzerView existingView)
                {
                    existingView.LoadFile(full);
                    FileTabs.SelectedItem = existing;
                    return;
                }
            }

            // 新路径 → 新建 tab
            IFileAnalyzerView view = AnalyzerFactory();
            view.FilesDropped = OnInnerFilesDropped;
            view.LoadFile(full);

            TabItem tab = new()
            {
                ToolTip = full,
                Tag = full,
                Content = view // 实现者为 UserControl，可直接作为 tab 内容
            };
            tab.Header = BuildTabHeader(Path.GetFileName(full), tab);

            FileTabs.Items.Add(tab);
            FileTabs.SelectedItem = tab;
            UpdateEmptyHint();
        }

        // tab 标题：文件名 + 右侧 × 关闭按钮
        private StackPanel BuildTabHeader(string title, TabItem owner)
        {
            StackPanel panel = new() { Orientation = Orientation.Horizontal };
            panel.Children.Add(new TextBlock
            {
                Text = title,
                VerticalAlignment = VerticalAlignment.Center
            });

            Button close = new()
            {
                Content = "✕",
                Margin = new Thickness(8, 0, 0, 0),
                Padding = new Thickness(2, 0, 2, 0),
                FontSize = 10,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                ToolTip = "关闭",
                Tag = owner
            };
            close.Click += CloseTab_Click;
            panel.Children.Add(close);

            return panel;
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TabItem tab)
            {
                if (tab.Content is IFileAnalyzerView view)
                {
                    view.FilesDropped = null;
                }

                FileTabs.Items.Remove(tab);
                UpdateEmptyHint();
            }
        }

        private void UpdateEmptyHint()
        {
            EmptyHint.Visibility = FileTabs.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
