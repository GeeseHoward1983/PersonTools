using System.IO;
using System.Text;

namespace MyTool
{
    public partial class MainWindow : System.Windows.Window
    {
        // CRC算法参数模型
        public class CRCAlgorithm
        {
            public required string Name { get; set; }
            public required string PolynomialFormula { get; set; }
            public int Width { get; set; }
            public uint Polynomial { get; set; }
            public uint InitialValue { get; set; }
            public uint FinalXor { get; set; }
            public bool ReverseInput { get; set; }
            public bool ReverseOutput { get; set; }
        }

        // 初始化CRC算法下拉框
        private void InitializeCRCAlgorithmComboBox()
        {
            // 绑定算法列表到下拉框
            CRCAlgorithmComboBox.ItemsSource = CRCAlgorithms;
            CRCAlgorithmComboBox.DisplayMemberPath = "Name";
            CRCAlgorithmComboBox.SelectedIndex = 0;
        }

        // CRC算法定义
        public static readonly List<CRCAlgorithm> CRCAlgorithms =
        [
            new CRCAlgorithm { Name = "CRC-8", PolynomialFormula = "x8 + x2 + x + 1", Width = 8, Polynomial = 0x07, InitialValue = 0x00, FinalXor = 0x00, ReverseInput = false, ReverseOutput = false },
            new CRCAlgorithm { Name = "CRC-8/ITU", PolynomialFormula = "x8 + x2 + x + 1", Width = 8, Polynomial = 0x07, InitialValue = 0x00, FinalXor = 0x55, ReverseInput = false, ReverseOutput = false },
            new CRCAlgorithm { Name = "CRC-8/ROHC", PolynomialFormula = "x8 + x2 + x + 1", Width = 8, Polynomial = 0x07, InitialValue = 0xFF, FinalXor = 0x00, ReverseInput = true, ReverseOutput = true },
            new CRCAlgorithm { Name = "CRC-8/MAXIM", PolynomialFormula = "x8 + x5 + x4 + 1", Width = 8, Polynomial = 0x31, InitialValue = 0x00, FinalXor = 0x00, ReverseInput = true, ReverseOutput = true },
            new CRCAlgorithm { Name = "CRC-16/IBM", PolynomialFormula = "x16 + x15 + x2 + 1", Width = 16, Polynomial = 0x8005, InitialValue = 0x0000, FinalXor = 0x0000, ReverseInput = true, ReverseOutput = true },
            new CRCAlgorithm { Name = "CRC-16/MAXIM", PolynomialFormula = "x16 + x15 + x2 + 1", Width = 16, Polynomial = 0x8005, InitialValue = 0x0000, FinalXor = 0xFFFF, ReverseInput = true, ReverseOutput = true },
            new CRCAlgorithm { Name = "CRC-16/USB", PolynomialFormula = "x16 + x15 + x2 + 1", Width = 16, Polynomial = 0x8005, InitialValue = 0xFFFF, FinalXor = 0xFFFF, ReverseInput = true, ReverseOutput = true },
            new CRCAlgorithm { Name = "CRC-16/MODBUS", PolynomialFormula = "x16 + x15 + x2 + 1", Width = 16, Polynomial = 0x8005, InitialValue = 0xFFFF, FinalXor = 0x0000, ReverseInput = true, ReverseOutput = true },
            new CRCAlgorithm { Name = "CRC-16/CCITT", PolynomialFormula = "x16 + x12 + x5 + 1", Width = 16, Polynomial = 0x1021, InitialValue = 0x0000, FinalXor = 0x0000, ReverseInput = true, ReverseOutput = true },
            new CRCAlgorithm { Name = "CRC-16/CCITT-FALSE", PolynomialFormula = "x16 + x12 + x5 + 1", Width = 16, Polynomial = 0x1021, InitialValue = 0xFFFF, FinalXor = 0x0000, ReverseInput = false, ReverseOutput = false },
            new CRCAlgorithm { Name = "CRC-16/X25", PolynomialFormula = "x16 + x12 + x5 + 1", Width = 16, Polynomial = 0x1021, InitialValue = 0xFFFF, FinalXor = 0xFFFF, ReverseInput = true, ReverseOutput = true },
            new CRCAlgorithm { Name = "CRC-16/XMODEM", PolynomialFormula = "x16 + x12 + x5 + 1", Width = 16, Polynomial = 0x1021, InitialValue = 0x0000, FinalXor = 0x0000, ReverseInput = false, ReverseOutput = false },
            new CRCAlgorithm { Name = "CRC-16/DNP", PolynomialFormula = "x16 + x13 + x12 + x11 + x10 + x8 + x6 + x5 + x2 + 1", Width = 16, Polynomial = 0x3D65, InitialValue = 0x0000, FinalXor = 0xFFFF, ReverseInput = true, ReverseOutput = true },
            new CRCAlgorithm { Name = "CRC-32", PolynomialFormula = "x32 + x26 + x23 + x22 + x16 + x12 + x11 + x10 + x8 + x7 + x5 + x4 + x2 + x + 1", Width = 32, Polynomial = 0x04C11DB7, InitialValue = 0xFFFFFFFF, FinalXor = 0xFFFFFFFF, ReverseInput = true, ReverseOutput = true },
            new CRCAlgorithm { Name = "CRC-32/MPEG-2", PolynomialFormula = "x32 + x26 + x23 + x22 + x16 + x12 + x11 + x10 + x8 + x7 + x5 + x4 + x2 + x + 1", Width = 32, Polynomial = 0x04C11DB7, InitialValue = 0xFFFFFFFF, FinalXor = 0x00000000, ReverseInput = false, ReverseOutput = false }
        ];

        // CRC计算器
        public class CRCCalculator(MainWindow.CRCAlgorithm algorithm)
        {
            private readonly CRCAlgorithm _algorithm = algorithm;

            public uint Compute(byte[] data)
            {
                uint crc = _algorithm.InitialValue;
                uint mask = (uint)((1UL << _algorithm.Width) - 1);
                uint topBit = (uint)(1UL << (_algorithm.Width - 1));

                for (int i = 0; i < data.Length; i++)
                {
                    byte value = (byte)(_algorithm.ReverseInput ? Utils.ReverseBits(data[i], 8) : data[i]);
                    crc ^= (uint)(value << (_algorithm.Width - 8));

                    for (int j = 0; j < 8; j++)
                    {
                        if ((crc & topBit) != 0)
                        {
                            crc = (crc << 1) ^ _algorithm.Polynomial;
                        }
                        else
                        {
                            crc <<= 1;
                        }
                        crc &= mask;
                    }
                }

                if (_algorithm.ReverseOutput)
                {
                    crc = Utils.ReverseBits(crc, _algorithm.Width);
                }

                crc ^= _algorithm.FinalXor;
                crc &= mask;

                return crc;
            }

        }

        // CRC计算功能
        private void CalculateCRC_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (CRCAlgorithmComboBox.SelectedItem == null)
            {
                System.Windows.MessageBox.Show("请选择一个CRC算法", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            string input = CRCInputTextBox.Text;
            if (string.IsNullOrEmpty(input))
            {
                System.Windows.MessageBox.Show("请输入要计算CRC的值", "提示", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            try
            {
                var selectedAlgorithm = (CRCAlgorithm)CRCAlgorithmComboBox.SelectedItem;
                byte[] inputBytes;

                if (CRCHexInputRadio.IsChecked == true)
                {
                    // Hex字符串模式
                    inputBytes = Utils.HexStringToByteArray(input);
                }
                else
                {
                    // 默认使用普通字符串模式
                    inputBytes = Encoding.UTF8.GetBytes(input);
                }

                var calculator = new CRCCalculator(selectedAlgorithm);
                uint crcResult = calculator.Compute(inputBytes);

                // 根据算法宽度格式化输出
                string formatString = selectedAlgorithm.Width <= 8 ? "X2" : selectedAlgorithm.Width <= 16 ? "X4" : "X8";
                CRCResultLabel.Content = crcResult.ToString(formatString);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"计算CRC时发生错误: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        // 清空CRC计算结果
        private void ClearCRC_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            CRCInputTextBox.Clear();
            CRCResultLabel.Content = "等待计算...";
        }

        // 处理CRC标签页的文件拖放事件
        private void CrcTab_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string filePath = files[0]; // 只处理第一个文件
                    ProcessFileForCrcCalculation(filePath);
                }
            }
        }

        // 预览拖拽事件，确保拖拽事件不被子控件拦截
        private void Grid_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;
        }

        // 处理文件CRC计算
        private void ProcessFileForCrcCalculation(string filePath)
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

                // 将文件内容转换为hex字符串显示在输入框中
                string hexString = BitConverter.ToString(fileBytes).Replace("-", "");
                CRCInputTextBox.Text = hexString;

                // 设置为hex输入模式
                CRCHexInputRadio.IsChecked = true;

            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"处理文件时发生错误: {ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}