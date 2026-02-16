using Microsoft.AspNetCore.Mvc;
using PEngineV.Models;
using PEngineV.Services;

namespace PEngineV.Controllers;

public class ProfileController : Controller
{
    private readonly IUserService _userService;
    private readonly IPostService _postService;

    public ProfileController(IUserService userService, IPostService postService)
    {
        _userService = userService;
        _postService = postService;
    }

    public async Task<IActionResult> Index(string username)
    {
        var user = await _userService.GetByUsernameAsync(username);

        var posts = new List<ProfilePostItem>();
        var comments = new List<ProfileCommentItem>();

        if (user is not null)
        {
            var userPosts = await _postService.GetPublicPostsByAuthorAsync(user.Id);
            posts = userPosts.Select(p => new ProfilePostItem(
                p.Id, p.Title, p.PublishAt ?? p.CreatedAt)).ToList();

            var userComments = await _postService.GetCommentsByAuthorAsync(user.Id);
            comments = userComments.Select(c => new ProfileCommentItem(
                c.Id, c.PostId, c.Post.Title,
                c.Content.Length > 100 ? c.Content[..100] + "..." : c.Content,
                c.CreatedAt)).ToList();
        }

        var guestbookReplies = new List<ProfileGuestbookItem>();

        var profile = new ProfileViewModel(
            username,
            user?.Nickname,
            user?.ProfileImageUrl,
            user?.Bio,
            user?.ContactEmail,
            user?.CreatedAt ?? DateTime.UtcNow,
            user?.LastLoginAt ?? DateTime.UtcNow,
            posts,
            comments,
            guestbookReplies);

        return View(profile);
    }
}
