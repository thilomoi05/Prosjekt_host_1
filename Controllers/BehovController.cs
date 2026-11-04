using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Håndterer listevisning, registrering, detaljsiden, endring og tilbaketrekking av behov.
/// Bare offentlige aktører (Heimevernet, kommuner osv.) kan se og melde behov.
/// Tildeling av ressurser gjøres av Admin (AdminController).
/// Private aktører skal bare tilby ressurser, og sendes til oversikten hvis de prøver
/// å åpne disse sidene (se AccessDeniedPath i Program.cs).
/// </summary>
[Authorize(Roles = Roller.OffentligAktor)]
public class BehovController : Controller
{
    private readonly IBehovRepository _behovRepository;
    private readonly IRessursRepository _ressursRepository;
    private readonly ITildelingRepository _tildelingRepository;
    private readonly TildelingTjeneste _tildelingTjeneste;

    public BehovController(IBehovRepository behovRepository, IRessursRepository ressursRepository,
        ITildelingRepository tildelingRepository, TildelingTjeneste tildelingTjeneste)
    {
        _behovRepository = behovRepository;
        _ressursRepository = ressursRepository;
        _tildelingRepository = tildelingRepository;
        _tildelingTjeneste = tildelingTjeneste;
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
                .ToList(),
            Historikk = _tildelingTjeneste.HistorikkForBehov(id),
            KanTrekkeTilbake = KanTrekkeTilbake(behov),
            ErAdmin = User?.IsInRole(Roller.Admin) == true,
            Foresporsler = _tildelingTjeneste.ForesporslerForBehov(id)
                .Select(f => new ForesporselRad { Foresporsel = f, Ressurs = _ressursRepository.HentMedId(f.RessursId) })
                .ToList()
        };

        return View(modell);
    }

    /// <summary>
    /// Skjemaet for å endre et behov, f.eks. /Behov/Rediger/5. Feltene er fylt ut med det som er lagret.
    /// Den som meldte behovet og administratorer kan endre det (samme regel som for å trekke det tilbake).
    /// Andre offentlige aktører kan se behovet, men ikke endre det.
    /// </summary>
    [HttpGet]
    public IActionResult Rediger(int id)
    {
        var behov = _behovRepository.HentMedId(id);
        if (behov is null)
        {
            return NotFound();
        }
        if (!KanTrekkeTilbake(behov))
        {
            return Forbid();
        }

        ViewData["BehovId"] = behov.Id;
        return View(new BehovViewModel
        {
            Type = behov.Type,
            Beskrivelse = behov.Beskrivelse,
            GeografiskOmrade = behov.GeografiskOmrade,
            Prioritet = behov.Prioritet,
            Kontaktperson = behov.Kontaktperson,
            Telefon = behov.Telefon,
            Breddegrad = behov.Breddegrad,
            Lengdegrad = behov.Lengdegrad
        });
    }

    /// <summary>
    /// Lagrer endringene. Tilgangen sjekkes på nytt, fordi en POST kan sendes uten å gå via siden over.
    /// Behov trenger ikke godkjenning (de meldes av offentlige aktører som allerede er godkjent),
    /// så endringen gjelder med en gang. Den skrives i historikken, med hva som ble endret i prioritet.
    /// Status, dato og hvem som meldte behovet endres ikke.
    /// </summary>
    [HttpPost]
    public IActionResult Rediger(int id, BehovViewModel modell)
    {
        var behov = _behovRepository.HentMedId(id);
        if (behov is null)
        {
            return NotFound();
        }
        if (!KanTrekkeTilbake(behov))
        {
            return Forbid();
        }

        if (modell.Breddegrad is not null && modell.Lengdegrad is null)
        {
            ModelState.AddModelError(nameof(modell.Breddegrad), "Du må klikke i kartet for å markere plasseringen.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["BehovId"] = id;
            return View(modell);
        }

        // Prioriteten er det viktigste å kunne følge med på, så den nevnes i historikken.
        var gammelPrioritet = behov.Prioritet;

        _behovRepository.Oppdater(new Behov
        {
            Id = id,
            Type = modell.Type!.Value,
            Beskrivelse = modell.Beskrivelse,
            GeografiskOmrade = modell.GeografiskOmrade,
            Prioritet = modell.Prioritet,
            Kontaktperson = modell.Kontaktperson,
            Telefon = modell.Telefon,
            Breddegrad = modell.Breddegrad!.Value,
            Lengdegrad = modell.Lengdegrad!.Value
        });

        var beskrivelse = gammelPrioritet == modell.Prioritet
            ? "Behovet ble endret"
            : $"Behovet ble endret, prioritet {PrioritetVisning.Tekst(gammelPrioritet).ToLowerInvariant()} → {PrioritetVisning.Tekst(modell.Prioritet).ToLowerInvariant()}";
        _tildelingTjeneste.Logg(beskrivelse, Brukernavn(), behovId: id);

        TempData["Melding"] = "Endringene er lagret.";
        return RedirectToAction(nameof(Detaljer), new { id });
    }

    /// <summary>
    /// "Er du sikker?"-siden før et behov trekkes tilbake, f.eks. /Behov/TrekkTilbake/5.
    /// Viser hvilke ressurser som blir ledige igjen.
    /// </summary>
    [HttpGet]
    public IActionResult TrekkTilbake(int id)
    {
        var behov = _behovRepository.HentMedId(id);
        if (behov is null)
        {
            return NotFound();
        }
        if (!KanTrekkeTilbake(behov))
        {
            return Forbid();
        }

        var ressursIder = _tildelingRepository.HentAlle().Where(t => t.BehovId == id).Select(t => t.RessursId).ToList();
        var modell = new BehovDetaljerViewModel
        {
            Behov = behov,
            TildelteRessurser = _ressursRepository.HentAlle().Where(r => ressursIder.Contains(r.Id)).ToList()
        };
        return View(modell);
    }

    /// <summary>
    /// Trekker tilbake (sletter) behovet. Tildelte ressurser blir ledige igjen (se TildelingTjeneste).
    /// "ActionName" gjør at denne metoden svarer på POST til samme adresse som siden over.
    /// </summary>
    [HttpPost]
    [ActionName(nameof(TrekkTilbake))]
    public IActionResult TrekkTilbakeBekreftet(int id)
    {
        var behov = _behovRepository.HentMedId(id);
        if (behov is null)
        {
            return NotFound();
        }
        if (!KanTrekkeTilbake(behov))
        {
            return Forbid();
        }

        _tildelingTjeneste.TrekkTilbakeBehov(id, Brukernavn());
        TempData["Melding"] = "Behovet er trukket tilbake.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Registrer()
    {
        return View(new BehovViewModel());
    }

    // Anti-forgery-tokenet sjekkes av filteret i Program.cs (gjelder alle POST-metoder).
    [HttpPost]
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
            RegistrertAv = Brukernavn(),
            // ".Value" henter tallet ut av double?. Det er trygt her, fordi
            // ModelState.IsValid over allerede har sjekket at feltene er fylt ut.
            Breddegrad = modell.Breddegrad!.Value,
            Lengdegrad = modell.Lengdegrad!.Value
        };

        _behovRepository.LeggTil(nyttBehov);
        _tildelingTjeneste.Logg("Behovet ble meldt inn", Brukernavn(), behovId: nyttBehov.Id);

        return RedirectToAction(nameof(Index));
    }

    // Brukernavnet til den innloggede brukeren. "?." gjør at det også virker i tester uten innlogging.
    private string Brukernavn() => User?.Identity?.Name ?? string.Empty;

    // Den som meldte behovet, og administratorer, kan trekke det tilbake.
    private bool KanTrekkeTilbake(Behov behov) =>
        User?.IsInRole(Roller.Admin) == true
        || (!string.IsNullOrEmpty(behov.RegistrertAv) && behov.RegistrertAv == Brukernavn());
}
