namespace PersonalTools.Utils.Hash
{
    /// <summary>CRC 算法参数。</summary>
    internal sealed class CrcAlgorithm
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

    /// <summary>
    /// CRC 计算核心（与 UI 无关）：内置常用算法表 + 位串行计算，
    /// 把 CRC 算法域从 CrcComputeControl 中剥离，降低控件类耦合。
    /// </summary>
    internal static class CrcCalculator
    {
        public static IReadOnlyList<CrcAlgorithm> Algorithms { get; } =
        [
            new() { Name = "CRC-8", PolynomialFormula = "x8 + x2 + x + 1", Width = 8, Polynomial = 0x07, InitialValue = 0x00, FinalXor = 0x00, ReverseInput = false, ReverseOutput = false },
            new() { Name = "CRC-8/ITU", PolynomialFormula = "x8 + x2 + x + 1", Width = 8, Polynomial = 0x07, InitialValue = 0x00, FinalXor = 0x55, ReverseInput = false, ReverseOutput = false },
            new() { Name = "CRC-8/ROHC", PolynomialFormula = "x8 + x2 + x + 1", Width = 8, Polynomial = 0x07, InitialValue = 0xFF, FinalXor = 0x00, ReverseInput = true, ReverseOutput = true },
            new() { Name = "CRC-8/MAXIM", PolynomialFormula = "x8 + x5 + x4 + 1", Width = 8, Polynomial = 0x31, InitialValue = 0x00, FinalXor = 0x00, ReverseInput = true, ReverseOutput = true },
            new() { Name = "CRC-16/IBM", PolynomialFormula = "x16 + x15 + x2 + 1", Width = 16, Polynomial = 0x8005, InitialValue = 0x0000, FinalXor = 0x0000, ReverseInput = true, ReverseOutput = true },
            new() { Name = "CRC-16/MAXIM", PolynomialFormula = "x16 + x15 + x2 + 1", Width = 16, Polynomial = 0x8005, InitialValue = 0x0000, FinalXor = 0xFFFF, ReverseInput = true, ReverseOutput = true },
            new() { Name = "CRC-16/USB", PolynomialFormula = "x16 + x15 + x2 + 1", Width = 16, Polynomial = 0x8005, InitialValue = 0xFFFF, FinalXor = 0xFFFF, ReverseInput = true, ReverseOutput = true },
            new() { Name = "CRC-16/MODBUS", PolynomialFormula = "x16 + x15 + x2 + 1", Width = 16, Polynomial = 0x8005, InitialValue = 0xFFFF, FinalXor = 0x0000, ReverseInput = true, ReverseOutput = true },
            new() { Name = "CRC-16/CCITT", PolynomialFormula = "x16 + x12 + x5 + 1", Width = 16, Polynomial = 0x1021, InitialValue = 0x0000, FinalXor = 0x0000, ReverseInput = true, ReverseOutput = true },
            new() { Name = "CRC-16/CCITT-FALSE", PolynomialFormula = "x16 + x12 + x5 + 1", Width = 16, Polynomial = 0x1021, InitialValue = 0xFFFF, FinalXor = 0x0000, ReverseInput = false, ReverseOutput = false },
            new() { Name = "CRC-16/X25", PolynomialFormula = "x16 + x12 + x5 + 1", Width = 16, Polynomial = 0x1021, InitialValue = 0xFFFF, FinalXor = 0xFFFF, ReverseInput = true, ReverseOutput = true },
            new() { Name = "CRC-16/XMODEM", PolynomialFormula = "x16 + x12 + x5 + 1", Width = 16, Polynomial = 0x1021, InitialValue = 0x0000, FinalXor = 0x0000, ReverseInput = false, ReverseOutput = false },
            new() { Name = "CRC-16/DNP", PolynomialFormula = "x16 + x13 + x12 + x11 + x10 + x8 + x6 + x5 + x2 + 1", Width = 16, Polynomial = 0x3D65, InitialValue = 0x0000, FinalXor = 0xFFFF, ReverseInput = true, ReverseOutput = true },
            new() { Name = "CRC-32", PolynomialFormula = "x32 + x26 + x23 + x22 + x16 + x12 + x11 + x10 + x8 + x7 + x5 + x4 + x2 + x + 1", Width = 32, Polynomial = 0x04C11DB7, InitialValue = 0xFFFFFFFF, FinalXor = 0xFFFFFFFF, ReverseInput = true, ReverseOutput = true },
            new() { Name = "CRC-32/MPEG-2", PolynomialFormula = "x32 + x26 + x23 + x22 + x16 + x12 + x11 + x10 + x8 + x7 + x5 + x4 + x2 + x + 1", Width = 32, Polynomial = 0x04C11DB7, InitialValue = 0xFFFFFFFF, FinalXor = 0x00000000, ReverseInput = false, ReverseOutput = false },
        ];

        public static uint Compute(byte[] data, CrcAlgorithm algo)
        {
            uint crc = algo.InitialValue;
            uint mask = (uint)((1UL << algo.Width) - 1);
            uint topBit = (uint)(1UL << (algo.Width - 1));

            for (int i = 0; i < data.Length; i++)
            {
                byte value = algo.ReverseInput switch
                {
                    true => (byte)ConvertUtils.ReverseBits(data[i], 8),
                    false => data[i]
                };
                crc ^= (uint)(value << (algo.Width - 8));

                for (int j = 0; j < 8; j++)
                {
                    if ((crc & topBit) != 0)
                    {
                        crc = (crc << 1) ^ algo.Polynomial;
                    }
                    else
                    {
                        crc <<= 1;
                    }
                    crc &= mask;
                }
            }

            if (algo.ReverseOutput)
            {
                crc = ConvertUtils.ReverseBits(crc, algo.Width);
            }

            crc ^= algo.FinalXor;
            crc &= mask;

            return crc;
        }
    }
}
