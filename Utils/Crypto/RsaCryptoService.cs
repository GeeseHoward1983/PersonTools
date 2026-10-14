using System.Security.Cryptography;

namespace PersonalTools.Utils.Crypto
{
    /// <summary>
    /// RSA 加解密/签名核心，与 UI 无关。把 RSA 等加密类型从控件中剥离，降低控件类耦合。
    /// 加密/解密使用 OAEP-SHA256，签名/验签使用 PKCS#1 + SHA256。isString=true 时明文按 UTF-8，否则按十六进制。
    /// </summary>
    internal static class RsaCryptoService
    {
        private static readonly RSA rsa = RSA.Create();

        public static string Encrypt(string input, string publicKey, bool isString)
        {
            try
            {
                rsa.ImportFromPem(publicKey);
                return ConvertUtils.ToHexString(rsa.Encrypt(ConvertUtils.InputBytes(input, !isString), RSAEncryptionPadding.OaepSHA256));
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException or FormatException)
            {
                AppLogger.Log($"RSA 加密失败: {ex}");
                throw new CryptographicException("导入公钥或加密失败，请检查公钥格式与输入数据。", ex);
            }
        }

        public static string Decrypt(string input, string privateKey, bool isString)
        {
            try
            {
                rsa.ImportFromPem(privateKey);
                return ConvertUtils.OutputString(rsa.Decrypt(ConvertUtils.HexStringToByteArray(input), RSAEncryptionPadding.OaepSHA256), !isString);
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException or FormatException)
            {
                AppLogger.Log($"RSA 解密失败: {ex}");
                throw new CryptographicException("导入私钥或解密失败，请检查私钥格式与密文。", ex);
            }
        }

        public static string Sign(string input, string privateKey, bool isString)
        {
            try
            {
                rsa.ImportFromPem(privateKey);
                return ConvertUtils.ToHexString(rsa.SignData(ConvertUtils.InputBytes(input, !isString), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException or FormatException)
            {
                AppLogger.Log($"RSA 签名失败: {ex}");
                throw new CryptographicException("导入私钥或签名失败，请检查私钥格式与输入数据。", ex);
            }
        }

        public static bool Verify(string input, string signature, string publicKey, bool isString)
        {
            try
            {
                rsa.ImportFromPem(publicKey);
                return rsa.VerifyData(ConvertUtils.InputBytes(input, !isString), ConvertUtils.HexStringToByteArray(signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException or FormatException)
            {
                AppLogger.Log($"RSA 验签输入无效: {ex}");
                throw new CryptographicException("导入公钥或解析输入/签名失败，请检查公钥格式与输入数据。", ex);
            }
        }

        public static bool CheckKeySize(int keySize)
        {
            return keySize switch
            {
                >= 512 and <= 16384 and var size when size % 8 == 0 => true,
                _ => throw new ArgumentException($"RSA 密钥长度非法: {keySize}，须为 512~16384 且 8 的倍数", nameof(keySize))
            };
        }

        /// <summary>生成指定长度的 RSA 密钥对，返回 (公钥PEM, 私钥PEM)。</summary>
        public static (string PublicKey, string PrivateKey) GenerateKeyPair(int keySize)
        {
            CheckKeySize(keySize);

            using RSA rsa = RSA.Create(keySize);
            return (rsa.ExportRSAPublicKeyPem(), rsa.ExportRSAPrivateKeyPem());
        }

        /// <summary>校验 PEM 是否能作为有效的 RSA 密钥导入。</summary>
        public static bool IsValidPem(string pem)
        {
            try
            {
                rsa.ImportFromPem(pem);
                return true;
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException or FormatException)
            {
                return false;
            }
        }
    }
}
