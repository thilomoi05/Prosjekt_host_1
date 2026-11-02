using System.Diagnostics;
using Beredskapsportal.Models;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Styrer forsiden (landingssiden) som alle besøkende møter først,
/// samt den generelle feilsiden.
/// Forsiden viser bare fast tekst, så den trenger ingen data fra databasen.
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
