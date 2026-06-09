using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PersonalTools
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

            byte[] inputBytes = hexMode ? Utils.HexStringToByteArray(input) : Encoding.UTF8.GetBytes(input);
#pragma warning disable CA5401
            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
#pragma warning restore CA5401
            using MemoryStream msEncrypt = new();
            using (CryptoStream csEncrypt = new(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                csEncrypt.Write(inputBytes, 0, inputBytes.Length);
            } // 释放 CryptoStream 即触发 FlushFinalBlock，写出最终块(含 PKCS7 填充)后再取字节，避免丢尾块

            byte[] encryptedBytes = msEncrypt.ToArray();
            return Utils.ToHexString(encryptedBytes);
        }

        public static string Decrypt(string input, byte[] key, byte[]? iv, CipherMode mode, PaddingMode padding, bool hexMode)
        {
            byte[] encryptedBytes = Utils.HexStringToByteArray(input);

            using Aes aesAlg = CreateAes(key, iv, mode, padding);
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using MemoryStream msDecrypt = new(encryptedBytes);
            using CryptoStream csDecrypt = new(msDecrypt, decryptor, CryptoStreamMode.Read);
            using MemoryStream resultStream = new();
            csDecrypt.CopyTo(resultStream);
            byte[] decryptedBytes = resultStream.ToArray();

            // 按明文表示方式输出：字符串模式 → UTF-8 文本；Hex 模式 → 十六进制，避免二进制数据丢失
            return hexMode ? Utils.ToHexString(decryptedBytes) : Encoding.UTF8.GetString(decryptedBytes);
        }

        private static Aes CreateAes(byte[] key, byte[]? iv, CipherMode mode, PaddingMode padding)
        {
            Aes aesAlg = Aes.Create();
            aesAlg.KeySize = key.Length * 8; // 根据密钥长度设置 KeySize
            aesAlg.Key = key;
            if (iv != null)
            {
                aesAlg.IV = iv;
            }
            aesAlg.Mode = mode;
            aesAlg.Padding = padding;
            return aesAlg;
        }
    }
}
