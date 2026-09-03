using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Viser dashbordet innloggede brukere møter: nøkkeltall og de sist meldte behovene.
/// [Authorize] gjør at uinnloggede besøkende automatisk sendes til innloggingssiden.
/// </summary>
[Authorize]
public class OversiktController : Controller
{
    private readonly IBehovRepository _behovRepository;
    private readonly IRessursRepository _ressursRepository;

    public OversiktController(IBehovRepository behovRepository, IRessursRepository ressursRepository)
    {
        _behovRepository = behovRepository;
        _ressursRepository = ressursRepository;
    }

    public IActionResult Index()
    {
        var modell = new OversiktViewModel
        {
            AntallAktiveBehov = _behovRepository.TellAktive(),
            AntallTilgjengeligeRessurser = _ressursRepository.TellTilgjengelige(),
            AntallUnderBehandling = _behovRepository.TellUnderBehandling(),
            SisteBehov = _behovRepository.HentSiste(5).ToList()
        };

        return View(modell);
    }
}
