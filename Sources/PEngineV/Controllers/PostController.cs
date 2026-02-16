using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PEngineV.Data;
using PEngineV.Models;
using PEngineV.Services;

namespace PEngineV.Controllers;

[AutoValidateAntiforgeryToken]
public class PostController : Controller
{
    private readonly IPostService _postService;

    public PostController(IPostService postService)
    {
        _postService = postService;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null ? int.Parse(claim.Value) : null;
    }

    public async Task<IActionResult> Read(int id)
    {
        var post = await _postService.GetPostForReadAsync(id);
        if (post is null) return NotFound();

        var userId = GetCurrentUserId();
        if (!await _postService.CanUserViewPostAsync(id, userId))
            return RedirectToAction("Locked");

        if (post.IsProtected)
            return RedirectToAction("Protected", new { id });

        var isOwner = userId.HasValue && post.AuthorId == userId.Value;
        var comments = await _postService.GetCommentsAsync(id);
        var attachments = await _postService.GetAttachmentsAsync(id);

        var commentVMs = comments.Select(c => MapComment(c, userId, isOwner)).ToList();
        var attachmentVMs = attachments.Select(a =>
            new AttachmentItem(a.Id, a.FileName, a.FileSize, a.Sha256Hash)).ToList();

        var postVM = new PostViewModel(
            post.Id, post.Title, post.Content, post.Author.Nickname,
            post.PublishAt ?? post.CreatedAt, post.IsProtected,
            post.Category?.Name, post.ThumbnailUrl,
            post.PostTags.Select(pt => pt.Tag.Name),
            post.Visibility.ToString(),
            post.Author.Username);

        return View(new PostReadViewModel(postVM, commentVMs, userId.HasValue, isOwner, attachmentVMs));
    }

    [Authorize]
    public async Task<IActionResult> Write()
    {
        var categories = await _postService.GetCategoriesAsync();
        var model = new PostWriteViewModel(null, "", "", null, null, null, "Public", null,
            categories.Select(c => new CategoryOption(c.Id, c.Name)), null);
        return View(model);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Write(string title, string content, string? categoryName,
        string? tags, string visibility, string? password, DateTime? publishAt, List<IFormFile>? files)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        int? categoryId = null;
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            var category = await _postService.GetOrCreateCategoryAsync(categoryName.Trim());
            categoryId = category.Id;
        }

        var vis = Enum.Parse<PostVisibility>(visibility, true);
        var tagList = string.IsNullOrWhiteSpace(tags)
            ? Enumerable.Empty<string>()
            : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var post = await _postService.CreatePostAsync(userId.Value, title, content, categoryId,
            vis, password, publishAt, tagList, null);

        // Handle file uploads
        if (files is not null)
        {
            await HandleFileUploadsAsync(post.Id, files);
        }

        // Set thumbnail from first image attachment
        await SetThumbnailAsync(post.Id);

        return RedirectToAction("Read", new { id = post.Id });
    }

    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var post = await _postService.GetPostByIdAsync(id);
        if (post is null) return NotFound();

        var userId = GetCurrentUserId();
        if (!userId.HasValue || post.AuthorId != userId.Value) return Forbid();

        var categories = await _postService.GetCategoriesAsync();
        var attachments = await _postService.GetAttachmentsAsync(id);

        var model = new PostWriteViewModel(
            post.Id, post.Title, post.IsProtected ? "" : post.Content, null,
            post.Category?.Name,
            string.Join(", ", post.PostTags.Select(pt => pt.Tag.Name)),
            post.Visibility.ToString(),
            post.PublishAt,
            categories.Select(c => new CategoryOption(c.Id, c.Name)),
            attachments.Select(a => new AttachmentItem(a.Id, a.FileName, a.FileSize, a.Sha256Hash)));

        return View("Write", model);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, string title, string content, string? categoryName,
        string? tags, string visibility, string? password, DateTime? publishAt, List<IFormFile>? files)
    {
        var post = await _postService.GetPostByIdAsync(id);
        if (post is null) return NotFound();

        var userId = GetCurrentUserId();
        if (!userId.HasValue || post.AuthorId != userId.Value) return Forbid();

        int? categoryId = null;
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            var category = await _postService.GetOrCreateCategoryAsync(categoryName.Trim());
            categoryId = category.Id;
        }

        var vis = Enum.Parse<PostVisibility>(visibility, true);
        var tagList = string.IsNullOrWhiteSpace(tags)
            ? Enumerable.Empty<string>()
            : tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        await _postService.UpdatePostAsync(id, title, content, categoryId, vis, password, publishAt, tagList, null);

        if (files is not null)
        {
            await HandleFileUploadsAsync(id, files);
        }

        await SetThumbnailAsync(id);
        return RedirectToAction("Read", new { id });
    }

    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await _postService.GetPostByIdAsync(id);
        if (post is null) return NotFound();

        var userId = GetCurrentUserId();
        if (!userId.HasValue || post.AuthorId != userId.Value) return Forbid();

        return View(new PostDeleteViewModel(id, post.Title));
    }

    [Authorize]
    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var post = await _postService.GetPostByIdAsync(id);
        if (post is null) return NotFound();

        var userId = GetCurrentUserId();
        if (!userId.HasValue || post.AuthorId != userId.Value) return Forbid();

        // Delete attachment files
        var attachments = await _postService.GetAttachmentsAsync(id);
        foreach (var att in attachments)
        {
            DeleteFileIfSafe(att.StoredPath);
        }

        await _postService.DeletePostAsync(id);
        return RedirectToAction("Index", "Home");
    }

    public async Task<IActionResult> Protected(int id)
    {
        var post = await _postService.GetPostByIdAsync(id);
        if (post is null) return NotFound();
        return View(new PostProtectedViewModel(id, post.Title));
    }

    [HttpPost]
    public async Task<IActionResult> Protected(int id, string password)
    {
        var content = await _postService.DecryptPostContentAsync(id, password);
        if (content is null)
        {
            var post = await _postService.GetPostByIdAsync(id);
            ViewData["Error"] = true;
            return View(new PostProtectedViewModel(id, post?.Title ?? ""));
        }

        var fullPost = await _postService.GetPostForReadAsync(id);
        if (fullPost is null) return NotFound();

        var userId = GetCurrentUserId();
        var isOwner = userId.HasValue && fullPost.AuthorId == userId.Value;
        var comments = await _postService.GetCommentsAsync(id);
        var attachments = await _postService.GetAttachmentsAsync(id);

        var commentVMs = comments.Select(c => MapComment(c, userId, isOwner)).ToList();
        var attachmentVMs = attachments.Select(a =>
            new AttachmentItem(a.Id, a.FileName, a.FileSize, a.Sha256Hash)).ToList();

        var postVM = new PostViewModel(
            fullPost.Id, fullPost.Title, content, fullPost.Author.Nickname,
            fullPost.PublishAt ?? fullPost.CreatedAt, true,
            fullPost.Category?.Name, fullPost.ThumbnailUrl,
            fullPost.PostTags.Select(pt => pt.Tag.Name),
            fullPost.Visibility.ToString(),
            fullPost.Author.Username);

        return View("Read", new PostReadViewModel(postVM, commentVMs, userId.HasValue, isOwner, attachmentVMs));
    }

    public IActionResult Locked()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Comment(CommentWriteViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        var userId = GetCurrentUserId();

        await _postService.CreateCommentAsync(
            model.PostId, userId, model.ParentCommentId,
            userId.HasValue ? null : model.Name,
            userId.HasValue ? null : model.Email,
            model.Password, model.Content, model.IsPrivate);

        return RedirectToAction("Read", new { id = model.PostId });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> DeleteComment(int id, int postId)
    {
        var post = await _postService.GetPostByIdAsync(postId);
        if (post is null) return NotFound();

        var userId = GetCurrentUserId();
        if (!userId.HasValue || post.AuthorId != userId.Value) return Forbid();

        await _postService.DeleteCommentAsync(id);
        return RedirectToAction("Read", new { id = postId });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> DeleteAttachment(int id, int postId)
    {
        var post = await _postService.GetPostByIdAsync(postId);
        if (post is null) return NotFound();

        var userId = GetCurrentUserId();
        if (!userId.HasValue || post.AuthorId != userId.Value) return Forbid();

        var attachment = (await _postService.GetAttachmentsAsync(postId))
            .FirstOrDefault(a => a.Id == id);
        if (attachment is not null)
        {
            DeleteFileIfSafe(attachment.StoredPath);
            await _postService.DeleteAttachmentAsync(id);
        }
        return RedirectToAction("Edit", new { id = postId });
    }

    private CommentViewModel MapComment(Comment comment, int? currentUserId, bool isPostOwner)
    {
        var canSee = !comment.IsPrivate ||
                     isPostOwner ||
                     (currentUserId.HasValue && comment.AuthorId == currentUserId.Value);

        var displayContent = canSee ? comment.Content : "";
        var displayName = comment.Author?.Nickname ?? comment.GuestName ?? "Anonymous";

        return new CommentViewModel(
            comment.Id, displayName, comment.GuestEmail, displayContent,
            comment.CreatedAt,
            comment.Replies.Select(r => MapComment(r, currentUserId, isPostOwner)),
            comment.IsPrivate, comment.AuthorId,
            comment.Author?.Username);
    }

    private async Task HandleFileUploadsAsync(int postId, List<IFormFile> files)
    {
        var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", postId.ToString());
        Directory.CreateDirectory(uploadDir);

        foreach (var file in files.Where(f => f.Length > 0))
        {
            var safeFileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadDir, safeFileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            using var hashStream = SHA256.Create();
            using var cryptoStream = new CryptoStream(stream, hashStream, CryptoStreamMode.Write);
            await file.CopyToAsync(cryptoStream);
            await cryptoStream.FlushFinalBlockAsync();

            var hash = Convert.ToHexStringLower(hashStream.Hash!);
            var storedPath = $"/uploads/{postId}/{safeFileName}";

            await _postService.AddAttachmentAsync(postId, file.FileName, storedPath,
                file.ContentType, file.Length, hash);
        }
    }

    private async Task SetThumbnailAsync(int postId)
    {
        var attachments = await _postService.GetAttachmentsAsync(postId);
        var firstImage = attachments.FirstOrDefault(a =>
            a.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase));

        await _postService.SetThumbnailAsync(postId, firstImage?.StoredPath);
    }

    private static string? GetSafeFilePath(string storedPath)
    {
        var uploadsRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads"));
        var fullPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", storedPath.TrimStart('/')));
        return fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase) ? fullPath : null;
    }

    private static void DeleteFileIfSafe(string storedPath)
    {
        var safePath = GetSafeFilePath(storedPath);
        if (safePath is not null && System.IO.File.Exists(safePath))
        {
            System.IO.File.Delete(safePath);
        }
    }
}
