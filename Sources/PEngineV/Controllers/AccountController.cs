using Microsoft.AspNetCore.Mvc;
using PEngineV.Models;

namespace PEngineV.Controllers;

public class AccountController : Controller
{
    public IActionResult Login()
    {
        return View(new LoginViewModel("", ""));
    }

    public IActionResult Register()
    {
        return View(new RegisterViewModel("", "", "", ""));
    }
}
