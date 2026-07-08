using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using PersonalTools.Utils;
using PersonalTools.Utils.Crypto;

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
        private int aesDropToken; // 拖放重入令牌：快速连续拖入多个文件时，仅最后一次结果回填，丢弃过期
        public AesEncryptionControl()
        {
            InitializeComponent();
            InitializeAesComboBoxes(); // 初始化AES下拉框
        }

        private void Grid_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            // 拖入文件时显示“复制”光标反馈，非文件拖放则禁止；与 FileTabHostControl/MarkdownToWordControl 行为一致
            e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
                ? System.Windows.DragDropEffects.Copy
                : System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        // 初始化AES下拉框选项
        private void InitializeAesComboBoxes()
        {
            // 初始化加密模式下拉框
            AesModeComboBox.Items.Add(new AesModeOption { Name = "CBC (默认)", Mode = CipherMode.CBC });
#pragma warning disable CA5358
            AesModeComboBox.Items.Add(new AesModeOption { Name = "ECB (不推荐：无IV、相同明文块产生相同密文)", Mode = CipherMode.ECB });
            AesModeComboBox.Items.Add(new AesModeOption { Name = "CFB", Mode = CipherMode.CFB });
            // 不提供 OFB：.NET 内置 Aes 全平台不支持 OFB，选中后 CreateEncryptor/CreateDecryptor 必抛异常
#pragma warning restore CA5358
            AesModeComboBox.SelectedIndex = 0; // 默认选择CBC

            // 初始化填充方式下拉框
            AesPaddingComboBox.Items.Add(new AesPaddingOption { Name = "PKCS7 (默认)", Padding = PaddingMode.PKCS7 });
            AesPaddingComboBox.Items.Add(new AesPaddingOption { Name = "Zero Padding (解密不可靠去填充)", Padding = PaddingMode.Zeros });
            AesPaddingComboBox.Items.Add(new AesPaddingOption { Name = "ISO10126 Padding", Padding = PaddingMode.ISO10126 });
            AesPaddingComboBox.Items.Add(new AesPaddingOption { Name = "ANSI X923 Padding", Padding = PaddingMode.ANSIX923 });
            AesPaddingComboBox.SelectedIndex = 0; // 默认选择PKCS7
        }

        // AES加密：UI 线程捕获参数，仅把纯加密运算放后台，避免大输入(近100MB hex)在 UI 线程计算卡界面
        private async void AesEncrypt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = AesInput.Text;
                if (string.IsNullOrEmpty(input))
                {
                    MessageHelper.ShowInfo("请输入要加密的文本");
                    return;
                }

                if (!TryGetAesParams(out byte[] key, out byte[]? iv, out CipherMode mode))
                {
                    return;
                }

                PaddingMode padding = AesPaddingComboBox.SelectedItem is AesPaddingOption paddingOption ? paddingOption.Padding : PaddingMode.PKCS7;
                bool inputIsHex = AesInputStringRadio.IsChecked != true;
                AesResult.Text = await Task.Run(() => AesCryptoService.Encrypt(input, key, iv, mode, padding, inputIsHex)).ConfigureAwait(true);
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException)
            {
                MessageHelper.ShowError($"AES加密时发生错误: {ex.Message}");
            }
            catch (FormatException ex)
            {
                MessageHelper.ShowError($"AES处理时发生错误：输入的十六进制格式无效。{ex.Message}");
            }
        }

        // AES解密：同上，仅把纯解密运算放后台
        private async void AesDecrypt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string input = AesResult.Text;
                if (string.IsNullOrEmpty(input))
                {
                    MessageHelper.ShowInfo("请输入要解密的文本");
                    return;
                }

                if (!TryGetAesParams(out byte[] key, out byte[]? iv, out CipherMode mode))
                {
                    return;
                }

                PaddingMode padding = AesPaddingComboBox.SelectedItem is AesPaddingOption paddingOption ? paddingOption.Padding : PaddingMode.PKCS7;
                bool inputIsHex = AesInputStringRadio.IsChecked != true;
                AesInput.Text = await Task.Run(() => AesCryptoService.Decrypt(input, key, iv, mode, padding, inputIsHex)).ConfigureAwait(true);
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException)
            {
                MessageHelper.ShowError($"AES解密时发生错误: {ex.Message}");
            }
            catch (FormatException ex)
            {
                MessageHelper.ShowError($"AES处理时发生错误：输入的十六进制格式无效。{ex.Message}");
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
                MessageHelper.ShowInfo("请输入密钥");
                return false;
            }

            // 验证密钥长度
            if (!IsValidKeyLength(keyInput, AesKeyStringRadio.IsChecked == true))
            {
                MessageHelper.ShowInfo("密钥长度不正确。支持16位、24位或32位密钥。");
                return false;
            }
            key = GetKeyBytes(keyInput, AesKeyStringRadio.IsChecked == true);

            if (AesModeComboBox.SelectedItem is not AesModeOption modeOption)
            {
                MessageHelper.ShowInfo("请选择加密模式");
                return false;
            }
            mode = modeOption.Mode;

            // 对于CBC/CFB等模式，需要IV向量
#pragma warning disable CA5358
            if (mode is CipherMode.CBC or CipherMode.CFB)
#pragma warning restore CA5358
            {
                string ivInput = AesIV.Text;
                if (string.IsNullOrEmpty(ivInput))
                {
                    MessageHelper.ShowInfo("当前模式需要IV向量");
                    return false;
                }

                // 验证IV长度（对于AES，IV长度应该是16字节）
                if (!IsValidIVLength(ivInput, AesIVStringRadio.IsChecked == true))
                {
                    MessageHelper.ShowInfo("IV向量长度不正确。应为16字节");
                    return false;
                }
                iv = GetIVBytes(ivInput, AesIVStringRadio.IsChecked == true);
            }

            return true;
        }

        // AES清空
        private void AesClear_Click(object sender, RoutedEventArgs e)        {
            AesInput.Clear();
            AesResult.Clear();
            AesKey.Clear();
            AesIV.Clear();
        }

        // 获取密钥字节数组
        private static byte[] GetKeyBytes(string keyInput, bool isString)
        {
            return ConvertUtils.InputBytes(keyInput, !isString);
        }

        // 获取IV字节数组
        private static byte[] GetIVBytes(string ivInput, bool isString)
        {
            // 不做截断/补齐：交由 IsValidIVLength 按字节长度校验，避免静默改变用户输入的 IV
            return ConvertUtils.InputBytes(ivInput, !isString);
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

        // 处理文件AES加密：读盘 + hex 编码移后台线程，UI 线程仅回填，避免近 50MB 文件生成 ~100MB 十六进制串卡界面
        private async void ProcessFileForAesEncryption(string filePath)
        {
            if (!FileDropHelper.IsWithinHexDisplayLimit(filePath))
            {
                MessageHelper.ShowWarning($"文件较大（超过 {FileDropHelper.HexDisplayWarnBytes / (1024 * 1024)} MB），转为十六进制显示会导致界面长时间无响应，已取消。");
                return;
            }

            int token = ++aesDropToken;
            try
            {
                string hex = await Task.Run(() => ConvertUtils.ToHexString(FileDropHelper.ReadAllBytes(filePath))).ConfigureAwait(true);
                if (token != aesDropToken)
                {
                    return; // 已有更晚的拖放在进行，丢弃本次过期结果，避免字段错配
                }

                // 将文件内容以 hex 显示在输入框，并切换到 Hex 模式
                AesInput.Text = hex;
                AesInputHexRadio.IsChecked = true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or NotSupportedException)
            {
                MessageHelper.ShowError($"处理文件时发生错误: {ex.Message}");
            }
            catch (OutOfMemoryException ex)
            {
                // 本方法为 Drop 处理器 fire-and-forget 调用的 async void：近 100MB 十六进制串在受限机器上可能 OOM，
                // 就地提示而非让异常冒泡到全局兜底弹通用错误框。
                MessageHelper.ShowError($"文件过大，转十六进制时内存不足: {ex.Message}");
            }
        }

    }
}