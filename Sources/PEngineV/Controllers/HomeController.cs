using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;

using PEngineV.Models;
using PEngineV.Services;

namespace PEngineV.Controllers;

public class HomeController : Controller
{
    private readonly IPostService _postService;

    public HomeController(IPostService postService)
    {
        _postService = postService;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim is not null ? int.Parse(claim.Value) : null;
    }

    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var posts = await _postService.GetPublishedPostsAsync(userId);

        var items = posts.Select(p => new HomePostItem(
            p.Id, p.Title, p.Category?.Name, p.Author.Nickname,
            p.PublishAt ?? p.CreatedAt, p.IsProtected, p.ThumbnailUrl,
            p.PostTags.Select(pt => pt.Tag.Name),
            p.Author.Username)).ToList();

        return View(new HomeViewModel(items));
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
