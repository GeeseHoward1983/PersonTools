using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// ELFAnalyzerHostControl.xaml 的交互逻辑。
    /// 多文件宿主：按完整路径管理 tab——同路径覆盖、新路径新建，每个 tab 内放一个 ELFAnalyzerControl。
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class ELFAnalyzerHostControl : UserControl
    {
        #pragma warning restore CA1515
        public ELFAnalyzerHostControl()
        {
            InitializeComponent();
            UpdateEmptyHint();
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new()
            {
                Filter = "Executable and Linkable Format files (*.elf)|*.elf|All files (*.*)|*.*",
                Multiselect = true
            };

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

        // 由 tab 内 ELFAnalyzerControl 拖入时上抛，统一走按路径建/覆盖逻辑
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
                    && existing.Content is ELFAnalyzerControl existingControl)
                {
                    existingControl.AnalyzeELFFile(full);
                    FileTabs.SelectedItem = existing;
                    return;
                }
            }

            // 新路径 → 新建 tab
            ELFAnalyzerControl control = new();
            control.FilesDropped = OnInnerFilesDropped;
            control.AnalyzeELFFile(full);

            TabItem tab = new()
            {
                ToolTip = full,
                Tag = full,
                Content = control
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
                if (tab.Content is ELFAnalyzerControl control)
                {
                    control.FilesDropped = null;
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
