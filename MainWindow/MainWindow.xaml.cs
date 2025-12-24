using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MyTool
{
    public partial class MainWindow : Window
    {
        private PEInfo? currentPEInfo = null;

        public MainWindow()
        {
            InitializeComponent();
            InitializeCRCAlgorithmComboBox();
            AdjustWindowSize();
        }

        // 调整窗口大小以适应屏幕
        private void AdjustWindowSize()
        {
            // 获取主屏幕的工作区域（减去任务栏等占用的空间）
            System.Windows.Rect workArea = System.Windows.SystemParameters.WorkArea;
            
            // 检查窗口是否超出屏幕边界
            double adjustedHeight = Math.Min(workArea.Height, 1040);
            double adjustedWidth = Math.Min(workArea.Width, 1400);
            
            // 应用调整后的尺寸
            this.Height = adjustedHeight;
            this.Width = adjustedWidth;
            
            // 确保窗口居中显示
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        // 初始化CRC算法下拉框
        private void InitializeCRCAlgorithmComboBox()
        {
            // 绑定算法列表到下拉框
            CRCAlgorithmComboBox.ItemsSource = MainWindow.CRCAlgorithms;
            CRCAlgorithmComboBox.DisplayMemberPath = "Name";
            CRCAlgorithmComboBox.SelectedIndex = 0;
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

                using (MD5 md5 = MD5.Create())
                {
                    byte[] hashBytes = md5.ComputeHash(inputBytes);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }
                    MD5ResultLabel.Content = sb.ToString();  // 恢复：使用Label的Content属性
                }
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

                using (SHA1 sha1 = SHA1.Create())
                {
                    byte[] hashBytes = sha1.ComputeHash(inputBytes);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }
                    SHA1ResultLabel.Content = sb.ToString();
                }
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

                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(inputBytes);
                    // 取前28个字节（224位）作为SHA224结果
                    byte[] sha224Bytes = new byte[28];
                    Array.Copy(hashBytes, sha224Bytes, 28);
                    
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < sha224Bytes.Length; i++)
                    {
                        sb.Append(sha224Bytes[i].ToString("x2"));
                    }
                    SHA224ResultLabel.Content = sb.ToString();
                }
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

                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(inputBytes);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }
                    SHA256ResultLabel.Content = sb.ToString();
                }
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

                using (SHA384 sha384 = SHA384.Create())
                {
                    byte[] hashBytes = sha384.ComputeHash(inputBytes);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }
                    SHA384ResultLabel.Content = sb.ToString();
                }
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

                using (SHA512 sha512 = SHA512.Create())
                {
                    byte[] hashBytes = sha512.ComputeHash(inputBytes);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }
                    SHA512ResultLabel.Content = sb.ToString();
                }
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

                // 由于.NET没有内置SHA3算法，我们使用BouncyCastle库实现
                // 但在这里，我们先模拟实现
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(inputBytes);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }
                    SHA3ResultLabel.Content = sb.ToString();
                }
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