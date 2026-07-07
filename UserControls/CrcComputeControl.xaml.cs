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
        private int crcDropToken; // 拖放重入令牌：快速连续拖入多个文件时，仅最后一次结果回填，丢弃过期
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

        // CRC计算功能：UI 线程捕获算法与输入，计算移后台，避免大 hex 输入卡界面
        private async void CalculateCRC_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (CRCAlgorithmComboBox.SelectedItem == null)
            {
                MessageHelper.ShowInfo("请选择一个CRC算法");
                return;
            }

            string input = CRCInputTextBox.Text;
            if (string.IsNullOrEmpty(input))
            {
                MessageHelper.ShowInfo("请输入要计算CRC的值");
                return;
            }

            CrcAlgorithm selectedAlgorithm = (CrcAlgorithm)CRCAlgorithmComboBox.SelectedItem;
            bool isHex = CRCHexInputRadio.IsChecked == true;
            try
            {
                uint crcResult = await Task.Run(() =>
                    CrcCalculator.Compute(ConvertUtils.InputBytes(input, isHex), selectedAlgorithm)).ConfigureAwait(true);

                // 根据算法宽度格式化输出
                string formatString = selectedAlgorithm.Width switch
                {
                    <= 8 => "X2",
                    <= 16 => "X4",
                    _ => "X8",
                };
                CRCResultLabel.Content = crcResult.ToString(formatString, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                MessageHelper.ShowError($"计算CRC时发生错误: {ex.Message}");
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
            // 拖入文件时显示“复制”光标反馈，非文件拖放则禁止；与 FileTabHostControl/MarkdownToWordControl 行为一致
            e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
                ? System.Windows.DragDropEffects.Copy
                : System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        // 处理文件CRC计算：读盘 + hex 编码移后台线程，UI 线程仅回填，避免大文件卡界面
        private async void ProcessFileForCrcCalculation(string filePath)
        {
            if (!FileDropHelper.IsWithinHexDisplayLimit(filePath))
            {
                MessageHelper.ShowWarning($"文件较大（超过 {FileDropHelper.HexDisplayWarnBytes / (1024 * 1024)} MB），转为十六进制显示会导致界面长时间无响应，已取消。");
                return;
            }

            int token = ++crcDropToken;
            try
            {
                string hex = await Task.Run(() => ConvertUtils.ToHexString(FileDropHelper.ReadAllBytes(filePath))).ConfigureAwait(true);
                if (token != crcDropToken)
                {
                    return; // 已有更晚的拖放在进行，丢弃本次过期结果
                }

                // 将文件内容（hex）显示在输入框中，并切换到 hex 输入模式
                CRCInputTextBox.Text = hex;
                CRCHexInputRadio.IsChecked = true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or NotSupportedException)
            {
                MessageHelper.ShowError($"处理文件时发生错误: {ex.Message}");
            }
            catch (OutOfMemoryException ex)
            {
                // 本方法为 Drop 处理器 fire-and-forget 调用的 async void：近 100MB 十六进制串在受限机器上可能 OOM，
                // 就地提示而非让异常冒泡到全局兜底弹通用错误框。
                MessageHelper.ShowError($"文件过大，转十六进制时内存不足: {ex.Message}");
            }
        }

    }
}