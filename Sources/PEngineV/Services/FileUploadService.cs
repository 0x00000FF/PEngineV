using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using PEngineV.Data;

namespace PEngineV.Services;

public interface IFileUploadService
{
    Task<UploadedFile> UploadProfileImageAsync(int userId, IFormFile file);
    Task<UploadedFile> UploadPostAttachmentAsync(int postId, int userId, IFormFile file);
    Task<UploadedFile> UploadPostThumbnailAsync(int postId, int userId, IFormFile file);
    Task<UploadedFile?> GetFileByGuidAsync(Guid fileGuid);
    Task<UploadedFile?> GetFileByIdAsync(int fileId);
    Task<bool> DeleteFileAsync(int fileId);
    Task<IEnumerable<UploadedFile>> GetFilesByPostIdAsync(int postId);
    Task<UploadedFile?> GetProfileImageByUserIdAsync(int userId);
    string GetPhysicalPath(UploadedFile file);
}

public class FileUploadService : IFileUploadService
{
    private readonly AppDbContext _context;
    private readonly string _storageRoot;

    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/gif", "image/webp"
    };

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".webp"
    };

    public FileUploadService(AppDbContext context)
    {
        _context = context;
        _storageRoot = Path.Combine(Directory.GetCurrentDirectory(), "storage");
        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<UploadedFile> UploadProfileImageAsync(int userId, IFormFile file)
    {
        ValidateImageFile(file);

        if (file.Length > 2 * 1024 * 1024)
            throw new InvalidOperationException("File size exceeds 2MB limit");

        var userGuid = await GetUserGuidAsync(userId);
        var fileGuid = Guid.NewGuid();
        var extension = Path.GetExtension(file.FileName);
        var relativePath = Path.Combine("profile", userGuid.ToString(), $"{fileGuid:N}{extension}");
        var physicalPath = Path.Combine(_storageRoot, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

        var hash = await SaveFileAndCalculateHashAsync(file, physicalPath);

        var uploadedFile = new UploadedFile
        {
            FileGuid = fileGuid,
            Category = FileCategory.ProfileImage,
            OriginalFileName = file.FileName,
            StoredPath = relativePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            Sha256Hash = hash,
            UploadedByUserId = userId,
            UploadedAt = DateTime.UtcNow
        };

        _context.UploadedFiles.Add(uploadedFile);
        await _context.SaveChangesAsync();

        return uploadedFile;
    }

    public async Task<UploadedFile> UploadPostAttachmentAsync(int postId, int userId, IFormFile file)
    {
        ValidateFile(file);

        if (file.Length > 10 * 1024 * 1024)
            throw new InvalidOperationException("File size exceeds 10MB limit");

        var fileGuid = Guid.NewGuid();
        var extension = Path.GetExtension(file.FileName);
        var relativePath = Path.Combine("post", postId.ToString(), $"{fileGuid:N}{extension}");
        var physicalPath = Path.Combine(_storageRoot, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

        var hash = await SaveFileAndCalculateHashAsync(file, physicalPath);

        var uploadedFile = new UploadedFile
        {
            FileGuid = fileGuid,
            Category = FileCategory.PostAttachment,
            OriginalFileName = file.FileName,
            StoredPath = relativePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            Sha256Hash = hash,
            UploadedByUserId = userId,
            RelatedPostId = postId,
            UploadedAt = DateTime.UtcNow
        };

        _context.UploadedFiles.Add(uploadedFile);
        await _context.SaveChangesAsync();

        return uploadedFile;
    }

    public async Task<UploadedFile> UploadPostThumbnailAsync(int postId, int userId, IFormFile file)
    {
        ValidateImageFile(file);

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("File size exceeds 5MB limit");

        var relativePath = Path.Combine("post", postId.ToString(), "thumbnail.png");
        var physicalPath = Path.Combine(_storageRoot, relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(physicalPath)!);

        if (File.Exists(physicalPath))
            File.Delete(physicalPath);

        var hash = await SaveFileAndCalculateHashAsync(file, physicalPath);

        var existing = await _context.UploadedFiles
            .FirstOrDefaultAsync(f => f.Category == FileCategory.PostThumbnail && f.RelatedPostId == postId);

        if (existing is not null)
        {
            existing.OriginalFileName = file.FileName;
            existing.ContentType = file.ContentType;
            existing.FileSize = file.Length;
            existing.Sha256Hash = hash;
            existing.UploadedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return existing;
        }

        var uploadedFile = new UploadedFile
        {
            FileGuid = Guid.NewGuid(),
            Category = FileCategory.PostThumbnail,
            OriginalFileName = file.FileName,
            StoredPath = relativePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            Sha256Hash = hash,
            UploadedByUserId = userId,
            RelatedPostId = postId,
            UploadedAt = DateTime.UtcNow
        };

        _context.UploadedFiles.Add(uploadedFile);
        await _context.SaveChangesAsync();

        return uploadedFile;
    }

    public async Task<UploadedFile?> GetFileByGuidAsync(Guid fileGuid) =>
        await _context.UploadedFiles
            .Include(f => f.UploadedBy)
            .Include(f => f.RelatedPost)
            .FirstOrDefaultAsync(f => f.FileGuid == fileGuid);

    public async Task<UploadedFile?> GetFileByIdAsync(int fileId) =>
        await _context.UploadedFiles
            .Include(f => f.UploadedBy)
            .Include(f => f.RelatedPost)
            .FirstOrDefaultAsync(f => f.Id == fileId);

    public async Task<bool> DeleteFileAsync(int fileId)
    {
        var file = await _context.UploadedFiles.FindAsync(fileId);
        if (file is null)
            return false;

        var physicalPath = GetPhysicalPath(file);
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        _context.UploadedFiles.Remove(file);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<UploadedFile>> GetFilesByPostIdAsync(int postId) =>
        await _context.UploadedFiles
            .Where(f => f.RelatedPostId == postId)
            .OrderBy(f => f.UploadedAt)
            .ToListAsync();

    public async Task<UploadedFile?> GetProfileImageByUserIdAsync(int userId) =>
        await _context.UploadedFiles
            .Where(f => f.Category == FileCategory.ProfileImage && f.UploadedByUserId == userId)
            .OrderByDescending(f => f.UploadedAt)
            .FirstOrDefaultAsync();

    public string GetPhysicalPath(UploadedFile file) =>
        Path.Combine(_storageRoot, file.StoredPath);

    private static void ValidateImageFile(IFormFile file)
    {
        var contentType = file.ContentType;
        var extension = Path.GetExtension(file.FileName);

        if (!AllowedImageTypes.Contains(contentType) || !AllowedImageExtensions.Contains(extension))
            throw new InvalidOperationException("Invalid image file type");
    }

    private static void ValidateFile(IFormFile file)
    {
        if (file.Length == 0)
            throw new InvalidOperationException("Empty file");

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
            throw new InvalidOperationException("File must have an extension");
    }

    private static async Task<string> SaveFileAndCalculateHashAsync(IFormFile file, string physicalPath)
    {
        using var fileStream = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        using var hashAlgorithm = SHA256.Create();
        using var cryptoStream = new CryptoStream(fileStream, hashAlgorithm, CryptoStreamMode.Write);

        await file.CopyToAsync(cryptoStream);
        await cryptoStream.FlushFinalBlockAsync();

        return Convert.ToHexStringLower(hashAlgorithm.Hash!);
    }

    private async Task<Guid> GetUserGuidAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
            throw new InvalidOperationException("User not found");

        return Guid.NewGuid();
    }
}
