using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PEngineV.Models;

namespace PEngineV.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var posts = new List<HomePostItem>
        {
            new(1, "hello-world.md", null, "Admin", DateTime.UtcNow.AddDays(-30), false),
            new(2, "getting-started.md", null, "Admin", DateTime.UtcNow.AddDays(-25), false),
            new(3, "secret-notes.md", null, "Admin", DateTime.UtcNow.AddDays(-14), true),
            new(4, "csharp-tips.md", "tutorials", "Admin", DateTime.UtcNow.AddDays(-7), false)
        };

        return View(new HomeViewModel(posts));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
