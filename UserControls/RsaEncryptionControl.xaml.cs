using Microsoft.Win32;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MyTool.UserControls
{
    /// <summary>
    /// RsaEncryptionControl.xaml 的交互逻辑
    /// </summary>
    public partial class RsaEncryptionControl : UserControl
    {
        public RsaEncryptionControl()
        {
            InitializeComponent();
            InitializeRsaComboBoxes();
        }

        private void Grid_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;
        }

        // RSA密钥长度选项类
        public class RsaKeySizeOption
        {
            public required string Name { get; set; }
            public int KeySize { get; set; }
        }

        // RSA加密
        private void RsaEncrypt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = RsaInput.Text;
                string publicKey = RsaPublicKey.Text;

                if (string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("请输入要加密的文本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (string.IsNullOrEmpty(publicKey))
                {
                    MessageBox.Show("请输入公钥", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                try
                {
                    string result = RsaEncryptString(input, publicKey, RsaInputStringRadio.IsChecked == true);
                    RsaResult.Text = result;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"RSA加密失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"RSA加密时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // RSA解密
        private void RsaDecrypt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = RsaResult.Text;
                string privateKey = RsaPrivateKey.Text;

                if (string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("请输入要解密的文本", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (string.IsNullOrEmpty(privateKey))
                {
                    MessageBox.Show("请输入私钥", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                try
                {
                    string result = RsaDecryptString(input, privateKey);
                    RsaInput.Text = result;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"RSA解密失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"RSA解密时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // RSA签名
        private void RsaSign_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = RsaInput.Text;
                string privateKey = RsaPrivateKey.Text;

                if (string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("请输入要签名的数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (string.IsNullOrEmpty(privateKey))
                {
                    MessageBox.Show("请输入私钥", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                try
                {
                    string result = RsaSignData(input, privateKey, RsaInputStringRadio.IsChecked == true);
                    RsaResult.Text = result;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"RSA签名失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"RSA签名时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // RSA验签
        private void RsaVerify_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = RsaInput.Text;      // 原始数据
                string signature = RsaResult.Text; // 签名值
                string publicKey = RsaPublicKey.Text;

                if (string.IsNullOrEmpty(input))
                {
                    MessageBox.Show("请输入原始数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (string.IsNullOrEmpty(signature))
                {
                    MessageBox.Show("请输入签名值", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (string.IsNullOrEmpty(publicKey))
                {
                    MessageBox.Show("请输入公钥", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                try
                {
                    bool isValid = RsaVerifySignature(input, signature, publicKey, RsaInputStringRadio.IsChecked == true);
                    if (isValid)
                    {
                        MessageBox.Show("验签成功！", "结果", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("验签失败！", "结果", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"RSA验签失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"RSA验签时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // RSA生成密钥对
        private void RsaGenerateKeyPair_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RsaKeySizeOption? selectedOption = (RsaKeySizeOption?)RsaKeySizeComboBox.SelectedItem;
                if (selectedOption == null)
                {
                    MessageBox.Show("请选择密钥长度", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                using RSA rsa = RSA.Create(selectedOption.KeySize); // 根据选择生成指定长度的密钥对
                string publicKey = rsa.ExportRSAPublicKeyPem(); // 导出公钥PEM格式
                string privateKey = rsa.ExportRSAPrivateKeyPem(); // 导出私钥PEM格式

                RsaPublicKey.Text = publicKey;
                RsaPrivateKey.Text = privateKey;

                MessageBox.Show("密钥对生成成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成密钥对时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // RSA导入公钥
        private void RsaImportPublicKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "PEM文件 (*.pem)|*.pem|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                    Title = "选择公钥文件"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string publicKey = File.ReadAllText(openFileDialog.FileName);

                    // 验证是否是有效的公钥
                    using RSA rsa = RSA.Create();
                    try
                    {
                        rsa.ImportFromPem(publicKey);
                        RsaPublicKey.Text = publicKey;
                        MessageBox.Show("公钥导入成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("选择的文件不是有效的公钥文件！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入公钥时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // RSA导入私钥
        private void RsaImportPrivateKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "PEM文件 (*.pem)|*.pem|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                    Title = "选择私钥文件"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string privateKey = File.ReadAllText(openFileDialog.FileName);

                    // 验证是否是有效的私钥
                    using RSA rsa = RSA.Create();
                    try
                    {
                        rsa.ImportFromPem(privateKey);
                        RsaPrivateKey.Text = privateKey;
                        MessageBox.Show("私钥导入成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("选择的文件不是有效的私钥文件！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入私钥时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // RSA清空
        private void RsaClear_Click(object sender, RoutedEventArgs e)
        {
            RsaInput.Clear();
            RsaResult.Clear();
            RsaPublicKey.Clear();
            RsaPrivateKey.Clear();
        }

        // RSA加密字符串
        private static string RsaEncryptString(string input, string publicKey, bool isString)
        {
            using RSA rsa = RSA.Create();

            try
            {
                // 根据选择的类型处理输入文本
                byte[] inputBytes;
                if (isString)
                {
                    // 普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                else
                {
                    // Hex字符串模式
                    inputBytes = Utils.HexStringToByteArray(input);
                }

                // 导入公钥
                rsa.ImportFromPem(publicKey);

                // 加密
                byte[] encryptedBytes = rsa.Encrypt(inputBytes, RSAEncryptionPadding.Pkcs1);

                // 返回十六进制字符串
                return BitConverter.ToString(encryptedBytes).Replace("-", "");
            }
            catch (Exception ex)
            {
                throw new Exception($"导入公钥或加密失败: {ex.Message}");
            }
        }

        // RSA解密字符串
        private static string RsaDecryptString(string input, string privateKey)
        {
            using RSA rsa = RSA.Create();

            try
            {
                // 将输入的十六进制字符串转换为字节数组
                byte[] encryptedBytes = Utils.HexStringToByteArray(input);

                // 导入私钥
                rsa.ImportFromPem(privateKey);

                // 解密
                byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.Pkcs1);

                // 返回解密后的字符串
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                throw new Exception($"导入私钥或解密失败: {ex.Message}");
            }
        }

        // RSA签名数据
        private static string RsaSignData(string input, string privateKey, bool isString)
        {
            using RSA rsa = RSA.Create();

            try
            {
                // 根据选择的类型处理输入文本
                byte[] inputBytes;
                if (isString)
                {
                    // 普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                else
                {
                    // Hex字符串模式
                    inputBytes = Utils.HexStringToByteArray(input);
                }

                // 导入私钥
                rsa.ImportFromPem(privateKey);

                // 使用SHA256作为哈希算法进行签名
                byte[] signatureBytes = rsa.SignData(inputBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                // 返回十六进制字符串
                return BitConverter.ToString(signatureBytes).Replace("-", "");
            }
            catch (Exception ex)
            {
                throw new Exception($"导入私钥或签名失败: {ex.Message}");
            }
        }

        // RSA验证签名
        private static bool RsaVerifySignature(string input, string signature, string publicKey, bool isString)
        {
            using RSA rsa = RSA.Create();

            try
            {
                // 根据选择的类型处理输入文本
                byte[] inputBytes;
                if (isString)
                {
                    // 普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }
                else
                {
                    // Hex字符串模式
                    inputBytes = Utils.HexStringToByteArray(input);
                }

                // 将签名的十六进制字符串转换为字节数组
                byte[] signatureBytes = Utils.HexStringToByteArray(signature);

                // 导入公钥
                rsa.ImportFromPem(publicKey);

                // 使用SHA256作为哈希算法进行验签
                return rsa.VerifyData(inputBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch
            {
                return false;
            }
        }

        // 处理RSA标签页的文件拖放事件
        private void RsaTab_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string filePath = files[0]; // 只处理第一个文件
                    ProcessFileForRsaEncryption(filePath);
                }
            }
        }

        // 处理文件RSA加密
        private void ProcessFileForRsaEncryption(string filePath)
        {
            try
            {
                byte[] fileBytes;

                // 读取文件内容
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fileBytes = new byte[fileStream.Length];
                    fileStream.Read(fileBytes, 0, fileBytes.Length);
                }

                // 将文件内容显示在输入框中
                RsaInput.Text = BitConverter.ToString(fileBytes).Replace("-", "");
                // 同时切换到Hex字符串模式
                RsaInputHexRadio.IsChecked = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"处理文件时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeRsaComboBoxes()
        {
            RsaKeySizeComboBox.Items.Add(new RsaKeySizeOption { Name = "512 (不推荐)", KeySize = 512 });
            RsaKeySizeComboBox.Items.Add(new RsaKeySizeOption { Name = "1024 (不推荐) ", KeySize = 1024 });
            RsaKeySizeComboBox.Items.Add(new RsaKeySizeOption { Name = "2048 (推荐)", KeySize = 2048 });
            RsaKeySizeComboBox.Items.Add(new RsaKeySizeOption { Name = "3072", KeySize = 3072 });
            RsaKeySizeComboBox.Items.Add(new RsaKeySizeOption { Name = "4096", KeySize = 4096 });
            RsaKeySizeComboBox.SelectedIndex = 2; // 默认选择2048
        }

    }
}