using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using PersonalTools.Utils;
using PersonalTools.Utils.Hash;

namespace PersonalTools.UserControls
{
    internal sealed class Sha3AlgorithmOption(string name, int value)
    {
        public string Name { get; } = name;
        public int Value { get; } = value;
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
            ApplySha3PlatformSupport();
            HashItemsControl.ItemsSource = hashRows;
        }

        private void Grid_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            // 拖入文件时显示“复制”光标反馈，非文件拖放则禁止；与 FileTabHostControl/MarkdownToWordControl 行为一致
            e.Effects = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
                ? System.Windows.DragDropEffects.Copy
                : System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        // 初始化SHA3算法下拉框
        private void InitializeSHA3AlgorithmComboBox()
        {
            // 添加算法选项到下拉框
            SHA3AlgorithmComboBox.Items.Add(new Sha3AlgorithmOption("SHA3-256", 256));
            SHA3AlgorithmComboBox.Items.Add(new Sha3AlgorithmOption("SHA3-384", 384));
            SHA3AlgorithmComboBox.Items.Add(new Sha3AlgorithmOption("SHA3-512", 512));

            // 设置默认选中项
            SHA3AlgorithmComboBox.SelectedIndex = 0; // 默认选择SHA3-256
        }

        // 按平台能力启用/禁用 SHA3 交互：CNG SHA3 仅 Win11 build 25324+ 提供，
        // 目标框架 net10.0-windows7.0 可运行在更低版本 Windows 上，此时直接禁用相关控件并给出提示，
        // 避免用户点击后才在 Sha3.ComputeHash 抛 PlatformNotSupportedException
        private void ApplySha3PlatformSupport()
        {
            if (Sha3.IsSupported)
            {
                return;
            }

            // 不支持 SHA3：禁用输入框、模式单选、算法下拉框、计算/清空按钮，避免无效交互
            SHA3InputTextBox.IsEnabled = false;
            SHA3StringInputRadio.IsEnabled = false;
            SHA3HexInputRadio.IsEnabled = false;
            SHA3AlgorithmComboBox.IsEnabled = false;
            CalculateSHA3Button.IsEnabled = false;
            ClearSHA3Button.IsEnabled = false;

            // 结果 Label 给出明确提示文案，告知当前平台不可用
            SHA3ResultLabel.Content = "当前平台不支持 SHA3 算法（需 Windows 11 build 25324 及以上）";
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
                MessageHelper.ShowInfo($"请输入要计算{row.Name}的值");
                return;
            }

            try
            {
                byte[] inputBytes = ConvertUtils.InputBytes(row.InputText, row.IsHexMode);
                row.Result = ConvertUtils.ToHexString(row.HashFunc(inputBytes));
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException)
            {
                MessageHelper.ShowError($"计算{row.Name}时发生错误: {ex.Message}");
            }
        }

        // SHA3计算功能
        private void CalculateSHA3_Click(object sender, RoutedEventArgs e)
        {
            string input = SHA3InputTextBox.Text;
            if (string.IsNullOrEmpty(input))
            {
                MessageHelper.ShowInfo("请输入要计算SHA3的值");
                return;
            }

            try
            {
                // 仅 Hex 单选钮被选中时按十六进制解析，其余（含未选）默认 UTF-8
                byte[] inputBytes = ConvertUtils.InputBytes(input, SHA3HexInputRadio.IsChecked == true);

                // 获取选中的算法选项
                if (SHA3AlgorithmComboBox.SelectedItem is not Sha3AlgorithmOption selectedOption)
                {
                    MessageHelper.ShowInfo("请选择SHA3算法类型");
                    return;
                }

                // 使用真正的SHA3算法（静态调用，无需实例化）
                byte[] hashBytes = Sha3.ComputeHash(inputBytes, selectedOption.Value);
                SHA3ResultLabel.Content = ConvertUtils.ToHexString(hashBytes);
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException or PlatformNotSupportedException)
            {
                // PlatformNotSupportedException：低版本 Windows 缺少 CNG SHA3，展示 Sha3.cs 写好的“当前平台不支持SHA3算法”提示
                MessageHelper.ShowError($"计算SHA3时发生错误: {ex.Message}");
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
            string? filePath = FileDropHelper.GetFirstDroppedFile(e);
            if (filePath != null)
            {
                ProcessFileForHashCalculation(filePath);
            }
        }

        // 处理文件哈希计算：读盘 + 各哈希计算 + hex 编码移后台线程，UI 线程仅回填结果，避免大文件卡界面
        private async void ProcessFileForHashCalculation(string filePath)
        {
            try
            {
                (string[] hashes, string sha3Hex) = await Task.Run(() =>
                {
                    byte[] fileBytes = FileDropHelper.ReadAllBytes(filePath);
                    string[] results = new string[hashRows.Count];
                    for (int i = 0; i < hashRows.Count; i++)
                    {
                        results[i] = ConvertUtils.ToHexString(hashRows[i].HashFunc(fileBytes));
                    }

                    return (results, ConvertUtils.ToHexString(fileBytes));
                }).ConfigureAwait(true);

                for (int i = 0; i < hashRows.Count; i++)
                {
                    hashRows[i].Result = hashes[i];
                }

                // 将文件内容（hex）显示在 SHA3 输入框，并切换到 hex 模式
                SHA3InputTextBox.Text = sha3Hex;
                SHA3HexInputRadio.IsChecked = true;

                FileDropHint.Text = $"已加载文件: {Path.GetFileName(filePath)}，请在下方选择SHA3算法类型并计算";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                MessageHelper.ShowError($"处理文件时发生错误: {ex.Message}");
            }
        }
    }
}