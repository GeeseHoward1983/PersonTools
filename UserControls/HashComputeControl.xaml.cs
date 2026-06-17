using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using PersonalTools.Utils;
using PersonalTools.Utils.Hash;

namespace PersonalTools.UserControls
{
    internal sealed class Sha3AlgorithmOption(string name, int value)
    {
        public string Name { get; } = name;
        public int Value { get; } = value;
    }

    /// <summary>
    /// HashComputeControl.xaml 的交互逻辑
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class HashComputeControl : UserControl
    {
        #pragma warning restore CA1515
        // 6 种定长哈希算法（数据驱动渲染，绑定到 HashItemsControl）
        private readonly List<HashAlgorithmRow> hashRows =
        [
            new("MD5", MD5.HashData),
            new("SHA1", SHA1.HashData),
            new("SHA224", Sha224.HashData),
            new("SHA256", SHA256.HashData),
            new("SHA384", SHA384.HashData),
            new("SHA512", SHA512.HashData),
        ];

        public HashComputeControl()
        {
            InitializeComponent();
            InitializeSHA3AlgorithmComboBox();
            HashItemsControl.ItemsSource = hashRows;
        }

        private void Grid_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;
        }

        // 初始化SHA3算法下拉框
        private void InitializeSHA3AlgorithmComboBox()
        {
            // 添加算法选项到下拉框
            SHA3AlgorithmComboBox.Items.Add(new Sha3AlgorithmOption("SHA3-256", 256));
            SHA3AlgorithmComboBox.Items.Add(new Sha3AlgorithmOption("SHA3-384", 384));
            SHA3AlgorithmComboBox.Items.Add(new Sha3AlgorithmOption("SHA3-512", 512));

            // 设置默认选中项
            SHA3AlgorithmComboBox.SelectedIndex = 0; // 默认选择SHA3-256
        }

        // 计算按钮：从 DataContext 取出该行算法并计算
        private void HashCompute_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: HashAlgorithmRow row })
            {
                ComputeRow(row);
            }
        }

        // 清空按钮：复位该行输入与结果
        private void HashClear_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: HashAlgorithmRow row })
            {
                row.InputText = string.Empty;
                row.Result = "等待计算...";
            }
        }

        // 按所选输入模式取字节，计算并把结果写回该行（Hex 模式→十六进制解析，否则 UTF-8）
        private static void ComputeRow(HashAlgorithmRow row)
        {
            if (string.IsNullOrEmpty(row.InputText))
            {
                MessageBox.Show($"请输入要计算{row.Name}的值", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                byte[] inputBytes = ConvertUtils.InputBytes(row.InputText, row.IsHexMode);
                row.Result = ConvertUtils.ToHexString(row.HashFunc(inputBytes));
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                MessageBox.Show($"计算{row.Name}时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // SHA3计算功能
        private void CalculateSHA3_Click(object sender, RoutedEventArgs e)
        {
            string input = SHA3InputTextBox.Text;
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("请输入要计算SHA3的值", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                // 仅 Hex 单选钮被选中时按十六进制解析，其余（含未选）默认 UTF-8
                byte[] inputBytes = ConvertUtils.InputBytes(input, SHA3HexInputRadio.IsChecked == true);

                // 获取选中的算法选项
                if (SHA3AlgorithmComboBox.SelectedItem is not Sha3AlgorithmOption selectedOption)
                {
                    MessageBox.Show("请选择SHA3算法类型", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 使用真正的SHA3算法
                using Sha3 sha3 = new(selectedOption.Value);
                byte[] hashBytes = sha3.ComputeHash(inputBytes);
                SHA3ResultLabel.Content = ConvertUtils.ToHexString(hashBytes);
            }
            catch (Exception ex) when (ex is FormatException or ArgumentNullException or ArgumentOutOfRangeException)
            {
                MessageBox.Show($"计算SHA3时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 清空SHA3计算结果
        private void ClearSHA3_Click(object sender, RoutedEventArgs e)
        {
            SHA3InputTextBox.Clear();
            SHA3ResultLabel.Content = "等待计算...";
        }

        // 处理文件拖放事件
        private void HashTab_Drop(object sender, DragEventArgs e)
        {
            string? filePath = FileDropHelper.GetFirstDroppedFile(e);
            if (filePath != null)
            {
                ProcessFileForHashCalculation(filePath);
            }
        }

        // 处理文件哈希计算
        private void ProcessFileForHashCalculation(string filePath)
        {
            try
            {
                byte[] fileBytes = FileDropHelper.ReadAllBytes(filePath);

                // 计算除SHA3外的所有哈希值
                CalculateAndDisplayHashValues(fileBytes);

                // 将文件内容转为 hex 显示在 SHA3 输入框，并切换到 hex 模式
                SHA3InputTextBox.Text = ConvertUtils.ToHexString(fileBytes);
                SHA3HexInputRadio.IsChecked = true;

                FileDropHint.Text = $"已加载文件: {Path.GetFileName(filePath)}，请在下方选择SHA3算法类型并计算";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageBox.Show($"处理文件时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 计算并显示各种哈希值（SHA3 单独处理）
        private void CalculateAndDisplayHashValues(byte[] data)
        {
            foreach (HashAlgorithmRow row in hashRows)
            {
                row.Result = ConvertUtils.ToHexString(row.HashFunc(data));
            }
        }
    }
}