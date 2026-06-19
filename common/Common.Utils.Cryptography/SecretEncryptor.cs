using System.Security.Cryptography;
using System.Text;

namespace QuantInfra.Common.Utils.Cryptography;

public sealed class SecretEncryptor
{
    private const int KeySizeBytes = 32;   // AES-256
    private const int NonceSizeBytes = 12; // recommended for GCM
    private const int TagSizeBytes = 16;   // 128-bit auth tag

    private readonly byte[] _masterKey;

    public SecretEncryptor(byte[] masterKey)
    {
        if (masterKey.Length != KeySizeBytes)
            throw new ArgumentException("Master key must be 32 bytes.", nameof(masterKey));

        _masterKey = masterKey;
    }

    public string EncryptToBase64(string plaintext)
    {
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);

        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(_masterKey, TagSizeBytes);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var result = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];

        Buffer.BlockCopy(nonce, 0, result, 0, NonceSizeBytes);
        Buffer.BlockCopy(tag, 0, result, NonceSizeBytes, TagSizeBytes);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSizeBytes + TagSizeBytes, ciphertext.Length);

        return Convert.ToBase64String(result);
    }

    public string DecryptFromBase64(string encryptedBase64)
    {
        var data = Convert.FromBase64String(encryptedBase64);

        if (data.Length < NonceSizeBytes + TagSizeBytes)
            throw new CryptographicException("Invalid encrypted secret.");

        var nonce = data[..NonceSizeBytes];
        var tag = data[NonceSizeBytes..(NonceSizeBytes + TagSizeBytes)];
        var ciphertext = data[(NonceSizeBytes + TagSizeBytes)..];

        var plaintextBytes = new byte[ciphertext.Length];

        using var aes = new AesGcm(_masterKey, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintextBytes);

        return Encoding.UTF8.GetString(plaintextBytes);
    }
}