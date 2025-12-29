using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace MyTool
{
    public partial class MainWindow : Window
    {
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

        // RSA生成密钥对
        private void RsaGenerateKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using RSA rsa = RSA.Create(2048); // 使用2048位密钥长度
                
                // 导出公钥和私钥
                string publicKey = rsa.ExportRSAPublicKeyPem();
                string privateKey = rsa.ExportRSAPrivateKeyPem();

                RsaPublicKey.Text = publicKey;
                RsaPrivateKey.Text = privateKey;

                MessageBox.Show("RSA密钥对生成成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"生成密钥对失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // RSA导入公钥
        private void RsaImportPublic_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                Filter = "PEM文件|*.pem|文本文件|*.txt|所有文件|*.*",
                Title = "导入公钥"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string key = File.ReadAllText(openFileDialog.FileName);
                    RsaPublicKey.Text = key;
                    MessageBox.Show("公钥导入成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入公钥失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // RSA导出公钥
        private void RsaExportPublic_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(RsaPublicKey.Text))
            {
                MessageBox.Show("当前没有公钥可导出", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Microsoft.Win32.SaveFileDialog saveFileDialog = new()
            {
                Filter = "PEM文件|*.pem|文本文件|*.txt|所有文件|*.*",
                Title = "导出公钥",
                FileName = "public_key.pem"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, RsaPublicKey.Text);
                    MessageBox.Show("公钥导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出公钥失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // RSA导入私钥
        private void RsaImportPrivate_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new()
            {
                Filter = "PEM文件|*.pem|文本文件|*.txt|所有文件|*.*",
                Title = "导入私钥"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string key = File.ReadAllText(openFileDialog.FileName);
                    RsaPrivateKey.Text = key;
                    MessageBox.Show("私钥导入成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导入私钥失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // RSA导出私钥
        private void RsaExportPrivate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(RsaPrivateKey.Text))
            {
                MessageBox.Show("当前没有私钥可导出", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Microsoft.Win32.SaveFileDialog saveFileDialog = new()
            {
                Filter = "PEM文件|*.pem|文本文件|*.txt|所有文件|*.*",
                Title = "导出私钥",
                FileName = "private_key.pem"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, RsaPrivateKey.Text);
                    MessageBox.Show("私钥导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出私钥失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}