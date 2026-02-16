using Microsoft.AspNetCore.Mvc;

using PEngineV.Models;

namespace PEngineV.Controllers;

[IgnoreAntiforgeryToken]
public class ErrorController : Controller
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("/Error")]
    public IActionResult Index()
    {
        return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [Route("/Error/{code:int}")]
    public IActionResult StatusCode(int code)
    {
        Response.StatusCode = code;
        return code switch
        {
            400 => View("Error400"),
            403 => View("Error403"),
            404 => View("Error404"),
            500 => View("Error500"),
            _ => View("Index", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier })
        };
    }
}
