using System.Security.Cryptography;
using System.Text;

namespace PEngineV.Services;

public record EncryptionResult(
    string EncryptedData,
    string Salt,
    string IV,
    string Tag);

public record ByteEncryptionResult(
    byte[] EncryptedData,
    string Salt,
    string IV,
    string Tag);

public interface IEncryptionService
{
    EncryptionResult Encrypt(string plaintext, string password);
    string Decrypt(string encryptedData, string password, string salt, string iv, string tag);
    ByteEncryptionResult EncryptBytes(byte[] data, string password, string salt);
    byte[] DecryptBytes(byte[] encryptedData, string password, string salt, string iv, string tag);
}

public class AesGcmEncryptionService : IEncryptionService
{
    private const int SaltSize = 32;
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int Iterations = 600000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            Algorithm,
            KeySize);
    }

    public EncryptionResult Encrypt(string plaintext, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = DeriveKey(password, salt);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        return new EncryptionResult(
            Convert.ToBase64String(ciphertext),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag));
    }

    public string Decrypt(string encryptedData, string password, string salt, string iv, string tag)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var key = DeriveKey(password, saltBytes);
        var nonce = Convert.FromBase64String(iv);
        var tagBytes = Convert.FromBase64String(tag);
        var ciphertext = Convert.FromBase64String(encryptedData);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tagBytes, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    public ByteEncryptionResult EncryptBytes(byte[] data, string password, string salt)
    {
        ArgumentNullException.ThrowIfNull(data);
        var saltBytes = Convert.FromBase64String(salt);
        var key = DeriveKey(password, saltBytes);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[data.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, data, ciphertext, tag);

        return new ByteEncryptionResult(
            ciphertext,
            salt,
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag));
    }

    public byte[] DecryptBytes(byte[] encryptedData, string password, string salt, string iv, string tag)
    {
        ArgumentNullException.ThrowIfNull(encryptedData);
        var saltBytes = Convert.FromBase64String(salt);
        var key = DeriveKey(password, saltBytes);
        var nonce = Convert.FromBase64String(iv);
        var tagBytes = Convert.FromBase64String(tag);
        var plaintext = new byte[encryptedData.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, encryptedData, tagBytes, plaintext);

        return plaintext;
    }
}
