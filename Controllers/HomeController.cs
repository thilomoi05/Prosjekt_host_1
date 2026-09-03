using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Beredskapsportal.Models;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Styrer forsiden (landingssiden) som alle besøkende møter først,
/// samt den generelle feilsiden.
/// </summary>
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
