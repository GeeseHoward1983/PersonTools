using System.Buffers.Binary;

namespace PersonalTools.Utils.Hash
{
    /// <summary>
    /// SHA-224 (FIPS 180-4) 实现。
    /// .NET 的 System.Security.Cryptography 未内置 SHA-224，这里基于 SHA-256 压缩函数 +
    /// SHA-224 专属初值实现真正的 SHA-224（并非对 SHA-256 结果的简单截断）。
    /// </summary>
    internal static class Sha224
    {
        // SHA-256/224 轮常量
        private static readonly uint[] K =
        [
            0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
            0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
            0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
            0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
            0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
            0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
            0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
            0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
        ];

        /// <summary>计算输入数据的 SHA-224 摘要（28 字节）。</summary>
        public static byte[] HashData(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            // 防 paddedLength(int) 溢出：输入接近 int.MaxValue 时 data.Length+1+8+对齐 会回绕为负导致分配错误。
            // 预留 72 字节(0x80 + 最长填充 + 8 字节长度)余量，超限明确拒绝而非产生错误结果/OverflowException
            if (data.Length > int.MaxValue - 72)
            {
                throw new ArgumentException("输入数据过大，无法计算 SHA-224", nameof(data));
            }

            // SHA-224 初始哈希值
            uint h0 = 0xc1059ed8, h1 = 0x367cd507, h2 = 0x3070dd17, h3 = 0xf70e5939,
                 h4 = 0xffc00b31, h5 = 0x68581511, h6 = 0x64f98fa7, h7 = 0xbefa4fa4;

            // 预处理：追加 0x80，补 0 到长度 ≡ 56 (mod 64)，再追加 64 位大端比特长度
            long bitLength = (long)data.Length * 8;
            int paddedLength = data.Length + 1;
            while (paddedLength % 64 != 56)
            {
                paddedLength++;
            }
            paddedLength += 8;

            byte[] padded = new byte[paddedLength];
            Array.Copy(data, padded, data.Length);
            padded[data.Length] = 0x80;
            BinaryPrimitives.WriteInt64BigEndian(padded.AsSpan(paddedLength - 8), bitLength);

            Span<uint> w = stackalloc uint[64];
            for (int chunk = 0; chunk < paddedLength; chunk += 64)
            {
                for (int i = 0; i < 16; i++)
                {
                    w[i] = BinaryPrimitives.ReadUInt32BigEndian(padded.AsSpan(chunk + (i * 4)));
                }
                for (int i = 16; i < 64; i++)
                {
                    uint s0 = RotR(w[i - 15], 7) ^ RotR(w[i - 15], 18) ^ (w[i - 15] >> 3);
                    uint s1 = RotR(w[i - 2], 17) ^ RotR(w[i - 2], 19) ^ (w[i - 2] >> 10);
                    w[i] = w[i - 16] + s0 + w[i - 7] + s1;
                }

                uint a = h0, b = h1, c = h2, d = h3, e = h4, f = h5, g = h6, h = h7;
                for (int i = 0; i < 64; i++)
                {
                    uint bigS1 = RotR(e, 6) ^ RotR(e, 11) ^ RotR(e, 25);
                    uint ch = (e & f) ^ (~e & g);
                    uint temp1 = h + bigS1 + ch + K[i] + w[i];
                    uint bigS0 = RotR(a, 2) ^ RotR(a, 13) ^ RotR(a, 22);
                    uint maj = (a & b) ^ (a & c) ^ (b & c);
                    uint temp2 = bigS0 + maj;

                    h = g;
                    g = f;
                    f = e;
                    e = d + temp1;
                    d = c;
                    c = b;
                    b = a;
                    a = temp1 + temp2;
                }

                h0 += a; h1 += b; h2 += c; h3 += d;
                h4 += e; h5 += f; h6 += g; h7 += h;
            }

            // 输出前 7 个字（224 位），大端
            byte[] result = new byte[28];
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(0), h0);
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(4), h1);
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(8), h2);
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(12), h3);
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(16), h4);
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(20), h5);
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(24), h6);
            return result;
        }

        private static uint RotR(uint x, int n)
        {
            return (x >> n) | (x << (32 - n));
        }
    }
}
