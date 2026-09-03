using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Viser det kartbaserte koordineringsverktøyet. Tilgjengelig for alle,
/// også besøkende som ikke er logget inn, siden det lenkes til fra forsiden.
/// Selve kartintegrasjonen er ikke bygget ennå - dette er et enkelt utgangspunkt.
/// </summary>
public class KartController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
