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

            // 防长度计算溢出：输入接近 int.MaxValue 时 (long)Length*8 仍安全，但仍保留上限以对齐既有契约
            if (data.Length > int.MaxValue - 72)
            {
                throw new ArgumentException("输入数据过大，无法计算 SHA-224", nameof(data));
            }

            // SHA-224 初始哈希值
            Span<uint> h = stackalloc uint[8];
            h[0] = 0xc1059ed8; h[1] = 0x367cd507; h[2] = 0x3070dd17; h[3] = 0xf70e5939;
            h[4] = 0xffc00b31; h[5] = 0x68581511; h[6] = 0x64f98fa7; h[7] = 0xbefa4fa4;

            Span<uint> w = stackalloc uint[64];
            ReadOnlySpan<byte> dataSpan = data;

            // 直接按 64 字节块压缩输入，不再把整段输入复制进一份等长 padded 缓冲，
            // 避免大文件(如 1GB)哈希时内存翻倍甚至 OutOfMemoryException。
            int fullBlocks = data.Length / 64;
            for (int b = 0; b < fullBlocks; b++)
            {
                ProcessBlock(dataSpan.Slice(b * 64, 64), w, h);
            }

            // 尾块：剩余字节 + 0x80 + 补零 + 64 位大端比特长度，仅需 64 或 128 字节小缓冲
            int remainder = data.Length - (fullBlocks * 64);
            long bitLength = (long)data.Length * 8;
            Span<byte> tail = stackalloc byte[128];
            tail.Clear();
            dataSpan.Slice(fullBlocks * 64, remainder).CopyTo(tail);
            tail[remainder] = 0x80;
            int tailLen = remainder < 56 ? 64 : 128; // 剩余≥56 时长度字段放不下，须再占一个块
            BinaryPrimitives.WriteInt64BigEndian(tail.Slice(tailLen - 8, 8), bitLength);
            for (int b = 0; b * 64 < tailLen; b++)
            {
                ProcessBlock(tail.Slice(b * 64, 64), w, h);
            }

            // 输出前 7 个字（224 位），大端
            byte[] result = new byte[28];
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(0), h[0]);
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(4), h[1]);
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(8), h[2]);
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(12), h[3]);
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(16), h[4]);
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(20), h[5]);
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(24), h[6]);
            return result;
        }

        // 压缩单个 64 字节消息块，就地更新 8 字哈希状态 h
        private static void ProcessBlock(ReadOnlySpan<byte> block, Span<uint> w, Span<uint> h)
        {
            for (int i = 0; i < 16; i++)
            {
                w[i] = BinaryPrimitives.ReadUInt32BigEndian(block.Slice(i * 4));
            }
            for (int i = 16; i < 64; i++)
            {
                uint s0 = RotR(w[i - 15], 7) ^ RotR(w[i - 15], 18) ^ (w[i - 15] >> 3);
                uint s1 = RotR(w[i - 2], 17) ^ RotR(w[i - 2], 19) ^ (w[i - 2] >> 10);
                w[i] = w[i - 16] + s0 + w[i - 7] + s1;
            }

            uint a = h[0], b = h[1], c = h[2], d = h[3], e = h[4], f = h[5], g = h[6], hh = h[7];
            for (int i = 0; i < 64; i++)
            {
                uint bigS1 = RotR(e, 6) ^ RotR(e, 11) ^ RotR(e, 25);
                uint ch = (e & f) ^ (~e & g);
                uint temp1 = hh + bigS1 + ch + K[i] + w[i];
                uint bigS0 = RotR(a, 2) ^ RotR(a, 13) ^ RotR(a, 22);
                uint maj = (a & b) ^ (a & c) ^ (b & c);
                uint temp2 = bigS0 + maj;

                hh = g;
                g = f;
                f = e;
                e = d + temp1;
                d = c;
                c = b;
                b = a;
                a = temp1 + temp2;
            }

            h[0] += a; h[1] += b; h[2] += c; h[3] += d;
            h[4] += e; h[5] += f; h[6] += g; h[7] += hh;
        }

        private static uint RotR(uint x, int n)
        {
            return (x >> n) | (x << (32 - n));
        }
    }
}
