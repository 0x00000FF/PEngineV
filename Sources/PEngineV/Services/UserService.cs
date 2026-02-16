using Microsoft.EntityFrameworkCore;

using PEngineV.Data;

namespace PEngineV.Services;

public interface IUserService
{
    Task<User?> AuthenticateAsync(string username, string password);
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByUsernameAsync(string username);
    Task<User> CreateUserAsync(string username, string email, string password, string nickname);
    Task UpdateProfileAsync(int userId, string nickname, string? bio, string? contactEmail, string? profileImageUrl);
    Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<string> EnableTwoFactorAsync(int userId);
    Task<bool> ConfirmTwoFactorAsync(int userId, string code);
    Task DisableTwoFactorAsync(int userId);
    Task<IReadOnlyList<UserPasskey>> GetPasskeysAsync(int userId);
    Task<UserPasskey> AddPasskeyAsync(int userId, string name, string credentialId, string publicKey);
    Task<UserPasskey> AddPasskeyAsync(int userId, string name, byte[] credentialId, byte[] publicKey, uint signCount, byte[] userHandle);
    Task<UserPasskey?> GetPasskeyByCredentialIdAsync(byte[] credentialId);
    Task<UserPasskey?> GetPasskeyByCredentialIdAsync(string credentialId);
    Task UpdatePasskeySignCountAsync(int passkeyId, uint signCount);
    Task<bool> RemovePasskeyAsync(int userId, int passkeyId);
}

public class UserService : IUserService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITotpService _totpService;

    public UserService(AppDbContext db, IPasswordHasher passwordHasher, ITotpService totpService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _totpService = totpService;
    }

    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user is null)
        {
            return null;
        }

        return _passwordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt)
            ? user
            : null;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _db.Users.FindAsync(id);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User> CreateUserAsync(string username, string email, string password, string nickname)
    {
        var (hash, salt) = _passwordHasher.HashPassword(password);
        var user = new User
        {
            Username = username,
            Email = email,
            Nickname = nickname,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task UpdateProfileAsync(int userId, string nickname, string? bio, string? contactEmail, string? profileImageUrl)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            return;
        }

        user.Nickname = nickname;
        user.Bio = bio;
        user.ContactEmail = contactEmail;
        user.ProfileImageUrl = profileImageUrl;
        await _db.SaveChangesAsync();
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            return false;
        }

        if (!_passwordHasher.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
        {
            return false;
        }

        var (hash, salt) = _passwordHasher.HashPassword(newPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<string> EnableTwoFactorAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            throw new InvalidOperationException("User not found");
        }

        var secret = _totpService.GenerateSecret();
        user.TwoFactorSecret = secret;
        await _db.SaveChangesAsync();
        return secret;
    }

    public async Task<bool> ConfirmTwoFactorAsync(int userId, string code)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user?.TwoFactorSecret is null)
        {
            return false;
        }

        if (!_totpService.ValidateCode(user.TwoFactorSecret, code))
        {
            return false;
        }

        user.TwoFactorEnabled = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task DisableTwoFactorAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            return;
        }

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<UserPasskey>> GetPasskeysAsync(int userId)
    {
        return await _db.UserPasskeys
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<UserPasskey> AddPasskeyAsync(int userId, string name, string credentialId, string publicKey)
    {
        var passkey = new UserPasskey
        {
            UserId = userId,
            Name = name,
            CredentialId = credentialId,
            PublicKey = publicKey,
            CreatedAt = DateTime.UtcNow
        };

        _db.UserPasskeys.Add(passkey);
        await _db.SaveChangesAsync();
        return passkey;
    }

    public async Task<UserPasskey> AddPasskeyAsync(int userId, string name, byte[] credentialId, byte[] publicKey, uint signCount, byte[] userHandle)
    {
        var passkey = new UserPasskey
        {
            UserId = userId,
            Name = name,
            CredentialId = Convert.ToBase64String(credentialId),
            PublicKey = Convert.ToBase64String(publicKey),
            SignCount = signCount,
            UserHandle = userHandle,
            CreatedAt = DateTime.UtcNow
        };

        _db.UserPasskeys.Add(passkey);
        await _db.SaveChangesAsync();
        return passkey;
    }

    public async Task<UserPasskey?> GetPasskeyByCredentialIdAsync(byte[] credentialId)
    {
        var credId = Convert.ToBase64String(credentialId);
        return await _db.UserPasskeys
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.CredentialId == credId);
    }

    public async Task<UserPasskey?> GetPasskeyByCredentialIdAsync(string credentialId)
    {
        return await _db.UserPasskeys
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.CredentialId == credentialId);
    }

    public async Task UpdatePasskeySignCountAsync(int passkeyId, uint signCount)
    {
        var passkey = await _db.UserPasskeys.FindAsync(passkeyId);
        if (passkey is null)
        {
            return;
        }

        passkey.SignCount = signCount;
        passkey.LastUsedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<bool> RemovePasskeyAsync(int userId, int passkeyId)
    {
        var passkey = await _db.UserPasskeys
            .FirstOrDefaultAsync(p => p.Id == passkeyId && p.UserId == userId);

        if (passkey is null)
        {
            return false;
        }

        _db.UserPasskeys.Remove(passkey);
        await _db.SaveChangesAsync();
        return true;
    }
}
