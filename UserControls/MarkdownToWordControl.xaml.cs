using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DocumentFormat.OpenXml.Packaging;
using Markdig.Syntax;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using PersonalTools.MarkdownToWord;
using PersonalTools.MarkdownToWord.Docx;
using PersonalTools.MarkdownToWord.Models;
using PersonalTools.Utils;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// MarkdownToWordControl.xaml 的交互逻辑：左侧编辑 Markdown、右侧 WebView2 实时预览、
    /// 顶部可配置导出样式，并把当前文本导出为符合中文排版规范的 .docx。
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class MarkdownToWordControl : UserControl
    {
        #pragma warning restore CA1515
        private readonly DocxStyleSettings settings;
        private readonly DispatcherTimer previewTimer;
        private readonly string previewFilePath;
        private string? baseDir;
        private string? currentMdPath;
        private bool previewReady;

        public MarkdownToWordControl()
        {
            InitializeComponent();

            settings = DocxStyleSettingsStore.Load();
            StyleGrid.ItemsSource = settings.Rows;
            ChineseFontColumn.ItemsSource = DocxStyleOptions.ChineseFonts;
            WesternFontColumn.ItemsSource = DocxStyleOptions.WesternFonts;
            FontSizeColumn.ItemsSource = DocxStyleOptions.FontSizeNames;
            GenerateTocCheckBox.IsChecked = settings.GenerateToc;

            string previewDir = Path.Combine(Path.GetTempPath(), "PersonalTools");
            Directory.CreateDirectory(previewDir);
            previewFilePath = Path.Combine(previewDir, "md-preview-" + Guid.NewGuid().ToString("N") + ".html");

            previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            previewTimer.Tick += PreviewTimer_Tick;

            Editor.Text = DefaultSample;
        }

        // ---- 预览（WebView2）----

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (previewReady)
            {
                return;
            }

            try
            {
                await Preview.EnsureCoreWebView2Async().ConfigureAwait(true);
                previewReady = true;
                RefreshPreview();
            }
            catch (Exception ex) when (ex is WebView2RuntimeNotFoundException or InvalidOperationException or IOException)
            {
                MessageBox.Show($"预览初始化失败（需要 WebView2 运行时）：{ex.Message}", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            previewTimer.Stop();
            DocxStyleSettingsStore.Save(settings);
            TryDeleteTempPreview();
        }

        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            previewTimer.Stop();
            previewTimer.Start();
        }

        private void PreviewTimer_Tick(object? sender, EventArgs e)
        {
            previewTimer.Stop();
            RefreshPreview();
        }

        private void RefreshPreview()
        {
            if (!previewReady)
            {
                return;
            }

            try
            {
                string html = MarkdownService.BuildPreviewHtml(Editor.Text, baseDir);
                File.WriteAllText(previewFilePath, html, Encoding.UTF8);
                Preview.CoreWebView2.Navigate(new Uri(previewFilePath).AbsoluteUri);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // 预览失败不影响导出，忽略
                System.Diagnostics.Debug.WriteLine($"刷新预览失败: {ex.Message}");
            }
        }

        // ---- 打开 / 拖放 ----

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Filter = "Markdown 文件 (*.md;*.markdown)|*.md;*.markdown|所有文件 (*.*)|*.*",
            };
            if (dialog.ShowDialog() == true)
            {
                LoadMarkdownFile(dialog.FileName);
            }
        }

        // 保存当前编辑的 Markdown：已有路径则直接写回，否则走「另存为」
        private void SaveMd_Click(object sender, RoutedEventArgs e)
        {
            if (currentMdPath == null)
            {
                SaveMarkdownAs();
                return;
            }

            try
            {
                File.WriteAllText(currentMdPath, Editor.Text);
                MessageBox.Show($"已保存：{currentMdPath}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveMarkdownAs()
        {
            SaveFileDialog dialog = new()
            {
                Filter = "Markdown 文件 (*.md)|*.md|所有文件 (*.*)|*.*",
                DefaultExt = ".md",
                AddExtension = true,
                FileName = currentMdPath != null ? Path.GetFileName(currentMdPath) : "未命名.md",
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                File.WriteAllText(dialog.FileName, Editor.Text);
                string fullPath = Path.GetFullPath(dialog.FileName);
                currentMdPath = fullPath;
                baseDir = Path.GetDirectoryName(fullPath);
                RefreshPreview();
                MessageBox.Show($"已保存：{fullPath}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnPreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            string? path = FileDropHelper.GetFirstDroppedFile(e);
            if (path != null)
            {
                LoadMarkdownFile(path);
            }
        }

        private void LoadMarkdownFile(string path)
        {
            try
            {
                Editor.Text = File.ReadAllText(path);
                string fullPath = Path.GetFullPath(path);
                currentMdPath = fullPath;
                baseDir = Path.GetDirectoryName(fullPath);
                RefreshPreview();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                MessageBox.Show($"打开文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---- 导出 docx ----

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new()
            {
                Filter = "Word 文档 (*.docx)|*.docx",
                DefaultExt = ".docx",
                AddExtension = true,
            };

            // 默认路径=当前 md 所在目录，默认文件名=md 文件名(不含扩展名)+.docx（需求 3）
            if (currentMdPath != null)
            {
                dialog.InitialDirectory = Path.GetDirectoryName(currentMdPath) ?? string.Empty;
                dialog.FileName = Path.GetFileNameWithoutExtension(currentMdPath) + ".docx";
            }
            else
            {
                dialog.FileName = "导出文档.docx";
            }

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                MarkdownDocument ast = MarkdownService.Parse(Editor.Text);
                bool updated;
                Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                    DocxWriter.Write(ast, settings, dialog.FileName, baseDir);
                    // 调用本机 Word 强制更新一次目录/编号域（生成即静态，无需打开时刷新）
                    updated = WordFieldUpdater.TryUpdateFields(dialog.FileName);
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }

                DocxStyleSettingsStore.Save(settings);
                string message = updated
                    ? $"导出成功（目录与图表编号已自动更新）：{dialog.FileName}"
                    : $"导出成功：{dialog.FileName}\n未检测到本机 Word，目录页码请在 Word 中按 Ctrl+A 全选后按 F9 手动更新一次。";
                MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or OpenXmlPackageException)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ---- 样式设置 ----

        private void ResetStyles_Click(object sender, RoutedEventArgs e)
        {
            DocxStyleSettings defaults = DocxStyleSettings.CreateDefault();
            settings.GenerateToc = defaults.GenerateToc;
            settings.Rows.Clear();
            foreach (ContentStyleRow row in defaults.Rows)
            {
                settings.Rows.Add(row);
            }

            GenerateTocCheckBox.IsChecked = settings.GenerateToc;
            DocxStyleSettingsStore.Save(settings);
        }

        private void GenerateToc_Click(object sender, RoutedEventArgs e)
        {
            settings.GenerateToc = GenerateTocCheckBox.IsChecked == true;
            DocxStyleSettingsStore.Save(settings);
        }

        private void StyleGrid_CellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
        {
            // 等绑定提交后再持久化
            Dispatcher.BeginInvoke(new Action(() => DocxStyleSettingsStore.Save(settings)), DispatcherPriority.Background);
        }

        private void TryDeleteTempPreview()
        {
            try
            {
                if (File.Exists(previewFilePath))
                {
                    File.Delete(previewFilePath);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // 临时文件清理失败可忽略
            }
        }

        private const string DefaultSample =
            "# 示例标题\n\n这是正文，支持 **加粗**、*斜体* 与 `行内代码`。\n\n" +
            "## 二级标题\n\n| 列1 | 列2 |\n| --- | --- |\n| A | B |\n\n" +
            "拖入或打开一个 .md 文件，在左侧编辑、右侧预览，点击「导出 Word」生成 docx。\n";
    }
}
