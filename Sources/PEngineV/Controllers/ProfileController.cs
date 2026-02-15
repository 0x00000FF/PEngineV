using Microsoft.AspNetCore.Mvc;
using PEngineV.Models;
using PEngineV.Services;

namespace PEngineV.Controllers;

public class ProfileController : Controller
{
    private readonly IUserService _userService;

    public ProfileController(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IActionResult> Index(string username)
    {
        var user = await _userService.GetByUsernameAsync(username);

        var posts = new List<ProfilePostItem>
        {
            new(1, "hello-world.md", DateTime.UtcNow.AddDays(-30)),
            new(2, "getting-started.md", DateTime.UtcNow.AddDays(-25))
        };

        var comments = new List<ProfileCommentItem>
        {
            new(1, 1, "hello-world.md", "Great post!", DateTime.UtcNow.AddHours(-5))
        };

        var guestbookReplies = new List<ProfileGuestbookItem>
        {
            new(1, "Thank you, Alice!", DateTime.UtcNow.AddDays(-1))
        };

        var profile = new ProfileViewModel(
            username,
            user?.Nickname,
            user?.ProfileImageUrl,
            user?.Bio,
            user?.ContactEmail,
            user?.CreatedAt ?? DateTime.UtcNow.AddMonths(-6),
            user?.LastLoginAt ?? DateTime.UtcNow.AddHours(-1),
            posts,
            comments,
            guestbookReplies);

        return View(profile);
    }
}
