using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace MyTool
{
    // SHA3算法选项类
    public class SHA3AlgorithmOption(string name, int value)
    {
        public string Name { get; set; } = name;
        public int Value { get; set; } = value;

        public override string ToString()
        {
            return Name;
        }
    }

    public partial class MainWindow : Window
    {
        // 初始化SHA3算法下拉框
        private void InitializeSHA3AlgorithmComboBox()
        {
            // 添加算法选项到下拉框
            SHA3AlgorithmComboBox.Items.Add(new SHA3AlgorithmOption("SHA3-256", 256));
            SHA3AlgorithmComboBox.Items.Add(new SHA3AlgorithmOption("SHA3-384", 384));
            SHA3AlgorithmComboBox.Items.Add(new SHA3AlgorithmOption("SHA3-512", 512));

            // 设置默认选中项
            SHA3AlgorithmComboBox.SelectedIndex = 0; // 默认选择SHA3-256
        }

        // MD5计算功能
        private void CalculateMD5_Click(object sender, RoutedEventArgs e)
        {
            string input = MD5InputTextBox.Text;
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("请输入要计算MD5的值", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                byte[] inputBytes;

                if (MD5StringInputRadio.IsChecked == true)
                {
                    // 普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                else if (MD5HexInputRadio.IsChecked == true)
                {
                    // Hex字符串模式
                    inputBytes = Utils.HexStringToByteArray(input);
                }
                else
                {
                    // 默认使用普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                byte[] hashBytes = MD5.HashData(inputBytes);
                StringBuilder sb = new();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                MD5ResultLabel.Content = sb.ToString();  // 恢复：使用Label的Content属性
            }
            catch (Exception ex)
            {
                MessageBox.Show($"计算MD5时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 清空MD5计算结果
        private void ClearMD5_Click(object sender, RoutedEventArgs e)
        {
            MD5InputTextBox.Clear();
            MD5ResultLabel.Content = "等待计算...";  // 恢复：使用Label的Content属性，并恢复提示文本
        }

        // SHA1计算功能
        private void CalculateSHA1_Click(object sender, RoutedEventArgs e)
        {
            string input = SHA1InputTextBox.Text;
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("请输入要计算SHA1的值", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                byte[] inputBytes;

                if (SHA1StringInputRadio.IsChecked == true)
                {
                    // 普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                else if (SHA1HexInputRadio.IsChecked == true)
                {
                    // Hex字符串模式
                    inputBytes = Utils.HexStringToByteArray(input);
                }
                else
                {
                    // 默认使用普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                byte[] hashBytes = SHA1.HashData(inputBytes);
                StringBuilder sb = new();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                SHA1ResultLabel.Content = sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"计算SHA1时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 清空SHA1计算结果
        private void ClearSHA1_Click(object sender, RoutedEventArgs e)
        {
            SHA1InputTextBox.Clear();
            SHA1ResultLabel.Content = "等待计算...";
        }

        // SHA224计算功能
        private void CalculateSHA224_Click(object sender, RoutedEventArgs e)
        {
            string input = SHA224InputTextBox.Text;
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("请输入要计算SHA224的值", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                byte[] inputBytes;

                if (SHA224StringInputRadio.IsChecked == true)
                {
                    // 普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                else if (SHA224HexInputRadio.IsChecked == true)
                {
                    // Hex字符串模式
                    inputBytes = Utils.HexStringToByteArray(input);
                }
                else
                {
                    // 默认使用普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                byte[] hashBytes = SHA256.HashData(inputBytes);
                // 取前28个字节（224位）作为SHA224结果
                byte[] sha224Bytes = new byte[28];
                Array.Copy(hashBytes, sha224Bytes, 28);

                StringBuilder sb = new();
                for (int i = 0; i < sha224Bytes.Length; i++)
                {
                    sb.Append(sha224Bytes[i].ToString("x2"));
                }
                SHA224ResultLabel.Content = sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"计算SHA224时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 清空SHA224计算结果
        private void ClearSHA224_Click(object sender, RoutedEventArgs e)
        {
            SHA224InputTextBox.Clear();
            SHA224ResultLabel.Content = "等待计算...";
        }

        // SHA256计算功能
        private void CalculateSHA256_Click(object sender, RoutedEventArgs e)
        {
            string input = SHA256InputTextBox.Text;
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("请输入要计算SHA256的值", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                byte[] inputBytes;

                if (SHA256StringInputRadio.IsChecked == true)
                {
                    // 普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                else if (SHA256HexInputRadio.IsChecked == true)
                {
                    // Hex字符串模式
                    inputBytes = Utils.HexStringToByteArray(input);
                }
                else
                {
                    // 默认使用普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                byte[] hashBytes = SHA256.HashData(inputBytes);
                StringBuilder sb = new();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                SHA256ResultLabel.Content = sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"计算SHA256时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 清空SHA256计算结果
        private void ClearSHA256_Click(object sender, RoutedEventArgs e)
        {
            SHA256InputTextBox.Clear();
            SHA256ResultLabel.Content = "等待计算...";
        }

        // SHA384计算功能
        private void CalculateSHA384_Click(object sender, RoutedEventArgs e)
        {
            string input = SHA384InputTextBox.Text;
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("请输入要计算SHA384的值", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                byte[] inputBytes;

                if (SHA384StringInputRadio.IsChecked == true)
                {
                    // 普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                else if (SHA384HexInputRadio.IsChecked == true)
                {
                    // Hex字符串模式
                    inputBytes = Utils.HexStringToByteArray(input);
                }
                else
                {
                    // 默认使用普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                byte[] hashBytes = SHA384.HashData(inputBytes);
                StringBuilder sb = new();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                SHA384ResultLabel.Content = sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"计算SHA384时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 清空SHA384计算结果
        private void ClearSHA384_Click(object sender, RoutedEventArgs e)
        {
            SHA384InputTextBox.Clear();
            SHA384ResultLabel.Content = "等待计算...";
        }

        // SHA512计算功能
        private void CalculateSHA512_Click(object sender, RoutedEventArgs e)
        {
            string input = SHA512InputTextBox.Text;
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("请输入要计算SHA512的值", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                byte[] inputBytes;

                if (SHA512StringInputRadio.IsChecked == true)
                {
                    // 普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                else if (SHA512HexInputRadio.IsChecked == true)
                {
                    // Hex字符串模式
                    inputBytes = Utils.HexStringToByteArray(input);
                }
                else
                {
                    // 默认使用普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                byte[] hashBytes = SHA512.HashData(inputBytes);
                StringBuilder sb = new();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                SHA512ResultLabel.Content = sb.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"计算SHA512时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
                SHA3 sha3 = new(selectedOption.Value);
                byte[] hashBytes = sha3.ComputeHash(inputBytes);
                StringBuilder sb = new();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                SHA3ResultLabel.Content = sb.ToString();
            }
            catch (Exception ex)
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
    }
}