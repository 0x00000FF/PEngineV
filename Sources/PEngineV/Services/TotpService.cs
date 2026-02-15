using System.Security.Cryptography;

namespace PEngineV.Services;

public interface ITotpService
{
    string GenerateSecret();
    string GenerateQrCodeUri(string secret, string username, string issuer = "PEngineV");
    bool ValidateCode(string secret, string code);
}

public class TotpService : ITotpService
{
    private const int SecretSize = 20;
    private const int CodeDigits = 6;
    private const int TimeStep = 30;
    private const int Window = 1;

    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(SecretSize);
        return Base32Encode(bytes);
    }

    public string GenerateQrCodeUri(string secret, string username, string issuer = "PEngineV")
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedUsername = Uri.EscapeDataString(username);
        return $"otpauth://totp/{encodedIssuer}:{encodedUsername}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits={CodeDigits}&period={TimeStep}";
    }

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != CodeDigits)
            return false;

        ArgumentNullException.ThrowIfNull(secret);
        var secretBytes = Base32Decode(secret);
        var currentTimeStep = GetCurrentTimeStep();

        for (var i = -Window; i <= Window; i++)
        {
            var expectedCode = ComputeTotp(secretBytes, currentTimeStep + i);
            if (CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(code),
                System.Text.Encoding.UTF8.GetBytes(expectedCode)))
            {
                return true;
            }
        }

        return false;
    }

    private static long GetCurrentTimeStep()
    {
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return unixTime / TimeStep;
    }

    private static string ComputeTotp(byte[] secret, long timeStep)
    {
        var timeBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timeBytes);

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(timeBytes);

        var offset = hash[^1] & 0x0F;
        var binaryCode =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        var otp = binaryCode % (int)Math.Pow(10, CodeDigits);
        return otp.ToString().PadLeft(CodeDigits, '0');
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new System.Text.StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                result.Append(alphabet[(buffer >> bitsLeft) & 0x1F]);
            }
        }

        if (bitsLeft > 0)
        {
            result.Append(alphabet[(buffer << (5 - bitsLeft)) & 0x1F]);
        }

        return result.ToString();
    }

    private static byte[] Base32Decode(string encoded)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new List<byte>();
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var c in encoded.ToUpperInvariant())
        {
            var val = alphabet.IndexOf(c);
            if (val < 0) continue;
            buffer = (buffer << 5) | val;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                output.Add((byte)(buffer >> bitsLeft));
            }
        }

        return output.ToArray();
    }
}
