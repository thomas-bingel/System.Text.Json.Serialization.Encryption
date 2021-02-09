using System.IO;
using System.Security.Cryptography;

namespace System.Text.Json.Serialization.Encryption
{
    internal class AesStringEncryption
    {

        /// <summary>
        /// Encrypt a string
        /// </summary>
        /// <param name="plainTextString">the string to encrypt</param>
        /// <param name="encryptionKey">the secret used to encrypt</param>
        /// <returns>the encrypted string</returns>
        public static string Encrypt(string plainTextString, string encryptionKey)
        {
            if (string.IsNullOrWhiteSpace(plainTextString))
            {
                throw new ArgumentException($"'{nameof(plainTextString)}' must contain a value.", nameof(plainTextString));
            }

            if (string.IsNullOrWhiteSpace(encryptionKey))
            {
                throw new ArgumentException($"'{nameof(encryptionKey)}' must contain a value.", nameof(encryptionKey));
            }

            var buffer = Encoding.UTF8.GetBytes(plainTextString);

            using (var sha = new SHA256Managed())
            using (var inputStream = new MemoryStream(buffer, false))
            using (var outputStream = new MemoryStream())
            using (var aes = new AesManaged())
            {
                aes.Key = sha.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
                var iv = aes.IV; 

                outputStream.Write(iv, 0, iv.Length);
                outputStream.Flush();

                var encryptor = aes.CreateEncryptor(aes.Key, iv);
                using (var cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write))
                {
                    inputStream.CopyTo(cryptoStream);
                }

                return Convert.ToBase64String(outputStream.ToArray());
            }
        }

        /// <summary>
        /// Decrypt a string
        /// </summary>
        /// <param name="encryptedString">the encrypted string</param>
        /// <param name="encryptionKey">the secret used to encrypt</param>
        /// <returns>decrypted string</returns>
        public static string Decrypt(string encryptedString, string encryptionKey)
        {
            if (string.IsNullOrWhiteSpace(encryptedString))
            {
                throw new ArgumentException($"'{nameof(encryptedString)}' must contain a value.", nameof(encryptedString));
            }

            if (string.IsNullOrWhiteSpace(encryptionKey))
            {
                throw new ArgumentException($"'{nameof(encryptionKey)}' must contain a value.", nameof(encryptionKey));
            }

            try
            {
                var buffer = Convert.FromBase64String(encryptedString);

                using (var sha = new SHA256Managed())
                using (var inputStream = new MemoryStream(buffer, false))
                using (var outputStream = new MemoryStream())
                using (var aes = new AesManaged())
                {
                    aes.Key = sha.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
                    var iv = new byte[16];
                    var bytesRead = inputStream.Read(iv, 0, 16);
                    if (bytesRead < 16)
                    {
                        throw new CryptographicException("Invalid or missing IV.");
                    }

                    var decryptor = aes.CreateDecryptor(aes.Key, iv);
                    using (var cryptoStream = new CryptoStream(inputStream, decryptor, CryptoStreamMode.Read))
                    {
                        cryptoStream.CopyTo(outputStream);
                    }

                    var decryptedValue = Encoding.UTF8.GetString(outputStream.ToArray());
                    return decryptedValue;
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
