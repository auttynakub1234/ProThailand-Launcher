using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace ProThailand
{
    public static class SecureCrypto
    {
        private const string MasterPassword = "config.sys.input=UTF8";
        private const int Iterations = 100000;

        public static byte[] DeriveKey(byte[] salt)
        {
            using (var rfc = new Rfc2898DeriveBytes(MasterPassword, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return rfc.GetBytes(32); // 256-bit AES key
            }
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            byte[] salt = new byte[16];
            byte[] iv = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
                rng.GetBytes(iv);
            }

            byte[] key = DeriveKey(salt);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] cipherBytes;

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                }
            }

            // Packed format: Salt[16] + IV[16] + Ciphertext[...]
            byte[] packed = new byte[salt.Length + iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(salt, 0, packed, 0, 16);
            Buffer.BlockCopy(iv, 0, packed, 16, 16);
            Buffer.BlockCopy(cipherBytes, 0, packed, 32, cipherBytes.Length);

            return Convert.ToBase64String(packed);
        }

        public static string Decrypt(string cipherTextBase64)
        {
            if (string.IsNullOrEmpty(cipherTextBase64))
                return string.Empty;

            byte[] raw = Convert.FromBase64String(cipherTextBase64);
            if (raw.Length < 48)
                throw new ArgumentException("Ciphertext payload is too short.");

            byte[] salt = new byte[16];
            byte[] iv = new byte[16];
            byte[] cipher = new byte[raw.Length - 32];

            Buffer.BlockCopy(raw, 0, salt, 0, 16);
            Buffer.BlockCopy(raw, 16, iv, 0, 16);
            Buffer.BlockCopy(raw, 32, cipher, 0, cipher.Length);

            byte[] key = DeriveKey(salt);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] decrypted = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                    return Encoding.UTF8.GetString(decrypted).TrimStart('\uFEFF');
                }
            }
        }
    }
}
