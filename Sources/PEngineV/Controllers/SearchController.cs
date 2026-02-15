using Microsoft.AspNetCore.Mvc;
using PEngineV.Models;

namespace PEngineV.Controllers;

public class SearchController : Controller
{
    public IActionResult Index(string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return View(new SearchViewModel("", Enumerable.Empty<SearchPostResult>(), Enumerable.Empty<SearchCommentResult>()));
        }

        var posts = new List<SearchPostResult>
        {
            new(1, "hello-world.md", "Admin",
                "This is a sample post content matching the search query...",
                DateTime.UtcNow.AddDays(-30))
        };

        var comments = new List<SearchCommentResult>
        {
            new(1, 1, "hello-world.md", "Alice",
                "Great post! This comment matches the search...",
                DateTime.UtcNow.AddHours(-5))
        };

        return View(new SearchViewModel(q, posts, comments));
    }
}
