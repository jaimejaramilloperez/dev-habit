using System.Security.Cryptography;
using DevHabit.Api.Configurations;
using Microsoft.Extensions.Options;

namespace DevHabit.Api.Services;

public sealed class EncryptionService(IOptions<EncryptionOptions> options)
{
    private readonly byte[] _key = Convert.FromBase64String(options.Value.Key);
    private const int IvSize = 16;

    public string Encrypt(string plainText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainText);

        try
        {
            using Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = _key;

            using MemoryStream memoryStream = new();
            memoryStream.Write(aes.IV, 0, IvSize);

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            using CryptoStream cryptoStream = new(memoryStream, encryptor, CryptoStreamMode.Write);
            using (StreamWriter streamWriter = new(cryptoStream))
            {
                streamWriter.Write(plainText);
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    public string Decrypt(string cipherText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cipherText);

        try
        {
            byte[] cipherData = Convert.FromBase64String(cipherText);

            if (cipherData.Length < IvSize)
            {
                throw new InvalidOperationException("Invalid cipher text format");
            }

            byte[] iv = new byte[IvSize];
            byte[] encryptedData = new byte[cipherData.Length - IvSize];

            Buffer.BlockCopy(cipherData, 0, iv, 0, IvSize);
            Buffer.BlockCopy(cipherData, IvSize, encryptedData, 0, encryptedData.Length);

            using Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = _key;
            aes.IV = iv;

            using MemoryStream memoryStream = new(encryptedData);
            using ICryptoTransform decryptor = aes.CreateDecryptor();
            using CryptoStream cryptoStream = new(memoryStream, decryptor, CryptoStreamMode.Read);
            using StreamReader streamReader = new(cryptoStream);

            return streamReader.ReadToEnd();
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Decryption failed", ex);
        }
    }
}
