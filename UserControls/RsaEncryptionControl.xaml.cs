using Microsoft.Win32;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using PersonalTools.Utils;
using PersonalTools.Utils.Crypto;

namespace PersonalTools.UserControls
{
    /// <summary>
    /// RsaEncryptionControl.xaml 的交互逻辑
    /// </summary>
    #pragma warning disable CA1515 // 符合WPF框架要求，需要保持public访问修饰符
    public partial class RsaEncryptionControl : UserControl
    {
        #pragma warning restore CA1515
        public RsaEncryptionControl()
        {
            InitializeComponent();
            InitializeRsaComboBoxes();
        }

        private void Grid_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            // 拖入文件时显示“复制”光标反馈，非文件拖放则禁止；与 FileTabHostControl/MarkdownToWordControl 行为一致
            e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
                ? System.Windows.DragDropEffects.Copy
                : System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        // RSA密钥长度选项类
        private sealed class RsaKeySizeOption
        {
            public required string Name { get; set; }
            public int KeySize { get; set; }
        }

        // RSA加密
        private void RsaEncrypt_Click(object sender, RoutedEventArgs e)
        {
            string input = RsaInput.Text;
            string publicKey = RsaPublicKey.Text;

            if (string.IsNullOrEmpty(input))
            {
                MessageHelper.ShowInfo("请输入要加密的文本");
                return;
            }

            if (string.IsNullOrEmpty(publicKey))
            {
                MessageHelper.ShowInfo("请输入公钥");
                return;
            }

            try
            {
                string result = RsaCryptoService.Encrypt(input, publicKey, RsaInputStringRadio.IsChecked == true);
                RsaResult.Text = result;
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException)
            {
                MessageHelper.ShowError($"RSA加密失败: {ex.Message}");
            }
        }

        // RSA解密
        private void RsaDecrypt_Click(object sender, RoutedEventArgs e)
        {
            string input = RsaResult.Text;
            string privateKey = RsaPrivateKey.Text;

            if (string.IsNullOrEmpty(input))
            {
                MessageHelper.ShowInfo("请输入要解密的文本");
                return;
            }

            if (string.IsNullOrEmpty(privateKey))
            {
                MessageHelper.ShowInfo("请输入私钥");
                return;
            }

            try
            {
                string result = RsaCryptoService.Decrypt(input, privateKey, RsaInputStringRadio.IsChecked == true);
                RsaInput.Text = result;
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException)
            {
                MessageHelper.ShowError($"RSA解密失败: {ex.Message}");
            }
        }

        // RSA签名
        private void RsaSign_Click(object sender, RoutedEventArgs e)
        {
            string input = RsaInput.Text;
            string privateKey = RsaPrivateKey.Text;

            if (string.IsNullOrEmpty(input))
            {
                MessageHelper.ShowInfo("请输入要签名的数据");
                return;
            }

            if (string.IsNullOrEmpty(privateKey))
            {
                MessageHelper.ShowInfo("请输入私钥");
                return;
            }

            try
            {
                string result = RsaCryptoService.Sign(input, privateKey, RsaInputStringRadio.IsChecked == true);
                RsaResult.Text = result;
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException)
            {
                MessageHelper.ShowError($"RSA签名失败: {ex.Message}");
            }
        }

        // RSA验签
        private void RsaVerify_Click(object sender, RoutedEventArgs e)
        {
            string input = RsaInput.Text;      // 原始数据
            string signature = RsaResult.Text; // 签名值
            string publicKey = RsaPublicKey.Text;

            if (string.IsNullOrEmpty(input))
            {
                MessageHelper.ShowInfo("请输入原始数据");
                return;
            }

            if (string.IsNullOrEmpty(signature))
            {
                MessageHelper.ShowInfo("请输入签名值");
                return;
            }

            if (string.IsNullOrEmpty(publicKey))
            {
                MessageHelper.ShowInfo("请输入公钥");
                return;
            }

            try
            {
                bool isValid = RsaCryptoService.Verify(input, signature, publicKey, RsaInputStringRadio.IsChecked == true);
                if (isValid)
                {
                    MessageHelper.ShowInfo("验签成功！", "结果");
                }
                else
                {
                    MessageHelper.ShowWarning("验签失败！", "结果");
                }
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException)
            {
                MessageHelper.ShowError($"RSA验签失败: {ex.Message}");
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
                    MessageHelper.ShowInfo("请选择密钥长度");
                    return;
                }

                (string publicKey, string privateKey) = RsaCryptoService.GenerateKeyPair(selectedOption.KeySize);
                RsaPublicKey.Text = publicKey;
                RsaPrivateKey.Text = privateKey;

                MessageHelper.ShowInfo("密钥对生成成功！");
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException)
            {
                MessageHelper.ShowError($"生成密钥对时发生错误: {ex.Message}");
            }
        }

        // RSA导入公钥/私钥
        private void RsaImportPublicKey_Click(object sender, RoutedEventArgs e) => ImportKey("选择公钥文件", isPublic: true);

        private void RsaImportPrivateKey_Click(object sender, RoutedEventArgs e) => ImportKey("选择私钥文件", isPublic: false);

        private void ImportKey(string dialogTitle, bool isPublic)
        {
            string kind = isPublic ? "公钥" : "私钥";
            try
            {
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "PEM文件 (*.pem)|*.pem|文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                    Title = dialogTitle
                };

                if (openFileDialog.ShowDialog() != true)
                {
                    return;
                }

                string pem = File.ReadAllText(openFileDialog.FileName);
                if (!RsaCryptoService.IsValidPem(pem))
                {
                    MessageHelper.ShowError($"选择的文件不是有效的{kind}文件！");
                    return;
                }

                if (isPublic)
                {
                    RsaPublicKey.Text = pem;
                }
                else
                {
                    RsaPrivateKey.Text = pem;
                }
                MessageHelper.ShowInfo($"{kind}导入成功！");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageHelper.ShowError($"导入{kind}时发生错误: {ex.Message}");
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

        // 处理RSA标签页的文件拖放事件
        private void RsaTab_Drop(object sender, DragEventArgs e)
        {
            string? filePath = FileDropHelper.GetFirstDroppedFile(e);
            if (filePath != null)
            {
                ProcessFileForRsaEncryption(filePath);
            }
        }

        // 处理文件RSA加密
        private void ProcessFileForRsaEncryption(string filePath)
        {
            if (!FileDropHelper.IsWithinHexDisplayLimit(filePath))
            {
                MessageHelper.ShowWarning($"文件较大（超过 {FileDropHelper.HexDisplayWarnBytes / (1024 * 1024)} MB），转为十六进制显示会导致界面长时间无响应，已取消。");
                return;
            }

            try
            {
                byte[] fileBytes = FileDropHelper.ReadAllBytes(filePath);

                // 将文件内容显示在输入框中
                RsaInput.Text = ConvertUtils.ToHexString(fileBytes);
                // 同时切换到Hex字符串模式
                RsaInputHexRadio.IsChecked = true;
            }
            catch (IOException ex)
            {
                MessageHelper.ShowError($"处理文件时发生错误: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageHelper.ShowError($"处理文件时发生错误: {ex.Message}");
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