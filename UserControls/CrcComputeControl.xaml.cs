using System.Globalization;
using System.IO;
using System.Windows.Controls;
using PersonalTools.Utils;
using PersonalTools.Utils.Hash;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// CrcComputeControl.xaml 的交互逻辑
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class CrcComputeControl : UserControl
    {
        #pragma warning restore CA1515
        public CrcComputeControl()
        {
            InitializeComponent();
            InitializeCRCAlgorithmComboBox();
        }

        // 初始化CRC算法下拉框
        private void InitializeCRCAlgorithmComboBox()
        {
            CRCAlgorithmComboBox.ItemsSource = CrcCalculator.Algorithms;
            CRCAlgorithmComboBox.DisplayMemberPath = "Name";
            CRCAlgorithmComboBox.SelectedIndex = 0;
        }

        // CRC计算功能
        private void CalculateCRC_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (CRCAlgorithmComboBox.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("请选择一个CRC算法", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            string input = CRCInputTextBox.Text;
            if (string.IsNullOrEmpty(input))
            {
                System.Windows.MessageBox.Show("请输入要计算CRC的值", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            try
            {
                CrcAlgorithm selectedAlgorithm = (CrcAlgorithm)CRCAlgorithmComboBox.SelectedItem;
                byte[] inputBytes = ConvertUtils.InputBytes(input, CRCHexInputRadio.IsChecked == true);

                uint crcResult = CrcCalculator.Compute(inputBytes, selectedAlgorithm);

                // 根据算法宽度格式化输出
                string formatString = selectedAlgorithm.Width switch
                {
                    <= 8 => "X2",
                    <= 16 => "X4",
                    _ => "X8",
                };
                CRCResultLabel.Content = crcResult.ToString(formatString, CultureInfo.InvariantCulture);
            }
            catch (FormatException ex)
            {
                System.Windows.MessageBox.Show($"计算CRC时发生错误: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            catch (ArgumentNullException ex)
            {
                System.Windows.MessageBox.Show($"计算CRC时发生错误: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                System.Windows.MessageBox.Show($"计算CRC时发生错误: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // 清空CRC计算结果
        private void ClearCRC_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CRCInputTextBox.Clear();
            CRCResultLabel.Content = "等待计算...";
        }

        // 处理CRC标签页的文件拖放事件
        private void CrcTab_Drop(object sender, System.Windows.DragEventArgs e)
        {
            string? filePath = FileDropHelper.GetFirstDroppedFile(e);
            if (filePath != null)
            {
                ProcessFileForCrcCalculation(filePath);
            }
        }

        // 预览拖拽事件，确保拖拽事件不被子控件拦截
        private void Grid_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;
        }

        // 处理文件CRC计算
        private void ProcessFileForCrcCalculation(string filePath)
        {
            try
            {
                byte[] fileBytes = FileDropHelper.ReadAllBytes(filePath);

                // 将文件内容转换为hex字符串显示在输入框中，并切换到 hex 输入模式
                CRCInputTextBox.Text = ConvertUtils.ToHexString(fileBytes);
                CRCHexInputRadio.IsChecked = true;
            }
            catch (IOException ex)
            {
                System.Windows.MessageBox.Show($"处理文件时发生错误: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            catch (UnauthorizedAccessException ex)
            {
                System.Windows.MessageBox.Show($"处理文件时发生错误: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

    }
}