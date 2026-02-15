using Microsoft.AspNetCore.Mvc;
using PEngineV.Models;

namespace PEngineV.Controllers;

public class PostController : Controller
{
    public IActionResult Read(int id)
    {
        return View(new PostViewModel(
            id,
            "Sample Post",
            "This is a sample post content. Backend logic will be implemented in a future task.",
            "Admin",
            DateTime.UtcNow,
            false));
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
}
