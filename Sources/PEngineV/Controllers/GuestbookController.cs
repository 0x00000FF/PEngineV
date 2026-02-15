using Microsoft.AspNetCore.Mvc;
using PEngineV.Models;

namespace PEngineV.Controllers;

public class GuestbookController : Controller
{
    public IActionResult Index()
    {
        var entries = new List<GuestbookEntryViewModel>
        {
            new(1, "Alice", "alice@example.com", "Great blog! Keep it up.", DateTime.UtcNow.AddDays(-2),
                new GuestbookReplyViewModel(1, "Admin", "Thank you, Alice!", DateTime.UtcNow.AddDays(-1))),
            new(2, "Bob", null, "Love the code-editor design.", DateTime.UtcNow.AddDays(-1))
        };

        return View(new GuestbookPageViewModel(entries, false, false));
    }

    [HttpPost]
    public IActionResult Write(GuestbookWriteViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return RedirectToAction("Index");
    }

    [HttpPost]
    public IActionResult Reply(int entryId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return RedirectToAction("Index");
        }

        return RedirectToAction("Index");
    }
}
