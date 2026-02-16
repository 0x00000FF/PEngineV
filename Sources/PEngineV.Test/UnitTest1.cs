using PEngineV.Services;

namespace PEngineV.Test;

public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Test]
    public void HashPassword_Returns_NonEmpty_HashAndSalt()
    {
        var (hash, salt) = _hasher.HashPassword("testpassword");

        Assert.That(hash, Is.Not.Null.And.Not.Empty);
        Assert.That(salt, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void HashPassword_Returns_Base64_Encoded_Values()
    {
        var (hash, salt) = _hasher.HashPassword("testpassword");

        Assert.DoesNotThrow(() => Convert.FromBase64String(hash));
        Assert.DoesNotThrow(() => Convert.FromBase64String(salt));
    }

    [Test]
    public void HashPassword_Produces_Different_Salts()
    {
        var (_, salt1) = _hasher.HashPassword("testpassword");
        var (_, salt2) = _hasher.HashPassword("testpassword");

        Assert.That(salt1, Is.Not.EqualTo(salt2));
    }

    [Test]
    public void VerifyPassword_Returns_True_For_Correct_Password()
    {
        var (hash, salt) = _hasher.HashPassword("correctpassword");

        var result = _hasher.VerifyPassword("correctpassword", hash, salt);

        Assert.That(result, Is.True);
    }

    [Test]
    public void VerifyPassword_Returns_False_For_Wrong_Password()
    {
        var (hash, salt) = _hasher.HashPassword("correctpassword");

        var result = _hasher.VerifyPassword("wrongpassword", hash, salt);

        Assert.That(result, Is.False);
    }
}

public class TotpServiceTests
{
    private readonly TotpService _totp = new();

    [Test]
    public void GenerateSecret_Returns_NonEmpty_String()
    {
        var secret = _totp.GenerateSecret();

        Assert.That(secret, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GenerateSecret_Returns_Base32_String()
    {
        var secret = _totp.GenerateSecret();

        Assert.That(secret, Does.Match("^[A-Z2-7]+$"));
    }

    [Test]
    public void GenerateQrCodeUri_Returns_OtpAuth_Uri()
    {
        var secret = _totp.GenerateSecret();

        var uri = _totp.GenerateQrCodeUri(secret, "testuser");

        Assert.That(uri, Does.StartWith("otpauth://totp/PEngineV:testuser"));
        Assert.That(uri, Does.Contain($"secret={secret}"));
        Assert.That(uri, Does.Contain("issuer=PEngineV"));
    }

    [Test]
    public void ValidateCode_Returns_False_For_Empty_Code()
    {
        var secret = _totp.GenerateSecret();

        Assert.That(_totp.ValidateCode(secret, ""), Is.False);
        Assert.That(_totp.ValidateCode(secret, "123"), Is.False);
    }

    [Test]
    public void ValidateCode_Throws_For_Null_Secret()
    {
        Assert.Throws<ArgumentNullException>(() => _totp.ValidateCode(null!, "123456"));
    }
}
