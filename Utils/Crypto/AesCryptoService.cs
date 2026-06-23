using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PersonalTools.Utils.Crypto
{
    /// <summary>
    /// AES 加解密核心，与 UI 无关。把 Aes/CryptoStream 等加密类型从控件中剥离，降低控件类耦合。
    /// hexMode：明文一侧是否按十六进制处理（加密时输入、解密时输出）。密文一律为十六进制字符串。
    /// </summary>
    internal static class AesCryptoService
    {
        public static string Encrypt(string input, byte[] key, byte[]? iv, CipherMode mode, PaddingMode padding, bool hexMode)
        {
            using Aes aesAlg = CreateAes(key, iv, mode, padding);

            byte[] inputBytes = ConvertUtils.InputBytes(input, hexMode);
#pragma warning disable CA5401
            using ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
#pragma warning restore CA5401
            using MemoryStream msEncrypt = new();
            using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                csEncrypt.Write(inputBytes, 0, inputBytes.Length);
            } // 释放 CryptoStream 即触发 FlushFinalBlock，写出最终块(含 PKCS7 填充)后再取字节，避免丢尾块

            byte[] encryptedBytes = msEncrypt.ToArray();
            return ConvertUtils.ToHexString(encryptedBytes);
        }

        public static string Decrypt(string input, byte[] key, byte[]? iv, CipherMode mode, PaddingMode padding, bool hexMode)
        {
            byte[] encryptedBytes = ConvertUtils.HexStringToByteArray(input);

            using Aes aesAlg = CreateAes(key, iv, mode, padding);
            using ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using MemoryStream msDecrypt = new(encryptedBytes);
            using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
            using MemoryStream resultStream = new();
            csDecrypt.CopyTo(resultStream);
            byte[] decryptedBytes = resultStream.ToArray();

            // 按明文表示方式输出：字符串模式 → UTF-8 文本；Hex 模式 → 十六进制，避免二进制数据丢失
            return ConvertUtils.OutputString(decryptedBytes, hexMode);
        }

        private static Aes CreateAes(byte[] key, byte[]? iv, CipherMode mode, PaddingMode padding)
        {
            // 需要 IV 的模式必须显式提供 IV：否则 Aes.Create() 会用随机 IV 加密却无法回传，
            // 导致密文不可解密。此处强制校验，不静默兜底（UI 层虽已校验，服务层亦需自洽）
#pragma warning disable CA5358 // 模式由调用方选择，此处仅做 IV/反馈校验，不引入新的弱模式
            if (iv == null && mode is CipherMode.CBC or CipherMode.CFB or CipherMode.OFB)
            {
                throw new ArgumentException($"加密模式 {mode} 必须提供 IV 向量", nameof(iv));
            }

            Aes aesAlg = Aes.Create();
            aesAlg.KeySize = key.Length * 8; // 根据密钥长度设置 KeySize
            aesAlg.Key = key;
            if (iv != null)
            {
                // AES 块大小固定 128 位，IV 必须恰为 16 字节。提前显式校验，兑现"服务层亦自洽"的契约，
                // 否则 aesAlg.IV = iv 仅在长度不符时才抛 CryptographicException，错误信息不直观。
                if (iv.Length != 16)
                {
                    throw new ArgumentException($"AES IV 长度必须为 16 字节，实际 {iv.Length}", nameof(iv));
                }
                aesAlg.IV = iv;
            }
            aesAlg.Mode = mode;
            aesAlg.Padding = padding;
            // CFB/OFB 显式设为 128 位反馈，与业界主流 CFB-128/OFB-128 一致
            // （.NET 默认 CFB8/OFB8，每次仅反馈 8 位，会与 OpenSSL 等外部密文不兼容）
            if (mode is CipherMode.CFB or CipherMode.OFB)
            {
                aesAlg.FeedbackSize = 128;
            }
#pragma warning restore CA5358
            return aesAlg;
        }
    }
}
