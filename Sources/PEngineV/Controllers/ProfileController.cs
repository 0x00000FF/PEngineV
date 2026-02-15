using Microsoft.AspNetCore.Mvc;
using PEngineV.Models;

namespace PEngineV.Controllers;

public class ProfileController : Controller
{
    public IActionResult Index(string username)
    {
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
            null,
            null,
            DateTime.UtcNow.AddMonths(-6),
            DateTime.UtcNow.AddHours(-1),
            posts,
            comments,
            guestbookReplies);

        return View(profile);
    }
}
