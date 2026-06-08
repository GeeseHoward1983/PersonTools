using System.Security.Cryptography;
using System.Text;

namespace PersonalTools
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
                byte[] inputBytes = isString ? Encoding.UTF8.GetBytes(input) : Utils.HexStringToByteArray(input);
                rsa.ImportFromPem(publicKey);

                // OAEP-SHA256，避免 PKCS#1 v1.5 的填充预言攻击（会减小可加密明文上限，512 位密钥过小无法使用）
                byte[] encryptedBytes = rsa.Encrypt(inputBytes, RSAEncryptionPadding.OaepSHA256);
                return Utils.ToHexString(encryptedBytes);
            }
            catch (ArgumentException ex)
            {
                throw new CryptographicException($"导入公钥或加密失败: {ex.Message}", ex);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException($"导入公钥或加密失败: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"导入公钥或加密失败: {ex.Message}", ex);
            }
        }

        public static string Decrypt(string input, string privateKey)
        {
            using RSA rsa = RSA.Create();

            try
            {
                byte[] encryptedBytes = Utils.HexStringToByteArray(input);
                rsa.ImportFromPem(privateKey);

                byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (ArgumentException ex)
            {
                throw new CryptographicException($"导入私钥或解密失败: {ex.Message}", ex);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException($"导入私钥或解密失败: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"导入私钥或解密失败: {ex.Message}", ex);
            }
        }

        public static string Sign(string input, string privateKey, bool isString)
        {
            using RSA rsa = RSA.Create();

            try
            {
                byte[] inputBytes = isString ? Encoding.UTF8.GetBytes(input) : Utils.HexStringToByteArray(input);
                rsa.ImportFromPem(privateKey);

                byte[] signatureBytes = rsa.SignData(inputBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                return Utils.ToHexString(signatureBytes);
            }
            catch (ArgumentException ex)
            {
                throw new CryptographicException($"导入私钥或签名失败: {ex.Message}", ex);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException($"导入私钥或签名失败: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"导入私钥或签名失败: {ex.Message}", ex);
            }
        }

        public static bool Verify(string input, string signature, string publicKey, bool isString)
        {
            using RSA rsa = RSA.Create();

            try
            {
                byte[] inputBytes = isString ? Encoding.UTF8.GetBytes(input) : Utils.HexStringToByteArray(input);
                byte[] signatureBytes = Utils.HexStringToByteArray(signature);
                rsa.ImportFromPem(publicKey);

                return rsa.VerifyData(inputBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
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
