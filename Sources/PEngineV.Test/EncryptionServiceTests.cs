using System.Security.Cryptography;

using PEngineV.Services;

namespace PEngineV.Test;

[TestFixture]
public class EncryptionServiceTests
{
    private readonly AesGcmEncryptionService _service = new();

    [Test]
    public void Encrypt_Returns_NonEmpty_Result()
    {
        var result = _service.Encrypt("hello world", "password123");

        Assert.That(result.EncryptedData, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Salt, Is.Not.Null.And.Not.Empty);
        Assert.That(result.IV, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Tag, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void Encrypt_Result_Is_Base64()
    {
        var result = _service.Encrypt("test data", "secret");

        Assert.DoesNotThrow(() => Convert.FromBase64String(result.EncryptedData));
        Assert.DoesNotThrow(() => Convert.FromBase64String(result.Salt));
        Assert.DoesNotThrow(() => Convert.FromBase64String(result.IV));
        Assert.DoesNotThrow(() => Convert.FromBase64String(result.Tag));
    }

    [Test]
    public void Decrypt_Returns_Original_Plaintext()
    {
        const string plaintext = "The quick brown fox jumps over the lazy dog";
        const string password = "strongPassword!";

        var encrypted = _service.Encrypt(plaintext, password);
        var decrypted = _service.Decrypt(encrypted.EncryptedData, password,
            encrypted.Salt, encrypted.IV, encrypted.Tag);

        Assert.That(decrypted, Is.EqualTo(plaintext));
    }

    [Test]
    public void Decrypt_With_Wrong_Password_Throws()
    {
        var encrypted = _service.Encrypt("secret message", "correctPassword");

        Assert.Catch<CryptographicException>(() =>
            _service.Decrypt(encrypted.EncryptedData, "wrongPassword",
                encrypted.Salt, encrypted.IV, encrypted.Tag));
    }

    [Test]
    public void Encrypt_Produces_Different_Salt()
    {
        var result1 = _service.Encrypt("same text", "same password");
        var result2 = _service.Encrypt("same text", "same password");

        Assert.That(result1.Salt, Is.Not.EqualTo(result2.Salt));
    }

    [Test]
    public void EncryptBytes_And_DecryptBytes_Roundtrip()
    {
        var original = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        const string password = "bytePassword";
        var salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var encrypted = _service.EncryptBytes(original, password, salt);
        var decrypted = _service.DecryptBytes(encrypted.EncryptedData, password,
            encrypted.Salt, encrypted.IV, encrypted.Tag);

        Assert.That(decrypted, Is.EqualTo(original));
    }

    [Test]
    public void Decrypt_With_Tampered_Ciphertext_Throws()
    {
        var encrypted = _service.Encrypt("important data", "myPassword");
        var ciphertextBytes = Convert.FromBase64String(encrypted.EncryptedData);
        ciphertextBytes[0] ^= 0xFF;
        var tampered = Convert.ToBase64String(ciphertextBytes);

        Assert.Catch<CryptographicException>(() =>
            _service.Decrypt(tampered, "myPassword",
                encrypted.Salt, encrypted.IV, encrypted.Tag));
    }
}
