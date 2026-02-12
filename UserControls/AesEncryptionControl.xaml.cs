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
                string keyInput = AesKey.Text;
                string ivInput = AesIV.Text;

                if (string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("请输入要加密的文本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (string.IsNullOrEmpty(keyInput))
                {
                    MessageBox.Show("请输入密钥", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 验证密钥长度
                if (!IsValidKeyLength(keyInput, AesKeyStringRadio.IsChecked == true))
                {
                    MessageBox.Show("密钥长度不正确。支持16位、24位或32位密钥。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 根据选择的类型处理密钥
                byte[] key = GetKeyBytes(keyInput, AesKeyStringRadio.IsChecked == true);

                // 对于CBC和CFB等模式，需要IV向量
                AesModeOption selectedMode = (AesModeOption)AesModeComboBox.SelectedItem;
#pragma warning disable CA5358
                if (selectedMode.Mode is CipherMode.CBC or CipherMode.CFB or CipherMode.OFB)
#pragma warning restore CA5358
                {
                    if (string.IsNullOrEmpty(ivInput))
                    {
                        MessageBox.Show("当前模式需要IV向量", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // 验证IV长度（对于AES，IV长度应该是16字节）
                    if (!IsValidIVLength(ivInput, AesIVStringRadio.IsChecked == true))
                    {
                        MessageBox.Show("IV向量长度不正确。应为16字节", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    byte[] iv = GetIVBytes(ivInput, AesIVStringRadio.IsChecked == true);
                    string result = AesEncryptString(input, key, iv, selectedMode.Mode);
                    AesResult.Text = result;
                }
                else // ECB模式不需要IV
                {
                    string result = AesEncryptString(input, key, null, selectedMode.Mode);
                    AesResult.Text = result;
                }
            }
            catch (CryptographicException ex)
            {
                MessageBox.Show($"AES加密时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"AES加密时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
            }
        }

        // AES解密
        private void AesDecrypt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = AesResult.Text;
                string keyInput = AesKey.Text;
                string ivInput = AesIV.Text;

                if (string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("请输入要解密的文本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (string.IsNullOrEmpty(keyInput))
                {
                    MessageBox.Show("请输入密钥", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 验证密钥长度
                if (!IsValidKeyLength(keyInput, AesKeyStringRadio.IsChecked == true))
                {
                    MessageBox.Show("密钥长度不正确。支持16位、24位或32位密钥。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // 根据选择的类型处理密钥
                byte[] key = GetKeyBytes(keyInput, AesKeyStringRadio.IsChecked == true);

                // 对于CBC和CFB等模式，需要IV向量
                AesModeOption selectedMode = (AesModeOption)AesModeComboBox.SelectedItem;
#pragma warning disable CA5358
                if (selectedMode.Mode is CipherMode.CBC or CipherMode.CFB or CipherMode.OFB)
                {
                    if (string.IsNullOrEmpty(ivInput))
                    {
                        MessageBox.Show("当前模式需要IV向量", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // 验证IV长度（对于AES，IV长度应该是16字节）
                    if (!IsValidIVLength(ivInput, AesIVStringRadio.IsChecked == true))
                    {
                        MessageBox.Show("IV向量长度不正确。应为16字节", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    byte[] iv = GetIVBytes(ivInput, AesIVStringRadio.IsChecked == true);
                    string result = AesDecryptString(input, key, iv, selectedMode.Mode);
                    AesInput.Text = result;
                }
                else // ECB模式不需要IV
                {
                    string result = AesDecryptString(input, key, null, selectedMode.Mode);
                    AesInput.Text = result;
                }
            }
            catch (CryptographicException ex)
            {
                MessageBox.Show($"AES解密时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show($"AES解密时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
            }
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
            AesPaddingOption selectedPadding = (AesPaddingOption)AesPaddingComboBox.SelectedItem;

            using Aes aesAlg = Aes.Create();
            aesAlg.KeySize = key.Length * 8; // 根据密钥长度设置KeySize
            aesAlg.Key = key;
            if (iv != null)
            {
                aesAlg.IV = iv;
            }
            aesAlg.Mode = mode;
            aesAlg.Padding = selectedPadding.Padding;

            // 根据选择的类型处理输入文本
            byte[] inputBytes;
            if (AesInputStringRadio.IsChecked == true)
            {
                // 普通字符串模式
                inputBytes = Encoding.UTF8.GetBytes(input);
            }
            else
            {
                // Hex字符串模式
                inputBytes = Utils.HexStringToByteArray(input);
            }
#pragma warning disable CA5401
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
#pragma warning restore CA5401
            using MemoryStream msEncrypt = new();
            using CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write);
            csEncrypt.Write(inputBytes, 0, inputBytes.Length);
            byte[] encryptedBytes = msEncrypt.ToArray();
            // 返回十六进制字符串而不是Base64
            return Utils.ToHexString(encryptedBytes);
        }

        // AES解密字符串
        private string AesDecryptString(string input, byte[] key, byte[]? iv, CipherMode mode)
        {
            AesPaddingOption selectedPadding = (AesPaddingOption)AesPaddingComboBox.SelectedItem;

            // 将输入的十六进制字符串转换为字节数组
            byte[] encryptedBytes = Utils.HexStringToByteArray(input);

            using Aes aesAlg = Aes.Create();
            aesAlg.KeySize = key.Length * 8; // 根据密钥长度设置KeySize
            aesAlg.Key = key;
            if (iv != null)
            {
                aesAlg.IV = iv;
            }
            aesAlg.Mode = mode;
            aesAlg.Padding = selectedPadding.Padding;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using MemoryStream msDecrypt = new(encryptedBytes);
            using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
            using StreamReader srDecrypt = new(csDecrypt);
            return srDecrypt.ReadToEnd();
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
            if (isString)
            {
                // 普通字符串模式
                // 对于IV，如果是字符串模式，我们只取前16个字节
                byte[] ivBytes = Encoding.UTF8.GetBytes(ivInput);
                if (ivBytes.Length < 16)
                {
                    // 如果长度不足16字节，用0填充
                    Array.Resize(ref ivBytes, 16);
                }
                else if (ivBytes.Length > 16)
                {
                    // 如果长度超过16字节，截取前16字节
                    Array.Resize(ref ivBytes, 16);
                }
                return ivBytes;
            }
            else
            {
                // Hex字符串模式
                return Utils.HexStringToByteArray(ivInput);
            }
        }

        // 验证密钥长度是否正确
        private static bool IsValidKeyLength(string key, bool isString)
        {
            if (isString)
            {
                // 如果是字符串，检查字符长度
                return key.Length is 16 or 24 or 32;
            }
            else
            {
                // 如果是十六进制字符串，检查字符长度（每个字节需要2个字符）
                return key.Length is 32 or 48 or 64; // 16*2, 24*2, 32*2
            }
        }

        // 验证IV长度是否正确
        private static bool IsValidIVLength(string iv, bool isString)
        {
            if (isString)
            {
                // 如果是字符串，长度应该是16字节
                return iv.Length == 16;
            }
            else
            {
                // 如果是十六进制字符串，长度应该是32字符（16字节）
                return iv.Length == 32;
            }
        }

        // 处理AES标签页的文件拖放事件
        private void AesTab_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string filePath = files[0]; // 只处理第一个文件
                    ProcessFileForAesEncryption(filePath);
                }
            }
        }

        // 处理文件AES加密
        private void ProcessFileForAesEncryption(string filePath)
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

                // 将文件内容显示在输入框中
                AesInput.Text = Utils.ToHexString(fileBytes);
                // 同时切换到Hex字符串模式
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
            // 其他异常重新抛出
            catch (Exception)
            {
                throw;
            }
        }

    }
}