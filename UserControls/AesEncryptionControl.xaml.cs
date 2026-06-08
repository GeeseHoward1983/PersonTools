using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace PersonalTools.UserControls
{
    // AES加密模式选项类
    internal sealed class AesModeOption
    {
        public required string Name { get; set; }
        public CipherMode Mode { get; set; }
    }

    // AES填充方式选项类
    internal sealed class AesPaddingOption
    {
        public required string Name { get; set; }
        public PaddingMode Padding { get; set; }
    }

    /// <summary>
    /// AesEncryptionControl.xaml 的交互逻辑
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class AesEncryptionControl : UserControl
    {
        #pragma warning restore CA1515
        public AesEncryptionControl()
        {
            InitializeComponent();
            InitializeAesComboBoxes(); // 初始化AES下拉框
        }

        private void Grid_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;
        }

        // 初始化AES下拉框选项
        private void InitializeAesComboBoxes()
        {
            // 初始化加密模式下拉框
            AesModeComboBox.Items.Add(new AesModeOption { Name = "CBC (默认)", Mode = CipherMode.CBC });
#pragma warning disable CA5358
            AesModeComboBox.Items.Add(new AesModeOption { Name = "ECB", Mode = CipherMode.ECB });
            AesModeComboBox.Items.Add(new AesModeOption { Name = "CFB", Mode = CipherMode.CFB });
            AesModeComboBox.Items.Add(new AesModeOption { Name = "OFB", Mode = CipherMode.OFB });
#pragma warning restore CA5358
            AesModeComboBox.SelectedIndex = 0; // 默认选择CBC

            // 初始化填充方式下拉框
            AesPaddingComboBox.Items.Add(new AesPaddingOption { Name = "PKCS7 (默认)", Padding = PaddingMode.PKCS7 });
            AesPaddingComboBox.Items.Add(new AesPaddingOption { Name = "Zero Padding", Padding = PaddingMode.Zeros });
            AesPaddingComboBox.Items.Add(new AesPaddingOption { Name = "PKCS5 Padding", Padding = PaddingMode.PKCS7 });
            AesPaddingComboBox.Items.Add(new AesPaddingOption { Name = "ISO7816 Padding", Padding = PaddingMode.ISO10126 });
            AesPaddingComboBox.Items.Add(new AesPaddingOption { Name = "ANSI X923 Padding", Padding = PaddingMode.ANSIX923 });
            AesPaddingComboBox.SelectedIndex = 0; // 默认选择PKCS7
        }

        // AES加密
        private void AesEncrypt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = AesInput.Text;
                if (string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("请输入要加密的文本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!TryGetAesParams(out byte[] key, out byte[]? iv, out CipherMode mode))
                {
                    return;
                }

                AesResult.Text = AesEncryptString(input, key, iv, mode);
            }
            catch (CryptographicException ex)
            {
                MessageBox.Show($"AES加密时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"AES加密时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (FormatException ex)
            {
                MessageBox.Show($"AES处理时发生错误：输入的十六进制格式无效。{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // AES解密
        private void AesDecrypt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = AesResult.Text;
                if (string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("请输入要解密的文本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (!TryGetAesParams(out byte[] key, out byte[]? iv, out CipherMode mode))
                {
                    return;
                }

                AesInput.Text = AesDecryptString(input, key, iv, mode);
            }
            catch (CryptographicException ex)
            {
                MessageBox.Show($"AES解密时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"AES解密时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (FormatException ex)
            {
                MessageBox.Show($"AES处理时发生错误：输入的十六进制格式无效。{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 校验并取出密钥/IV/模式（加解密共用）。校验失败时弹提示并返回 false。
        private bool TryGetAesParams(out byte[] key, out byte[]? iv, out CipherMode mode)
        {
            key = [];
            iv = null;
            mode = CipherMode.CBC;

            string keyInput = AesKey.Text;
            if (string.IsNullOrEmpty(keyInput))
            {
                MessageBox.Show("请输入密钥", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            // 验证密钥长度
            if (!IsValidKeyLength(keyInput, AesKeyStringRadio.IsChecked == true))
            {
                MessageBox.Show("密钥长度不正确。支持16位、24位或32位密钥。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            key = GetKeyBytes(keyInput, AesKeyStringRadio.IsChecked == true);

            mode = ((AesModeOption)AesModeComboBox.SelectedItem).Mode;

            // 对于CBC/CFB/OFB等模式，需要IV向量
#pragma warning disable CA5358
            if (mode is CipherMode.CBC or CipherMode.CFB or CipherMode.OFB)
#pragma warning restore CA5358
            {
                string ivInput = AesIV.Text;
                if (string.IsNullOrEmpty(ivInput))
                {
                    MessageBox.Show("当前模式需要IV向量", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }

                // 验证IV长度（对于AES，IV长度应该是16字节）
                if (!IsValidIVLength(ivInput, AesIVStringRadio.IsChecked == true))
                {
                    MessageBox.Show("IV向量长度不正确。应为16字节", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }
                iv = GetIVBytes(ivInput, AesIVStringRadio.IsChecked == true);
            }

            return true;
        }

        // AES清空
        private void AesClear_Click(object sender, RoutedEventArgs e)
        {
            AesInput.Clear();
            AesResult.Clear();
            AesKey.Clear();
            AesIV.Clear();
        }

        // AES加密字符串
        private string AesEncryptString(string input, byte[] key, byte[]? iv, CipherMode mode)
        {
            PaddingMode padding = ((AesPaddingOption)AesPaddingComboBox.SelectedItem).Padding;
            return AesCryptoService.Encrypt(input, key, iv, mode, padding, AesInputStringRadio.IsChecked != true);
        }

        // AES解密字符串
        private string AesDecryptString(string input, byte[] key, byte[]? iv, CipherMode mode)
        {
            PaddingMode padding = ((AesPaddingOption)AesPaddingComboBox.SelectedItem).Padding;
            return AesCryptoService.Decrypt(input, key, iv, mode, padding, AesInputStringRadio.IsChecked != true);
        }

        // 获取密钥字节数组
        private static byte[] GetKeyBytes(string keyInput, bool isString)
        {
            if (isString)
            {
                // 普通字符串模式
                return Encoding.UTF8.GetBytes(keyInput);
            }
            else
            {
                // Hex字符串模式
                return Utils.HexStringToByteArray(keyInput);
            }
        }

        // 获取IV字节数组
        private static byte[] GetIVBytes(string ivInput, bool isString)
        {
            // 不做截断/补齐：交由 IsValidIVLength 按字节长度校验，避免静默改变用户输入的 IV
            return isString ? Encoding.UTF8.GetBytes(ivInput) : Utils.HexStringToByteArray(ivInput);
        }

        // 验证密钥长度是否正确（按实际字节数校验，兼容多字节字符）
        private static bool IsValidKeyLength(string key, bool isString)
        {
            return GetKeyBytes(key, isString).Length is 16 or 24 or 32;
        }

        // 验证IV长度是否正确（AES 固定 16 字节）
        private static bool IsValidIVLength(string iv, bool isString)
        {
            return GetIVBytes(iv, isString).Length == 16;
        }

        // 处理AES标签页的文件拖放事件
        private void AesTab_Drop(object sender, DragEventArgs e)
        {
            string? filePath = FileDropHelper.GetFirstDroppedFile(e);
            if (filePath != null)
            {
                ProcessFileForAesEncryption(filePath);
            }
        }

        // 处理文件AES加密
        private void ProcessFileForAesEncryption(string filePath)
        {
            try
            {
                byte[] fileBytes = FileDropHelper.ReadAllBytes(filePath);

                // 将文件内容以 hex 显示在输入框，并切换到 Hex 模式
                AesInput.Text = Utils.ToHexString(fileBytes);
                AesInputHexRadio.IsChecked = true;
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

    }
}