using System;
using System.Collections.Generic;
using System.Linq;
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

        // CRC算法定义
        public static readonly List<CRCAlgorithm> CRCAlgorithms = new List<CRCAlgorithm>
        {
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
        };

        // CRC计算器
        public class CRCCalculator
        {
            private readonly CRCAlgorithm _algorithm;

            public CRCCalculator(CRCAlgorithm algorithm)
            {
                _algorithm = algorithm;
            }

            public uint Compute(byte[] data)
            {
                uint crc = _algorithm.InitialValue;
                uint mask = (uint)((1UL << _algorithm.Width) - 1);
                uint topBit = (uint)(1UL << (_algorithm.Width - 1));

                for (int i = 0; i < data.Length; i++)
                {
                    byte value = (byte)(_algorithm.ReverseInput ? ReverseBits(data[i], 8) : data[i]);
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
                    crc = ReverseBits(crc, _algorithm.Width);
                }

                crc ^= _algorithm.FinalXor;
                crc &= mask;

                return crc;
            }

            private uint ReverseBits(uint value, int width)
            {
                uint result = 0;
                for (int i = 0; i < width; i++)
                {
                    result <<= 1;
                    result |= value & 1;
                    value >>= 1;
                }
                return result;
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
    }
}