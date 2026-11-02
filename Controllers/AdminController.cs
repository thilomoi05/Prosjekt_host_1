using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Adminsiden (/Admin). Bare for rollen Admin. Her gjør administratoren fire ting:
///  1. Godkjenner eller avslår forespørsler fra offentlige aktører (ett klikk tildeler ressursen).
///  2. Godkjenner eller avviser nye ressurser (så falske innsendinger ikke vises).
///  3. Tildeler ledige ressurser til behov som venter, og fjerner tildelinger.
///  4. Godkjenner eller avviser offentlige aktører som har registrert seg.
///
/// Selve reglene for godkjenning og tildeling ligger i TildelingTjeneste.
/// Alle handlinger som endrer noe, er POST. Anti-forgery-tokenet sjekkes av filteret i Program.cs.
/// </summary>
[Authorize(Roles = Roller.Admin)]
public class AdminController : Controller
{
    private readonly IBehovRepository _behovRepository;
    private readonly IRessursRepository _ressursRepository;
    private readonly ITildelingRepository _tildelingRepository;
    private readonly TildelingTjeneste _tildelingTjeneste;
    private readonly UserManager<Bruker> _brukerManager;

    public AdminController(IBehovRepository behovRepository, IRessursRepository ressursRepository,
        ITildelingRepository tildelingRepository, TildelingTjeneste tildelingTjeneste, UserManager<Bruker> brukerManager)
    {
        _behovRepository = behovRepository;
        _ressursRepository = ressursRepository;
        _tildelingRepository = tildelingRepository;
        _tildelingTjeneste = tildelingTjeneste;
        _brukerManager = brukerManager;
    }

    // ---------------------------------------------------------------------
    // Oversikten med de tre køene
    // ---------------------------------------------------------------------

    public IActionResult Index()
    {
        // De ledige ressursene hentes én gang og brukes til å telle forslag for hvert behov.
        var ledige = _tildelingTjeneste.LedigeRessurser();

        var modell = new AdminViewModel
        {
            Foresporsler = _tildelingTjeneste.AlleVentendeForesporsler()
                .Select(f => LagForesporselRad(f))
                .ToList(),

            RessurserTilGodkjenning = _ressursRepository.HentAlle()
                .Where(r => r.Status == RessursStatus.Venter)
                .OrderBy(r => r.Id)
                .ToList(),

            BehovSomVenter = _behovRepository.HentAlle()
                .Where(b => b.Status == BehovStatus.Venter)
                .OrderByDescending(b => b.Prioritet)      // Hoy er størst i enumen
                .ThenByDescending(b => b.Dato)
                .Select(b => new VentendeBehov
                {
                    Behov = b,
                    AntallForslag = Tildelingsforslag.Lag(b, ledige).Count
                })
                .ToList(),

            // Users er Identity sin liste over alle brukere.
            BrukereTilGodkjenning = _brukerManager.Users
                .Where(b => b.VenterPaGodkjenning)
                .OrderBy(b => b.UserName)
                .ToList()
        };

        return View(modell);
    }

    // ---------------------------------------------------------------------
    // Forespørsler
    // ---------------------------------------------------------------------

    /// <summary>Godkjenner en forespørsel: ressursen tildeles behovet med en gang.</summary>
    [HttpPost]
    public IActionResult GodkjennForesporsel(int id)
    {
        var feil = _tildelingTjeneste.GodkjennForesporsel(id, Brukernavn());
        if (feil is null)
        {
            TempData["Melding"] = "Forespørselen er godkjent, og ressursen er tildelt behovet.";
        }
        else
        {
            TempData["Feil"] = feil;
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Avslår en forespørsel. Den som spurte, ser begrunnelsen.</summary>
    [HttpPost]
    public IActionResult AvslaForesporsel(int id, string? begrunnelse)
    {
        var tekst = string.IsNullOrWhiteSpace(begrunnelse) ? "Ingen begrunnelse oppgitt." : begrunnelse.Trim();
        _tildelingTjeneste.AvslaForesporsel(id, Kortet(tekst, 500), Brukernavn());
        TempData["Melding"] = "Forespørselen er avslått.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------------
    // Ressurser
    // ---------------------------------------------------------------------

    [HttpPost]
    public IActionResult GodkjennRessurs(int id)
    {
        _tildelingTjeneste.Godkjenn(id, Brukernavn());
        TempData["Melding"] = "Ressursen er godkjent og vises nå på kartet.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult AvvisRessurs(int id, string? begrunnelse)
    {
        // Eieren ser begrunnelsen. Er feltet tomt, får de en standardtekst.
        var tekst = string.IsNullOrWhiteSpace(begrunnelse) ? "Ingen begrunnelse oppgitt." : begrunnelse.Trim();
        _tildelingTjeneste.Avvis(id, Kortet(tekst, 500), Brukernavn());
        TempData["Melding"] = "Ressursen er avvist.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------------
    // Tildeling
    // ---------------------------------------------------------------------

    /// <summary>
    /// Siden der Admin velger ressurser til et behov, f.eks. /Admin/Tildel/5.
    /// visAlleTyper: true viser alle ledige ressurser, ikke bare de som passer til behovet.
    /// </summary>
    [HttpGet]
    public IActionResult Tildel(int id, bool visAlleTyper = false)
    {
        var modell = LagTildelModell(id, visAlleTyper);
        return modell is null ? NotFound() : View(modell);
    }

    /// <summary>Tildeler ressursene som er krysset av (ressursIder) til behovet.</summary>
    [HttpPost]
    public IActionResult Tildel(int id, List<int> ressursIder, string? kommentar, bool visAlleTyper = false)
    {
        var kortKommentar = kommentar is null ? null : Kortet(kommentar, 500);
        var feil = _tildelingTjeneste.Tildel(id, ressursIder, kortKommentar, Brukernavn());

        if (feil is not null)
        {
            // Viser siden på nytt med feilmeldingen øverst.
            var modell = LagTildelModell(id, visAlleTyper);
            if (modell is null)
            {
                return NotFound();
            }
            ModelState.AddModelError(string.Empty, feil);
            return View(modell);
        }

        TempData["Melding"] = ressursIder.Count == 1 ? "Ressursen er tildelt." : $"{ressursIder.Count} ressurser er tildelt.";
        return RedirectToAction("Detaljer", "Behov", new { id });
    }

    /// <summary>Fjerner én tildeling. Ressursen blir ledig og kan tildeles et annet behov.</summary>
    [HttpPost]
    public IActionResult FjernTildeling(int behovId, int ressursId)
    {
        _tildelingTjeneste.FjernTildeling(behovId, ressursId, Brukernavn());
        TempData["Melding"] = "Tildelingen er fjernet. Ressursen er ledig igjen.";
        return RedirectToAction("Detaljer", "Behov", new { id = behovId });
    }

    // ---------------------------------------------------------------------
    // Brukere
    // ---------------------------------------------------------------------

    /// <summary>Godkjenner en offentlig aktør: gir rollen OffentligAktor, så de kan logge inn.</summary>
    [HttpPost]
    public async Task<IActionResult> GodkjennBruker(string id)
    {
        var bruker = await _brukerManager.FindByIdAsync(id);
        if (bruker is null || !bruker.VenterPaGodkjenning)
        {
            return NotFound();
        }

        bruker.VenterPaGodkjenning = false;
        await _brukerManager.UpdateAsync(bruker);
        await _brukerManager.AddToRoleAsync(bruker, Roller.OffentligAktor);

        TempData["Melding"] = $"{bruker.FulltNavn} er godkjent som offentlig aktør og kan logge inn.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Avviser en offentlig aktør ved å slette kontoen. Bare kontoer som venter på godkjenning
    /// kan slettes her, så ingen kan slette andre brukere ved å endre id-en.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AvvisBruker(string id)
    {
        var bruker = await _brukerManager.FindByIdAsync(id);
        if (bruker is null || !bruker.VenterPaGodkjenning)
        {
            return NotFound();
        }

        await _brukerManager.DeleteAsync(bruker);

        TempData["Melding"] = $"Kontoen til {bruker.FulltNavn} er avvist og slettet.";
        return RedirectToAction(nameof(Index));
    }

    // ---------------------------------------------------------------------
    // Hjelpere
    // ---------------------------------------------------------------------

    /// <summary>Bygger modellen til Tildel-siden. Null hvis behovet ikke finnes.</summary>
    private TildelViewModel? LagTildelModell(int behovId, bool visAlleTyper)
    {
        var behov = _behovRepository.HentMedId(behovId);
        if (behov is null)
        {
            return null;
        }

        var tildelteIder = _tildelingRepository.HentAlle().Where(t => t.BehovId == behovId).Select(t => t.RessursId).ToList();

        return new TildelViewModel
        {
            Behov = behov,
            TildelteRessurser = _ressursRepository.HentAlle().Where(r => tildelteIder.Contains(r.Id)).ToList(),
            Forslag = _tildelingTjeneste.LagForslag(behov, visAlleTyper),
            VisAlleTyper = visAlleTyper
        };
    }

    /// <summary>Én forespørsel med behovet, ressursen og avstanden mellom dem.</summary>
    private ForesporselRad LagForesporselRad(Foresporsel foresporsel)
    {
        var behov = _behovRepository.HentMedId(foresporsel.BehovId);
        var ressurs = _ressursRepository.HentMedId(foresporsel.RessursId);
        return new ForesporselRad
        {
            Foresporsel = foresporsel,
            Behov = behov,
            Ressurs = ressurs,
            AvstandKm = behov is null || ressurs is null ? 0
                : Tildelingsforslag.AvstandKm(behov.Breddegrad, behov.Lengdegrad, ressurs.Breddegrad, ressurs.Lengdegrad)
        };
    }

    private string Brukernavn() => User?.Identity?.Name ?? string.Empty;

    // Klipper teksten hvis den er for lang for databasefeltet.
    private static string Kortet(string tekst, int maks) => tekst.Length <= maks ? tekst : tekst[..maks];
}
