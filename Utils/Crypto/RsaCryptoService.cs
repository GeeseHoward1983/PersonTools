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

            byte[] inputBytes;
            byte[] signatureBytes;
            try
            {
                inputBytes = ConvertUtils.InputBytes(input, !isString);
                signatureBytes = ConvertUtils.HexStringToByteArray(signature);
                rsa.ImportFromPem(publicKey);
            }
            catch (Exception ex) when (ex is CryptographicException or ArgumentException or FormatException)
            {
                // 公钥 PEM 非法 / input 或签名为非法十六进制：属"输入格式错误"，与"签名不匹配"是两回事。
                // 记录日志（与 Sign/Encrypt/Decrypt 一致，不再无痕）并抛出清晰错误，避免被静默归为验签失败误导排查。
                PersonalTools.Utils.AppLogger.Log($"RSA 验签输入无效: {ex}");
                throw new CryptographicException("导入公钥或解析输入/签名失败，请检查公钥格式与输入数据。", ex);
            }

            // 仅此处的 false 才是真正的"签名与数据不匹配"（VerifyData 对格式合法但不匹配的签名返回 false，不抛异常）
            return rsa.VerifyData(inputBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }

        /// <summary>生成指定长度的 RSA 密钥对，返回 (公钥PEM, 私钥PEM)。</summary>
        public static (string PublicKey, string PrivateKey) GenerateKeyPair(int keySize)
        {
            // 服务层参数兜底：拒绝明显非法的密钥长度并给出清晰错误(平台对过小/非对齐值仅抛较隐晦异常)。
            // 上限 16384：更大的 keySize 会让 RSA.Create(keySize) 在调用线程长时间阻塞甚至卡死 UI，且无实际用途。
            // 注：不强制 2048 下限，保留工具按需生成较短密钥用于测试/教学的能力。
            if (keySize < 512 || keySize > 16384 || keySize % 8 != 0)
            {
                throw new ArgumentException($"RSA 密钥长度非法: {keySize}，须为 512~16384 且 8 的倍数", nameof(keySize));
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
