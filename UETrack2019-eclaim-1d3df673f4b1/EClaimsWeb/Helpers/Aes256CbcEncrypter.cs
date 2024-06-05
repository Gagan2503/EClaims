using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EClaimsWeb.Helpers
{
    public static class Aes256CbcEncrypter
    {
        private static string EncryptionKey = "UEMSV2SPBNI99212";
        public static string Encrypt(string input)
        {
            // Get the bytes of the string
            byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(input);
            byte[] encryptionBytes = Encoding.UTF8.GetBytes(EncryptionKey);

            // Hash the encryptionKey with SHA256
            encryptionBytes = SHA256.Create().ComputeHash(encryptionBytes);
            byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, encryptionBytes);
            string result = Convert.ToBase64String(bytesEncrypted);

            return result;
        }

        public static string Decrypt(string input)
        {
            // Get the bytes of the string
            byte[] bytesToBeDecrypted = Convert.FromBase64String(input);
            byte[] encryptionBytes = Encoding.UTF8.GetBytes(EncryptionKey);

            // Hash the encryption with SHA256
            encryptionBytes = SHA256.Create().ComputeHash(encryptionBytes);
            byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, encryptionBytes);
            string result = Encoding.UTF8.GetString(bytesDecrypted);

            return result;
        }

        private static byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] encryptionBytes)
        {
            byte[] encryptedBytes = null;

            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(encryptionBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                        cs.Close();
                    }
                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }

        private static byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] encryptionBytes)
        {
            byte[] decryptedBytes = null;

            // The salt bytes must be at least 8 bytes.
            byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(encryptionBytes, saltBytes, 1000);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                        cs.Close();
                    }
                    decryptedBytes = ms.ToArray();
                }
            }

            return decryptedBytes;
        }

        public static void EncryptFile(byte[] bytesToBeEncrypted, string filePath)
        {

            byte[] encryptionBytes = Encoding.UTF8.GetBytes(EncryptionKey);

            // Hash the encryptionKey with SHA256
            encryptionBytes = SHA256.Create().ComputeHash(encryptionBytes);
            byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, encryptionBytes);

            File.WriteAllBytes(filePath, bytesEncrypted);
        }

        public static byte[] DecryptFile(byte[] bytesToBeDecrypted)
        {
            byte[] encryptionBytes = Encoding.UTF8.GetBytes(EncryptionKey);
            // Hash the encryptionKey with SHA256
            encryptionBytes = SHA256.Create().ComputeHash(encryptionBytes);
            byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, encryptionBytes);

            return bytesDecrypted;
        }
    }
}
