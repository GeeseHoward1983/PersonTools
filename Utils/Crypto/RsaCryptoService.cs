using System.Security.Cryptography;

namespace PersonalTools.Utils.Crypto
{
    /// <summary>
    /// RSA 加解密/签名核心，与 UI 无关。把 RSA 等加密类型从控件中剥离，降低控件类耦合。
    /// 加密/解密使用 OAEP-SHA256，签名/验签使用 PKCS#1 + SHA256。isString=true 时明文按 UTF-8，否则按十六进制。
    /// </summary>
    internal static class RsaCryptoService
    {
        public static string Encrypt(string input, string publicKey, bool isString)
        {
            using RSA rsa = RSA.Create();

            try
            {
                byte[] inputBytes = ConvertUtils.InputBytes(input, !isString);
                rsa.ImportFromPem(publicKey);

                // OAEP-SHA256，避免 PKCS#1 v1.5 的填充预言攻击（会减小可加密明文上限，512 位密钥过小无法使用）
                byte[] encryptedBytes = rsa.Encrypt(inputBytes, RSAEncryptionPadding.OaepSHA256);
                return ConvertUtils.ToHexString(encryptedBytes);
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException or FormatException)
            {
                PersonalTools.Utils.AppLogger.Log($"RSA 加密失败: {ex}");
                throw new CryptographicException("导入公钥或加密失败，请检查公钥格式与输入数据。", ex);
            }
        }

        public static string Decrypt(string input, string privateKey, bool isString)
        {
            using RSA rsa = RSA.Create();

            try
            {
                byte[] encryptedBytes = ConvertUtils.HexStringToByteArray(input);
                rsa.ImportFromPem(privateKey);

                byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);
                // 与加密对称：String 模式按 UTF-8 文本输出，Hex 模式按十六进制输出，避免二进制明文被 UTF-8 解码损坏
                return ConvertUtils.OutputString(decryptedBytes, !isString);
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException or FormatException)
            {
                PersonalTools.Utils.AppLogger.Log($"RSA 解密失败: {ex}");
                throw new CryptographicException("导入私钥或解密失败，请检查私钥格式与密文。", ex);
            }
        }

        public static string Sign(string input, string privateKey, bool isString)
        {
            using RSA rsa = RSA.Create();

            try
            {
                byte[] inputBytes = ConvertUtils.InputBytes(input, !isString);
                rsa.ImportFromPem(privateKey);

                byte[] signatureBytes = rsa.SignData(inputBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return ConvertUtils.ToHexString(signatureBytes);
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException or FormatException)
            {
                PersonalTools.Utils.AppLogger.Log($"RSA 签名失败: {ex}");
                throw new CryptographicException("导入私钥或签名失败，请检查私钥格式与输入数据。", ex);
            }
        }

        public static bool Verify(string input, string signature, string publicKey, bool isString)
        {
            using RSA rsa = RSA.Create();

            try
            {
                byte[] inputBytes = ConvertUtils.InputBytes(input, !isString);
                byte[] signatureBytes = ConvertUtils.HexStringToByteArray(signature);
                rsa.ImportFromPem(publicKey);

                return rsa.VerifyData(inputBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException or FormatException)
            {
                // FormatException：input/signature 为非法十六进制时 HexStringToByteArray 会抛，按验签失败处理而非崩溃
                return false;
            }
        }

        /// <summary>生成指定长度的 RSA 密钥对，返回 (公钥PEM, 私钥PEM)。</summary>
        public static (string PublicKey, string PrivateKey) GenerateKeyPair(int keySize)
        {
            // 服务层参数兜底：拒绝明显非法的密钥长度并给出清晰错误(平台对过小/非对齐值仅抛较隐晦异常)。
            // 注：不强制 2048 下限，保留工具按需生成较短密钥用于测试/教学的能力。
            if (keySize < 512 || keySize % 8 != 0)
            {
                throw new ArgumentException($"RSA 密钥长度非法: {keySize}，须为 ≥512 且 8 的倍数", nameof(keySize));
            }

            using RSA rsa = RSA.Create(keySize);
            return (rsa.ExportRSAPublicKeyPem(), rsa.ExportRSAPrivateKeyPem());
        }

        /// <summary>校验 PEM 是否能作为有效的 RSA 密钥导入。</summary>
        public static bool IsValidPem(string pem)
        {
            try
            {
                using RSA rsa = RSA.Create();
                rsa.ImportFromPem(pem);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
