using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    internal sealed class SHA3AlgorithmOption(string name, int value)
    {
        public required string Name { get; set; } = name;
        public required int Value { get; set; } = value;
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
            SHA3AlgorithmComboBox.Items.Add(new SHA3AlgorithmOption(name: "SHA3-256", value: 256) { Name = "SHA3-256", Value = 256 });
            SHA3AlgorithmComboBox.Items.Add(new SHA3AlgorithmOption(name: "SHA3-384", value: 384) { Name = "SHA3-384", Value = 384 });
            SHA3AlgorithmComboBox.Items.Add(new SHA3AlgorithmOption(name: "SHA3-512", value: 512) { Name = "SHA3-512", Value = 512 });

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
                byte[] inputBytes = row.IsHexMode
                    ? Utils.HexStringToByteArray(row.InputText)
                    : Encoding.UTF8.GetBytes(row.InputText);
                row.Result = Utils.ToHexString(row.HashFunc(inputBytes));
            }
            catch (FormatException ex)
            {
                MessageBox.Show($"计算{row.Name}时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (ArgumentException ex)
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
                byte[] inputBytes;

                if (SHA3StringInputRadio.IsChecked == true)
                {
                    // 普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                else if (SHA3HexInputRadio.IsChecked == true)
                {
                    // Hex字符串模式
                    inputBytes = Utils.HexStringToByteArray(input);
                }
                else
                {
                    // 默认使用普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }

                // 获取选中的算法选项
                if (SHA3AlgorithmComboBox.SelectedItem is not SHA3AlgorithmOption selectedOption)
                {
                    MessageBox.Show("请选择SHA3算法类型", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 使用真正的SHA3算法
                using SHA3 sha3 = new(selectedOption.Value);
                byte[] hashBytes = sha3.ComputeHash(inputBytes);
                SHA3ResultLabel.Content = Utils.ToHexString(hashBytes);
            }
            catch (FormatException ex)
            {
                MessageBox.Show($"计算SHA3时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show($"计算SHA3时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (ArgumentOutOfRangeException ex)
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
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string filePath = files[0]; // 只处理第一个文件
                    ProcessFileForHashCalculation(filePath);
                }
            }
        }

        // 处理文件哈希计算
        private void ProcessFileForHashCalculation(string filePath)
        {
            try
            {
                byte[] fileBytes;

                // 读取文件内容
                using (FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read))
                {
                    fileBytes = new byte[fileStream.Length];
                    fileStream.ReadExactly(fileBytes);
                }

                // 计算除SHA3外的所有哈希值
                CalculateAndDisplayHashValues(fileBytes);

                // 将文件内容转换为hex字符串显示在SHA3输入框中
                string hexString = Utils.ToHexString(fileBytes);
                SHA3InputTextBox.Text = hexString;

                // 设置SHA3为hex输入模式
                SHA3HexInputRadio.IsChecked = true;

                // 更新提示文本
                FileDropHint.Text = $"已加载文件: {Path.GetFileName(filePath)}，请在下方选择SHA3算法类型并计算";
            }
            catch (IOException ex)
            {
                MessageBox.Show($"处理文件时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"处理文件时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 计算并显示各种哈希值（SHA3 单独处理）
        private void CalculateAndDisplayHashValues(byte[] data)
        {
            foreach (HashAlgorithmRow row in hashRows)
            {
                row.Result = Utils.ToHexString(row.HashFunc(data));
            }
        }
    }
}