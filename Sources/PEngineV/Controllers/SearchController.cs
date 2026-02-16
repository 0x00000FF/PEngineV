using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;

using PEngineV.Models;
using PEngineV.Services;

namespace PEngineV.Controllers;

public class SearchController : Controller
{
    private readonly IPostService _postService;

    public SearchController(IPostService postService)
    {
        _postService = postService;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null ? int.Parse(claim.Value) : null;
    }

    public async Task<IActionResult> Index(string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return View(new SearchViewModel("", Enumerable.Empty<SearchPostResult>(), Enumerable.Empty<SearchCommentResult>()));
        }

        var userId = GetCurrentUserId();
        var (posts, comments) = await _postService.SearchAsync(q, userId);

        var postResults = posts.Select(p => new SearchPostResult(
            p.Id, p.Title, p.Author.Nickname,
            p.Content.Length > 200 ? p.Content[..200] + "..." : p.Content,
            p.PublishAt ?? p.CreatedAt,
            p.Author.Username)).ToList();

        var commentResults = comments.Select(c => new SearchCommentResult(
            c.Id, c.PostId, c.Post.Title,
            c.Author?.Nickname ?? c.GuestName ?? "Anonymous",
            c.Content.Length > 200 ? c.Content[..200] + "..." : c.Content,
            c.CreatedAt,
            c.Author?.Username)).ToList();

        return View(new SearchViewModel(q, postResults, commentResults));
    }
}
