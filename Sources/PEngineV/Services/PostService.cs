using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using PEngineV.Data;

namespace PEngineV.Services;

public interface IPostService
{
    // Post CRUD
    Task<Post> CreatePostAsync(int authorId, string title, string content, int? categoryId,
        PostVisibility visibility, string? password, DateTime? publishAt, IEnumerable<string> tagNames,
        IEnumerable<int>? groupIds);
    Task<Post?> GetPostByIdAsync(int id);
    Task<Post?> GetPostForReadAsync(int id);
    Task UpdatePostAsync(int id, string title, string content, int? categoryId,
        PostVisibility visibility, string? password, DateTime? publishAt, IEnumerable<string> tagNames,
        IEnumerable<int>? groupIds);
    Task DeletePostAsync(int id);
    Task<IReadOnlyList<Post>> GetPublishedPostsAsync(int? userId);
    Task<bool> CanUserViewPostAsync(int postId, int? userId);
    Task<string?> DecryptPostContentAsync(int postId, string password);

    // Category
    Task<IReadOnlyList<Category>> GetCategoriesAsync();
    Task<Category> GetOrCreateCategoryAsync(string name);

    // Tags
    Task<IReadOnlyList<Tag>> GetTagsAsync();

    // Attachments
    Task<Attachment> AddAttachmentAsync(int postId, string fileName, string storedPath,
        string contentType, long fileSize, string sha256Hash);
    Task<IReadOnlyList<Attachment>> GetAttachmentsAsync(int postId);
    Task DeleteAttachmentAsync(int attachmentId);

    // Comments
    Task<Comment> CreateCommentAsync(int postId, int? authorId, int? parentCommentId,
        string? guestName, string? guestEmail, string? password, string content, bool isPrivate);
    Task<IReadOnlyList<Comment>> GetCommentsAsync(int postId);
    Task UpdateCommentAsync(int commentId, string content);
    Task DeleteCommentAsync(int commentId);
    Task<bool> VerifyCommentPasswordAsync(int commentId, string password);

    // Thumbnail
    Task SetThumbnailAsync(int postId, string? thumbnailUrl);

    // Search
    Task<(IReadOnlyList<Post> Posts, IReadOnlyList<Comment> Comments)> SearchAsync(string query, int? userId);

    // Profile
    Task<IReadOnlyList<Post>> GetPublicPostsByAuthorAsync(int authorId);
    Task<IReadOnlyList<Comment>> GetCommentsByAuthorAsync(int authorId);
}

public class PostService : IPostService
{
    private const int SearchResultLimit = 50;

    private readonly AppDbContext _db;
    private readonly IEncryptionService _encryption;
    private readonly IPasswordHasher _passwordHasher;

    public PostService(AppDbContext db, IEncryptionService encryption, IPasswordHasher passwordHasher)
    {
        _db = db;
        _encryption = encryption;
        _passwordHasher = passwordHasher;
    }

    public async Task<Post> CreatePostAsync(int authorId, string title, string content, int? categoryId,
        PostVisibility visibility, string? password, DateTime? publishAt, IEnumerable<string> tagNames,
        IEnumerable<int>? groupIds)
    {
        var post = new Post
        {
            AuthorId = authorId,
            Title = title,
            Content = content,
            CategoryId = categoryId,
            Visibility = visibility,
            PublishAt = publishAt,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(password))
        {
            post.IsProtected = true;
            var result = _encryption.Encrypt(content, password);
            post.EncryptedContent = result.EncryptedData;
            post.PasswordSalt = result.Salt;
            post.EncryptionIV = result.IV;
            post.EncryptionTag = result.Tag;
            post.Content = ""; // Don't store plaintext when protected
        }

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        // Handle tags
        await SyncTagsAsync(post.Id, tagNames);

        // Handle groups for visibility
        if (visibility == PostVisibility.Groups && groupIds != null)
        {
            foreach (var groupId in groupIds)
            {
                _db.PostGroups.Add(new PostGroup { PostId = post.Id, GroupId = groupId });
            }
            await _db.SaveChangesAsync();
        }

        return post;
    }

    public async Task<Post?> GetPostByIdAsync(int id)
    {
        return await _db.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Post?> GetPostForReadAsync(int id)
    {
        return await _db.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Include(p => p.Attachments)
            .Include(p => p.Comments.Where(c => c.ParentCommentId == null))
                .ThenInclude(c => c.Author)
            .Include(p => p.Comments.Where(c => c.ParentCommentId == null))
                .ThenInclude(c => c.Replies).ThenInclude(r => r.Author)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task UpdatePostAsync(int id, string title, string content, int? categoryId,
        PostVisibility visibility, string? password, DateTime? publishAt, IEnumerable<string> tagNames,
        IEnumerable<int>? groupIds)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post is null) return;

        post.Title = title;
        post.CategoryId = categoryId;
        post.Visibility = visibility;
        post.PublishAt = publishAt;
        post.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(password))
        {
            post.IsProtected = true;
            var result = _encryption.Encrypt(content, password);
            post.EncryptedContent = result.EncryptedData;
            post.PasswordSalt = result.Salt;
            post.EncryptionIV = result.IV;
            post.EncryptionTag = result.Tag;
            post.Content = "";
        }
        else
        {
            post.IsProtected = false;
            post.Content = content;
            post.EncryptedContent = null;
            post.PasswordSalt = null;
            post.EncryptionIV = null;
            post.EncryptionTag = null;
        }

        await _db.SaveChangesAsync();
        await SyncTagsAsync(post.Id, tagNames);

        // Sync groups
        var existingGroups = await _db.PostGroups.Where(pg => pg.PostId == id).ToListAsync();
        _db.PostGroups.RemoveRange(existingGroups);
        if (visibility == PostVisibility.Groups && groupIds != null)
        {
            foreach (var groupId in groupIds)
            {
                _db.PostGroups.Add(new PostGroup { PostId = id, GroupId = groupId });
            }
        }
        await _db.SaveChangesAsync();
    }

    public async Task DeletePostAsync(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post is null) return;
        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Post>> GetPublishedPostsAsync(int? userId)
    {
        var now = DateTime.UtcNow;
        var query = _db.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
            .Where(p => p.PublishAt == null || p.PublishAt <= now);

        if (userId == null)
        {
            // Anonymous: only public posts
            query = query.Where(p => p.Visibility == PostVisibility.Public);
        }
        else
        {
            // Logged-in user: public + internal + own private + groups they belong to
            var userGroupIds = await _db.UserGroups
                .Where(ug => ug.UserId == userId.Value)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            query = query.Where(p =>
                p.Visibility == PostVisibility.Public ||
                p.Visibility == PostVisibility.Internal ||
                (p.Visibility == PostVisibility.Private && p.AuthorId == userId.Value) ||
                (p.Visibility == PostVisibility.Groups &&
                    p.PostGroups.Any(pg => userGroupIds.Contains(pg.GroupId))));
        }

        return await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
    }

    public async Task<bool> CanUserViewPostAsync(int postId, int? userId)
    {
        var post = await _db.Posts.Include(p => p.PostGroups).FirstOrDefaultAsync(p => p.Id == postId);
        if (post is null) return false;

        // Check if published
        if (post.PublishAt != null && post.PublishAt > DateTime.UtcNow)
        {
            return userId != null && post.AuthorId == userId.Value;
        }

        return post.Visibility switch
        {
            PostVisibility.Public => true,
            PostVisibility.Internal => userId != null,
            PostVisibility.Private => userId != null && post.AuthorId == userId.Value,
            PostVisibility.Groups => userId != null && (post.AuthorId == userId.Value ||
                await _db.UserGroups.AnyAsync(ug =>
                    ug.UserId == userId.Value &&
                    post.PostGroups.Select(pg => pg.GroupId).Contains(ug.GroupId))),
            _ => false
        };
    }

    public async Task<string?> DecryptPostContentAsync(int postId, string password)
    {
        var post = await _db.Posts.FindAsync(postId);
        if (post?.EncryptedContent is null || post.PasswordSalt is null ||
            post.EncryptionIV is null || post.EncryptionTag is null)
            return null;

        try
        {
            return _encryption.Decrypt(post.EncryptedContent, password, post.PasswordSalt,
                post.EncryptionIV, post.EncryptionTag);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    // Categories
    public async Task<IReadOnlyList<Category>> GetCategoriesAsync()
    {
        return await _db.Categories.OrderBy(c => c.Name).ToListAsync();
    }

    public async Task<Category> GetOrCreateCategoryAsync(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var slug = Regex.Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Name == name);
        if (category is not null) return category;

        category = new Category { Name = name, Slug = slug };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    // Tags
    public async Task<IReadOnlyList<Tag>> GetTagsAsync()
    {
        return await _db.Tags.OrderBy(t => t.Name).ToListAsync();
    }

    private async Task SyncTagsAsync(int postId, IEnumerable<string> tagNames)
    {
        var existing = await _db.PostTags.Where(pt => pt.PostId == postId).ToListAsync();
        _db.PostTags.RemoveRange(existing);
        await _db.SaveChangesAsync();

        var uniqueNames = tagNames
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n.Trim())
            .Distinct()
            .ToList();

        if (uniqueNames.Count == 0) return;

        var existingTags = await _db.Tags
            .Where(t => uniqueNames.Contains(t.Name))
            .ToDictionaryAsync(t => t.Name);

        var newTags = uniqueNames
            .Where(n => !existingTags.ContainsKey(n))
            .Select(n => new Tag { Name = n })
            .ToList();

        if (newTags.Count > 0)
        {
            _db.Tags.AddRange(newTags);
            await _db.SaveChangesAsync();
            foreach (var tag in newTags)
                existingTags[tag.Name] = tag;
        }

        foreach (var name in uniqueNames)
        {
            _db.PostTags.Add(new PostTag { PostId = postId, TagId = existingTags[name].Id });
        }
        await _db.SaveChangesAsync();
    }

    // Attachments
    public async Task<Attachment> AddAttachmentAsync(int postId, string fileName, string storedPath,
        string contentType, long fileSize, string sha256Hash)
    {
        var attachment = new Attachment
        {
            PostId = postId,
            FileName = fileName,
            StoredPath = storedPath,
            ContentType = contentType,
            FileSize = fileSize,
            Sha256Hash = sha256Hash,
            UploadedAt = DateTime.UtcNow
        };
        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync();
        return attachment;
    }

    public async Task<IReadOnlyList<Attachment>> GetAttachmentsAsync(int postId)
    {
        return await _db.Attachments
            .Where(a => a.PostId == postId)
            .OrderBy(a => a.UploadedAt)
            .ToListAsync();
    }

    public async Task DeleteAttachmentAsync(int attachmentId)
    {
        var attachment = await _db.Attachments.FindAsync(attachmentId);
        if (attachment is null) return;
        _db.Attachments.Remove(attachment);
        await _db.SaveChangesAsync();
    }

    // Thumbnail
    public async Task SetThumbnailAsync(int postId, string? thumbnailUrl)
    {
        var post = await _db.Posts.FindAsync(postId);
        if (post is null) return;
        post.ThumbnailUrl = thumbnailUrl;
        await _db.SaveChangesAsync();
    }

    // Comments
    public async Task<Comment> CreateCommentAsync(int postId, int? authorId, int? parentCommentId,
        string? guestName, string? guestEmail, string? password, string content, bool isPrivate)
    {
        var comment = new Comment
        {
            PostId = postId,
            AuthorId = authorId,
            ParentCommentId = parentCommentId,
            GuestName = guestName,
            GuestEmail = guestEmail,
            Content = content,
            IsPrivate = isPrivate,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(password) && authorId == null)
        {
            var (hash, salt) = _passwordHasher.HashPassword(password);
            comment.PasswordHash = hash;
            comment.PasswordSalt = salt;
        }

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();
        return comment;
    }

    public async Task<IReadOnlyList<Comment>> GetCommentsAsync(int postId)
    {
        return await _db.Comments
            .Include(c => c.Author)
            .Include(c => c.Replies).ThenInclude(r => r.Author)
            .Where(c => c.PostId == postId && c.ParentCommentId == null)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateCommentAsync(int commentId, string content)
    {
        var comment = await _db.Comments.FindAsync(commentId);
        if (comment is null) return;
        comment.Content = content;
        comment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteCommentAsync(int commentId)
    {
        var comment = await _db.Comments.FindAsync(commentId);
        if (comment is null) return;
        _db.Comments.Remove(comment);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> VerifyCommentPasswordAsync(int commentId, string password)
    {
        var comment = await _db.Comments.FindAsync(commentId);
        if (comment?.PasswordHash is null || comment.PasswordSalt is null) return false;
        return _passwordHasher.VerifyPassword(password, comment.PasswordHash, comment.PasswordSalt);
    }

    // Search - full text search across posts and comments
    public async Task<(IReadOnlyList<Post> Posts, IReadOnlyList<Comment> Comments)> SearchAsync(string query, int? userId)
    {
        ArgumentNullException.ThrowIfNull(query);
        var normalizedQuery = query.ToLowerInvariant();
        var now = DateTime.UtcNow;

        var postsQuery = _db.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Where(p => !p.IsProtected)
            .Where(p => p.PublishAt == null || p.PublishAt <= now)
            .Where(p => p.Title.ToLower().Contains(normalizedQuery) ||
                        p.Content.ToLower().Contains(normalizedQuery));

        if (userId == null)
        {
            postsQuery = postsQuery.Where(p => p.Visibility == PostVisibility.Public);
        }
        else
        {
            var userGroupIds = await _db.UserGroups
                .Where(ug => ug.UserId == userId.Value)
                .Select(ug => ug.GroupId)
                .ToListAsync();

            postsQuery = postsQuery.Where(p =>
                p.Visibility == PostVisibility.Public ||
                p.Visibility == PostVisibility.Internal ||
                (p.Visibility == PostVisibility.Private && p.AuthorId == userId.Value) ||
                (p.Visibility == PostVisibility.Groups &&
                    p.PostGroups.Any(pg => userGroupIds.Contains(pg.GroupId))));
        }

        var posts = await postsQuery.OrderByDescending(p => p.CreatedAt).Take(SearchResultLimit).ToListAsync();

        var commentsQuery = _db.Comments
            .Include(c => c.Author)
            .Include(c => c.Post)
            .Where(c => !c.IsPrivate)
            .Where(c => c.Post.PublishAt == null || c.Post.PublishAt <= now)
            .Where(c => c.Content.ToLower().Contains(normalizedQuery));

        if (userId == null)
        {
            commentsQuery = commentsQuery.Where(c => c.Post.Visibility == PostVisibility.Public);
        }

        var comments = await commentsQuery.OrderByDescending(c => c.CreatedAt).Take(SearchResultLimit).ToListAsync();

        return (posts, comments);
    }

    // Profile
    public async Task<IReadOnlyList<Post>> GetPublicPostsByAuthorAsync(int authorId)
    {
        var now = DateTime.UtcNow;
        return await _db.Posts
            .Include(p => p.Author)
            .Include(p => p.Category)
            .Where(p => p.AuthorId == authorId)
            .Where(p => p.Visibility == PostVisibility.Public)
            .Where(p => p.PublishAt == null || p.PublishAt <= now)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Comment>> GetCommentsByAuthorAsync(int authorId)
    {
        return await _db.Comments
            .Include(c => c.Post)
            .Include(c => c.Author)
            .Where(c => c.AuthorId == authorId)
            .Where(c => !c.IsPrivate)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }
}
