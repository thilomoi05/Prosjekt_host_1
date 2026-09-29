using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;


namespace Beredskapsportal.Controllers;

/// <summary>
/// Styrer forsiden (landingssiden) som alle besøkende møter først,
/// samt den generelle feilsiden.
/// </summary>
public class HomeController : Controller
{
    
        private readonly IBehovRepository _behovRepository;
        private readonly IRessursRepository _ressursRepository;

        // ASP.NET gir oss repositoryene automatisk (dependency injection),
        // fordi de er registrert i Program.cs.
        public HomeController(IBehovRepository behovRepository, IRessursRepository ressursRepository)
        {
            _behovRepository = behovRepository;
            _ressursRepository = ressursRepository;
        }
        
    public IActionResult Index()
    {
        var modell = new ForsideViewModel
        {
            AntallAktiveBehov = _behovRepository.TellAktive(),
            AntallAkutteBehov = _behovRepository.TellAkutte(),
            AntallTilgjengeligeRessurser = _ressursRepository.TellTilgjengelige()
        };

        return View(modell);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

