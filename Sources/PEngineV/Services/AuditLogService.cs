using Microsoft.EntityFrameworkCore;
using PEngineV.Data;

namespace PEngineV.Services;

public interface IAuditLogService
{
    Task LogAsync(int userId, string actionType, string? ipAddress, string? userAgent, string? details);
    Task<IReadOnlyList<AuditLog>> GetLogsForUserAsync(int userId, int page = 1, int pageSize = 20);
}

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(int userId, string actionType, string? ipAddress, string? userAgent, string? details)
    {
        var entry = new AuditLog
        {
            UserId = userId,
            ActionType = actionType,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AuditLog>> GetLogsForUserAsync(int userId, int page = 1, int pageSize = 20)
    {
        return await _db.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }
}
