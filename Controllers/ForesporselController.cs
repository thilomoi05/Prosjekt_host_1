using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Lar en offentlig aktør finne og forespørre ressurser til sine egne behov.
/// En administrator godkjenner eller avslår forespørselen på adminsiden (AdminController).
/// Slik slipper Admin å finne ressursene selv, men har fortsatt kontrollen.
///
/// Bare den som meldte behovet (eller en administrator) kan forespørre til det.
/// Reglene for forespørsler ligger i TildelingTjeneste.
/// </summary>
[Authorize(Roles = Roller.OffentligAktor)]
public class ForesporselController : Controller
{
    private readonly IBehovRepository _behovRepository;
    private readonly TildelingTjeneste _tildelingTjeneste;

    public ForesporselController(IBehovRepository behovRepository, TildelingTjeneste tildelingTjeneste)
    {
        _behovRepository = behovRepository;
        _tildelingTjeneste = tildelingTjeneste;
    }

    /// <summary>
    /// Forslag til ressurser for et behov, f.eks. /Foresporsel/FinnRessurs/5.
    /// Samme forslag som Admin får: passende type, nærmeste først.
    /// </summary>
    [HttpGet]
    public IActionResult FinnRessurs(int id, bool visAlleTyper = false)
    {
        var behov = _behovRepository.HentMedId(id);
        if (behov is null)
        {
            return NotFound();
        }
        if (!ErEgetBehov(behov))
        {
            return Forbid();
        }

        var modell = new FinnRessursViewModel
        {
            Behov = behov,
            Forslag = _tildelingTjeneste.LagForslag(behov, visAlleTyper),
            VisAlleTyper = visAlleTyper,
            AlleredeForespurt = _tildelingTjeneste.ForesporslerForBehov(id)
                .Where(f => f.Status == ForesporselStatus.Venter)
                .Select(f => f.RessursId)
                .ToHashSet()
        };

        return View(modell);
    }

    /// <summary>
    /// Sender en forespørsel. Brukes både fra "Finn ressurs" og fra detaljsiden til en ressurs.
    /// fraRessurs: true sender brukeren tilbake til ressursen i stedet for til behovet.
    /// Anti-forgery-tokenet sjekkes av filteret i Program.cs.
    /// </summary>
    [HttpPost]
    public IActionResult Foresporr(int behovId, int ressursId, string? kommentar, bool fraRessurs = false)
    {
        var behov = _behovRepository.HentMedId(behovId);
        if (behov is null)
        {
            return NotFound();
        }
        if (!ErEgetBehov(behov))
        {
            return Forbid();
        }

        var kortKommentar = kommentar is { Length: > 500 } ? kommentar[..500] : kommentar;
        var feil = _tildelingTjeneste.Foresporr(behovId, ressursId, kortKommentar, Brukernavn());

        if (feil is null)
        {
            TempData["Melding"] = "Forespørselen er sendt. En administrator godkjenner den.";
        }
        else
        {
            TempData["Feil"] = feil;
        }

        return fraRessurs
            ? RedirectToAction("Detaljer", "Ressurs", new { id = ressursId })
            : RedirectToAction("Detaljer", "Behov", new { id = behovId });
    }

    // Brukernavnet til den innloggede brukeren. "?." gjør at det også virker i tester uten innlogging.
    private string Brukernavn() => User?.Identity?.Name ?? string.Empty;

    // Bare den som meldte behovet, og administratorer, kan forespørre ressurser til det.
    private bool ErEgetBehov(Behov behov) =>
        User?.IsInRole(Roller.Admin) == true
        || (!string.IsNullOrEmpty(behov.RegistrertAv) && behov.RegistrertAv == Brukernavn());
}
