using System.IO;
using System.Security.Cryptography;

namespace PersonalTools.Utils.Crypto
{
    /// <summary>
    /// AES 加解密核心，与 UI 无关。把 Aes/CryptoStream 等加密类型从控件中剥离，降低控件类耦合。
    /// hexMode：明文一侧是否按十六进制处理（加密时输入、解密时输出）。密文一律为十六进制字符串。
    /// </summary>
    internal static class AesCryptoService
    {
        private static readonly Aes aesAlg = Aes.Create();
        public static string Encrypt(string input, byte[] key, byte[]? iv, CipherMode mode, PaddingMode padding, bool hexMode)
        {
            InitAes(key, iv, mode, padding);

            byte[] inputBytes = ConvertUtils.InputBytes(input, hexMode);
#pragma warning disable CA5401
            using MemoryStream msEncrypt = new();
            using (CryptoStream csEncrypt = new(msEncrypt, aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV), CryptoStreamMode.Write))
            {
                csEncrypt.Write(inputBytes, 0, inputBytes.Length);
            } // 释放 CryptoStream 即触发 FlushFinalBlock，写出最终块(含 PKCS7 填充)后再取字节，避免丢尾块

            return ConvertUtils.ToHexString(msEncrypt.ToArray());
        }

        public static string Decrypt(string input, byte[] key, byte[]? iv, CipherMode mode, PaddingMode padding, bool hexMode)
        {
            InitAes(key, iv, mode, padding);

            using MemoryStream msDecrypt = new(ConvertUtils.HexStringToByteArray(input));
            using CryptoStream csDecrypt = new(msDecrypt, aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV), CryptoStreamMode.Read);
            using MemoryStream resultStream = new();
            csDecrypt.CopyTo(resultStream);

            // 按明文表示方式输出：字符串模式 → UTF-8 文本；Hex 模式 → 十六进制，避免二进制数据丢失
            return ConvertUtils.OutputString(resultStream.ToArray(), hexMode);
        }

#pragma warning disable CA5358 // 模式由调用方选择，此处仅做 IV/反馈校验，不引入新的弱模式
        private static void CheckIV(byte[]? iv, CipherMode mode)
        {
            // 需要 IV 的模式必须显式提供 IV：否则 Aes.Create() 会用随机 IV 加密却无法回传，
            // 导致密文不可解密。此处强制校验，不静默兜底（UI 层虽已校验，服务层亦需自洽）
            bool _ = iv switch
            {
                null => CheckMode(mode),
                _ => iv.Length switch
                {
                    16 => true,
                    _ => throw new ArgumentException($"IV 向量长度必须为 16 字节，实际 {iv.Length}", nameof(iv)),
                }
            };
            SetIV(iv);
            SetMode(mode);
        }
        private static bool CheckMode(CipherMode mode)
        {
            // 需要 IV 的模式必须显式提供 IV：否则 Aes.Create() 会用随机 IV 加密却无法回传，
            // 导致密文不可解密。此处强制校验，不静默兜底（UI 层虽已校验，服务层亦需自洽）
            return mode switch
            {
                CipherMode.CBC or CipherMode.CFB => true,
                _ => throw new ArgumentException($"加密模式 {mode} 必须提供 IV 向量"),
            };
        }

        private static void SetKey(byte[] key)
        {
            aesAlg.SetKey(key);
        }

        private static void SetIV(byte[]? iv)
        {
            aesAlg.IV = iv switch
            {
                null => aesAlg.IV,
                _ => iv,
            };
        }

        private static void SetPadding(PaddingMode padding)
        {
            aesAlg.Padding = padding;
        }

        private static void SetFeedbackSize(CipherMode mode)
        {
            // CFB 显式设为 128 位反馈，与业界主流 CFB-128 一致
            // （.NET 默认 CFB8，每次仅反馈 8 位，会与 OpenSSL 等外部密文不兼容）
            aesAlg.FeedbackSize = mode switch
            {
                CipherMode.CFB => 128,
                _ => aesAlg.BlockSize, // 其他模式反馈大小无意义，保持默认即可
            };
        }

        private static void SetMode(CipherMode mode) 
        {
            //try
            //{
            aesAlg.Mode = mode;
            //}
            //catch (CryptographicException ex)
            //{
            //    throw new ArgumentException($".NET 内置 AES 不支持加密模式 {mode}，请改用 CBC/CFB/ECB", nameof(mode), ex);
            //}
        }

        private static void InitAes(byte[] key, byte[]? iv, CipherMode mode, PaddingMode padding)
        {
            CheckIV(iv, mode);
            SetKey(key);
            SetPadding(padding);
            SetFeedbackSize(mode);
        }
    }
}
