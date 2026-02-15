using Microsoft.AspNetCore.Mvc;
using PEngineV.Models;

namespace PEngineV.Controllers;

public class GuestbookController : Controller
{
    public IActionResult Index()
    {
        var entries = new List<GuestbookEntryViewModel>
        {
            new(1, "Alice", "Great blog! Keep it up.", DateTime.UtcNow.AddDays(-2)),
            new(2, "Bob", "Love the code-editor design.", DateTime.UtcNow.AddDays(-1))
        };

        return View(entries);
    }
}
