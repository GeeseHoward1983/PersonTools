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
        public HashComputeControl()
        {
            InitializeComponent();
            InitializeSHA3AlgorithmComboBox();
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

        // 统一的哈希计算：按所选输入模式取字节，计算并显示结果（Hex 单选选中→按十六进制解析，否则按 UTF-8）
        private static void ComputeHash(string algoName, TextBox inputBox, RadioButton hexRadio, Func<byte[], byte[]> hashFunc, ContentControl resultLabel)
        {
            string input = inputBox.Text;
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show($"请输入要计算{algoName}的值", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                byte[] inputBytes = hexRadio.IsChecked == true
                    ? Utils.HexStringToByteArray(input)
                    : Encoding.UTF8.GetBytes(input);
                resultLabel.Content = Utils.ToHexString(hashFunc(inputBytes));
            }
            catch (FormatException ex)
            {
                MessageBox.Show($"计算{algoName}时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"计算{algoName}时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // MD5计算功能
        private void CalculateMD5_Click(object sender, RoutedEventArgs e)
            => ComputeHash("MD5", MD5InputTextBox, MD5HexInputRadio, MD5.HashData, MD5ResultLabel);

        // 清空MD5计算结果
        private void ClearMD5_Click(object sender, RoutedEventArgs e)
        {
            MD5InputTextBox.Clear();
            MD5ResultLabel.Content = "等待计算...";  // 恢复：使用Label的Content属性，并恢复提示文本
        }

        // SHA1计算功能
        private void CalculateSHA1_Click(object sender, RoutedEventArgs e)
            => ComputeHash("SHA1", SHA1InputTextBox, SHA1HexInputRadio, SHA1.HashData, SHA1ResultLabel);

        // 清空SHA1计算结果
        private void ClearSHA1_Click(object sender, RoutedEventArgs e)
        {
            SHA1InputTextBox.Clear();
            SHA1ResultLabel.Content = "等待计算...";
        }

        // SHA224计算功能
        private void CalculateSHA224_Click(object sender, RoutedEventArgs e)
            => ComputeHash("SHA224", SHA224InputTextBox, SHA224HexInputRadio, Sha224.HashData, SHA224ResultLabel);

        // 清空SHA224计算结果
        private void ClearSHA224_Click(object sender, RoutedEventArgs e)
        {
            SHA224InputTextBox.Clear();
            SHA224ResultLabel.Content = "等待计算...";
        }

        // SHA256计算功能
        private void CalculateSHA256_Click(object sender, RoutedEventArgs e)
            => ComputeHash("SHA256", SHA256InputTextBox, SHA256HexInputRadio, SHA256.HashData, SHA256ResultLabel);

        // 清空SHA256计算结果
        private void ClearSHA256_Click(object sender, RoutedEventArgs e)
        {
            SHA256InputTextBox.Clear();
            SHA256ResultLabel.Content = "等待计算...";
        }

        // SHA384计算功能
        private void CalculateSHA384_Click(object sender, RoutedEventArgs e)
            => ComputeHash("SHA384", SHA384InputTextBox, SHA384HexInputRadio, SHA384.HashData, SHA384ResultLabel);

        // 清空SHA384计算结果
        private void ClearSHA384_Click(object sender, RoutedEventArgs e)
        {
            SHA384InputTextBox.Clear();
            SHA384ResultLabel.Content = "等待计算...";
        }

        // SHA512计算功能
        private void CalculateSHA512_Click(object sender, RoutedEventArgs e)
            => ComputeHash("SHA512", SHA512InputTextBox, SHA512HexInputRadio, SHA512.HashData, SHA512ResultLabel);

        // 清空SHA512计算结果
        private void ClearSHA512_Click(object sender, RoutedEventArgs e)
        {
            SHA512InputTextBox.Clear();
            SHA512ResultLabel.Content = "等待计算...";
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

        // 计算并显示各种哈希值
        private void CalculateAndDisplayHashValues(byte[] data)
        {
            // 计算MD5
            byte[] hash = MD5.HashData(data);
            MD5ResultLabel.Content = Utils.ToHexString(hash);

            // 计算SHA1
            hash = SHA1.HashData(data);
            SHA1ResultLabel.Content = Utils.ToHexString(hash);

            // 计算SHA224
            SHA224ResultLabel.Content = Utils.ToHexString(Sha224.HashData(data));

            // 计算SHA256
            hash = SHA256.HashData(data);
            SHA256ResultLabel.Content = Utils.ToHexString(hash);

            // 计算SHA384
            hash = SHA384.HashData(data);
            SHA384ResultLabel.Content = Utils.ToHexString(hash);

            // 计算SHA512
            hash = SHA512.HashData(data);
            SHA512ResultLabel.Content = Utils.ToHexString(hash);
        }
    }
}