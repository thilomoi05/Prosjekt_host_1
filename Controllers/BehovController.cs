using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Håndterer listevisning og registrering av behov.
/// Bare offentlige aktører (Heimevernet, kommuner osv.) kan se og melde behov.
/// Private aktører skal bare tilby ressurser, og sendes til oversikten hvis de prøver
/// å åpne disse sidene (se AccessDeniedPath i Program.cs).
/// </summary>
[Authorize(Roles = nameof(BrukerRolle.OffentligAktor))]
public class BehovController : Controller
{
    private readonly IBehovRepository _behovRepository;
    private readonly IRessursRepository _ressursRepository;
    private readonly ITildelingRepository _tildelingRepository;

    public BehovController(IBehovRepository behovRepository, IRessursRepository ressursRepository,
        ITildelingRepository tildelingRepository)
    {
        _behovRepository = behovRepository;
        _ressursRepository = ressursRepository;
        _tildelingRepository = tildelingRepository;
    }

    public IActionResult Index()
    {
        var alleBehov = _behovRepository.HentAlle();
        return View(alleBehov);
    }

    /// <summary>
    /// Detaljsiden for ett behov, f.eks. /Behov/Detaljer/5.
    /// Viser alt om behovet, et lite kart og ressursene som er tildelt det.
    /// </summary>
    [HttpGet]
    public IActionResult Detaljer(int id)
    {
        var behov = _behovRepository.HentMedId(id);
        if (behov is null)
        {
            return NotFound();   // ukjent id: "Fant ikke siden"
        }

        // Id-ene til ressursene som er tildelt dette behovet.
        var ressursIder = _tildelingRepository.HentAlle()
            .Where(t => t.BehovId == id)
            .Select(t => t.RessursId)
            .ToList();

        var modell = new BehovDetaljerViewModel
        {
            Behov = behov,
            TildelteRessurser = _ressursRepository.HentAlle()
                .Where(r => ressursIder.Contains(r.Id))
                .ToList()
        };

        return View(modell);
    }

    [HttpGet]
    public IActionResult Registrer()
    {
        return View(new BehovViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Registrer(BehovViewModel modell)
    {
        // Sikkerhetsnett: kartet setter alltid begge koordinatene samtidig, men
        // skulle Lengdegrad likevel mangle, gir vi samme feilmelding som for Breddegrad.
        // (Gjelder bare når Breddegrad finnes, ellers ville meldingen kommet to ganger.)
        if (modell.Breddegrad is not null && modell.Lengdegrad is null)
        {
            ModelState.AddModelError(nameof(modell.Breddegrad), "Du må klikke i kartet for å markere plasseringen.");
        }

        if (!ModelState.IsValid)
        {
            return View(modell);
        }

        var nyttBehov = new Behov
        {
            Type = modell.Type!.Value,
            Beskrivelse = modell.Beskrivelse,
            GeografiskOmrade = modell.GeografiskOmrade,
            Prioritet = modell.Prioritet,
            Status = BehovStatus.Venter,
            Kontaktperson = modell.Kontaktperson,
            Telefon = modell.Telefon,
            Dato = DateOnly.FromDateTime(DateTime.Now),
            // ".Value" henter tallet ut av double?. Det er trygt her, fordi
            // ModelState.IsValid over allerede har sjekket at feltene er fylt ut.
            Breddegrad = modell.Breddegrad!.Value,
            Lengdegrad = modell.Lengdegrad!.Value
        };

        _behovRepository.LeggTil(nyttBehov);

        return RedirectToAction(nameof(Index));
    }
}
