using Microsoft.EntityFrameworkCore;
using PEngineV.Data;

namespace PEngineV.Services;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await db.Database.EnsureCreatedAsync();

        if (!await db.Users.AnyAsync(u => u.Username == "admin"))
        {
            var (hash, salt) = passwordHasher.HashPassword("admin");
            db.Users.Add(new User
            {
                Username = "admin",
                Nickname = "Administrator",
                Email = "admin@example.com",
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }
}
