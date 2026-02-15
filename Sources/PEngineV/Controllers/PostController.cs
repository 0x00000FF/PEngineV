using Microsoft.AspNetCore.Mvc;
using PEngineV.Models;

namespace PEngineV.Controllers;

public class PostController : Controller
{
    public IActionResult Read(int id)
    {
        var post = new PostViewModel(
            id,
            "Sample Post",
            "This is a sample post content. Backend logic will be implemented in a future task.",
            "Admin",
            DateTime.UtcNow,
            false);

        var comments = new List<CommentViewModel>
        {
            new(1, "Alice", "alice@example.com", "Great post!", DateTime.UtcNow.AddHours(-5),
                new List<CommentViewModel>
                {
                    new(3, "Admin", null, "Thank you, Alice!", DateTime.UtcNow.AddHours(-4),
                        Enumerable.Empty<CommentViewModel>())
                }),
            new(2, "Bob", null, "Very informative, thanks for sharing.", DateTime.UtcNow.AddHours(-2),
                Enumerable.Empty<CommentViewModel>())
        };

        return View(new PostReadViewModel(post, comments, false, false));
    }

    public IActionResult Write()
    {
        return View(new PostWriteViewModel(null, "", "", null));
    }

    public IActionResult Edit(int id)
    {
        return View("Write", new PostWriteViewModel(
            id,
            "Sample Post",
            "Sample content for editing.",
            null));
    }

    public IActionResult Delete(int id)
    {
        return View(new PostDeleteViewModel(id, "Sample Post"));
    }

    public IActionResult Protected(int id)
    {
        return View(new PostProtectedViewModel(id, "Protected Post"));
    }

    public IActionResult Locked()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Comment(CommentWriteViewModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return RedirectToAction("Read", new { id = model.PostId });
    }
}
