using MyTool.PEAnalyzer.Models;
using System.Windows;
using Microsoft.Win32;
using System.IO;
using MyTool.ELFAnalyzer;

namespace MyTool
{
    public partial class MainWindow : Window
    {
        private PEInfo? currentPEInfo = null;

        public MainWindow()
        {
            InitializeComponent();
            InitializeCRCAlgorithmComboBox();
            InitializeSHA3AlgorithmComboBox();
            InitializeAesComboBoxes(); // 初始化AES下拉框
            InitializeRsaComboBoxes();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        #region ELF Analyzer Tab

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
                var analyzer = new MyTool.ELFAnalyzer.ELFAnalyzer(filePath);
                
                // 显示ELF头信息
                ELFHeaderInfoTextBlock.Text = analyzer.GetFormattedELFHeaderInfo();
                
                // 显示程序头信息
                ELFProgramHeaderInfoTextBlock.Text = analyzer.GetFormattedProgramHeadersInfo();
                
                // 显示节头信息
                ELFSectionHeaderInfoTextBlock.Text = analyzer.GetFormattedSectionHeadersInfo();
                
                // 显示符号表信息
                ELFSymbolTableInfoTextBlock.Text = analyzer.GetFormattedSymbolTableInfo();
                
                // 显示动态段信息
                ELFDynamicSectionInfoTextBlock.Text = analyzer.GetFormattedDynamicSectionInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"分析ELF文件时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion
    }
}