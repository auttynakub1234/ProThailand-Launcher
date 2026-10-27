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

        /// <summary>
        /// Pure PBKDF2-HMAC-SHA256 implementation compatible with all .NET versions (4.0 - 4.8+).
        /// Avoids missing constructor issues on older Windows .NET Frameworks.
        /// </summary>
        public static byte[] DeriveKey(byte[] salt)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(MasterPassword);
            return Pbkdf2HmacSha256(passwordBytes, salt, Iterations, 32);
        }

        public static byte[] Pbkdf2HmacSha256(byte[] password, byte[] salt, int iterations, int outputBytes)
        {
            using (var hmac = new HMACSHA256(password))
            {
                byte[] result = new byte[outputBytes];
                int numBlocks = (outputBytes + 31) / 32;
                int offset = 0;

                for (int i = 1; i <= numBlocks; i++)
                {
                    byte[] blockIndex = new byte[4];
                    blockIndex[0] = (byte)(i >> 24);
                    blockIndex[1] = (byte)(i >> 16);
                    blockIndex[2] = (byte)(i >> 8);
                    blockIndex[3] = (byte)i;

                    byte[] saltBlock = new byte[salt.Length + 4];
                    Buffer.BlockCopy(salt, 0, saltBlock, 0, salt.Length);
                    Buffer.BlockCopy(blockIndex, 0, saltBlock, salt.Length, 4);

                    byte[] u = hmac.ComputeHash(saltBlock);
                    byte[] f = (byte[])u.Clone();

                    for (int j = 1; j < iterations; j++)
                    {
                        u = hmac.ComputeHash(u);
                        for (int k = 0; k < 32; k++)
                        {
                            f[k] ^= u[k];
                        }
                    }

                    int toCopy = Math.Min(32, outputBytes - offset);
                    Buffer.BlockCopy(f, 0, result, offset, toCopy);
                    offset += toCopy;
                }

                return result;
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
