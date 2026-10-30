using System.Security.Claims;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Håndterer registrering av ressurser som bedrifter, entreprenører og privatpersoner tilbyr,
/// inkludert opplasting og visning av bilder av ressursen.
/// </summary>
[Authorize]
public class RessursController : Controller
{
    // Regler for bildeopplasting. Samlet her, så de er lette å finne og endre.
    private const int MaksAntallBilder = 5;
    private const long MaksBildestorrelse = 5 * 1024 * 1024;   // 5 MB per bilde
    private static readonly string[] TillatteFiltyper = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly IRessursRepository _ressursRepository;
    private readonly IBehovRepository _behovRepository;
    private readonly ITildelingRepository _tildelingRepository;

    // Mappen der bildene lagres. Den ligger bevisst UTENFOR wwwroot:
    // alt i wwwroot kan alle laste ned, men bildene skal bare autoriserte se.
    private readonly string _bildemappe;

    // IWebHostEnvironment gir oss blant annet stien til prosjektmappen (ContentRootPath).
    public RessursController(IRessursRepository ressursRepository, IBehovRepository behovRepository,
        ITildelingRepository tildelingRepository, IWebHostEnvironment miljo)
    {
        _ressursRepository = ressursRepository;
        _behovRepository = behovRepository;
        _tildelingRepository = tildelingRepository;
        _bildemappe = Path.Combine(miljo.ContentRootPath, "Opplastinger", "bilder");
    }

    // Nav-punktet "Ressurser" peker rett på registreringsskjemaet,
    // siden det foreløpig ikke finnes noen egen liste-visning for ressurser.
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Registrer));
    }

    [HttpGet]
    public IActionResult Registrer()
    {
        // Kontaktpersonen fylles inn med navnet til den innloggede brukeren,
        // så de fleste slipper å skrive det. Det kan endres i skjemaet.
        var modell = new RessursViewModel
        {
            Kontaktperson = User?.FindFirst(ClaimTypes.GivenName)?.Value ?? string.Empty
        };
        return View(modell);
    }

    // "async Task" fordi det tar litt tid å lagre filer på disk. Mens vi venter
    // (await), kan serveren gjøre andre ting i stedet for å stå og vente.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrer(RessursViewModel modell)
    {
        // Sjekker bildene i tillegg til de vanlige [Required]-reglene i view-modellen.
        SjekkBilder(modell.Bilder);

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

        // Lagrer bildene på disk og tar vare på filnavnene.
        var filnavn = new List<string>();
        foreach (var bilde in modell.Bilder)
        {
            filnavn.Add(await LagreBilde(bilde));
        }

        var nyRessurs = new Ressurs
        {
            Type = modell.Type!.Value,
            BeskrivelseAvKapasitet = modell.BeskrivelseAvKapasitet,
            Adresse = modell.Adresse,
            Tilgangsbeskrivelse = modell.Tilgangsbeskrivelse,
            Bilder = filnavn,
            TilgjengeligFra = modell.TilgjengeligFra,
            TilgjengeligTil = modell.TilgjengeligTil,
            Kontaktperson = modell.Kontaktperson,
            Telefon = modell.Telefon,
            // Brukernavnet til den innloggede brukeren (fra innloggingscookien).
            // "?." og "??" gjør at det blir tom tekst hvis ingen er logget inn (f.eks. i tester).
            RegistrertAv = User?.Identity?.Name ?? string.Empty,
            // Trygt å bruke ".Value": ModelState.IsValid har sjekket at feltene finnes.
            Breddegrad = modell.Breddegrad!.Value,
            Lengdegrad = modell.Lengdegrad!.Value
        };

        _ressursRepository.LeggTil(nyRessurs);

        // Meldingen vises øverst på neste side (se _Layout.cshtml). TempData husker den
        // akkurat så lenge at den overlever omdirigeringen til oversikten.
        TempData["Melding"] = "Takk! Ressursen er registrert.";

        return RedirectToAction("Index", "Oversikt");
    }

    /// <summary>
    /// Detaljsiden for én ressurs, f.eks. /Ressurs/Detaljer/7.
    /// Tilgang:
    ///  - Offentlige aktører kan se alle ressurser.
    ///  - Private aktører kan bare se sine egne. Prøver de å åpne en annen,
    ///    sendes de til oversikten (Forbid() + AccessDeniedPath i Program.cs).
    /// </summary>
    [HttpGet]
    public IActionResult Detaljer(int id)
    {
        var ressurs = _ressursRepository.HentMedId(id);
        if (ressurs is null)
        {
            return NotFound();   // ukjent id: "Fant ikke siden"
        }

        var erOffentligAktor = ErOffentligAktor();
        if (!erOffentligAktor && ressurs.RegistrertAv != Brukernavn())
        {
            return Forbid();
        }

        // Er ressursen satt inn på et behov? I så fall finnes det en Tildeling med denne ressursen.
        var tildeling = _tildelingRepository.HentAlle().FirstOrDefault(t => t.RessursId == id);

        var modell = new RessursDetaljerViewModel
        {
            Ressurs = ressurs,
            ErTildelt = tildeling is not null,
            // Bare offentlige aktører får vite hvilket behov ressursen er tildelt.
            TildeltBehov = tildeling is not null && erOffentligAktor
                ? _behovRepository.HentMedId(tildeling.BehovId)
                : null,
            ErOffentligAktor = erOffentligAktor
        };

        return View(modell);
    }

    /// <summary>
    /// Sender ett bilde til nettleseren, f.eks. /Ressurs/Bilde/3f2a...jpg.
    /// Offentlige aktører får se alle bildene (samme regel som på kartet).
    /// Private aktører får bare se bildene av sine egne ressurser (på detaljsiden).
    /// </summary>
    [HttpGet]
    public IActionResult Bilde(string id)
    {
        // Path.GetFileName fjerner eventuelle mappenavn fra id-en. Da kan ingen
        // lure oss til å sende andre filer, f.eks. med "../../Program.cs".
        var filnavn = Path.GetFileName(id);
        var sti = Path.Combine(_bildemappe, filnavn);

        if (!System.IO.File.Exists(sti))
        {
            return NotFound();
        }

        // Tilgang: offentlig aktør, eller eieren av ressursen bildet hører til.
        var erEgetBilde = _ressursRepository.HentAlle()
            .Any(r => r.RegistrertAv == Brukernavn() && r.Bilder.Contains(filnavn));
        if (!ErOffentligAktor() && !erEgetBilde)
        {
            return Forbid();
        }

        // Finner riktig filtype, så nettleseren vet at det er et bilde.
        var filtype = Path.GetExtension(filnavn).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        return PhysicalFile(sti, filtype);
    }

    // Hjelpere for tilgangssjekkene over. "?." gjør at de også virker i tester uten innlogget bruker.
    private bool ErOffentligAktor() => User?.IsInRole(KartController.RolleSomSerRessurser) == true;

    private string Brukernavn() => User?.Identity?.Name ?? string.Empty;

    /// <summary>
    /// Sjekker at brukeren har lastet opp 1-5 bilder, at de er bildefiler,
    /// og at ingen er for store. Feil legges i ModelState og vises i skjemaet.
    /// </summary>
    private void SjekkBilder(List<IFormFile> bilder)
    {
        if (bilder.Count == 0)
        {
            ModelState.AddModelError(nameof(RessursViewModel.Bilder), "Du må legge ved minst ett bilde av ressursen.");
            return;
        }

        if (bilder.Count > MaksAntallBilder)
        {
            ModelState.AddModelError(nameof(RessursViewModel.Bilder), $"Du kan legge ved maks {MaksAntallBilder} bilder.");
        }

        foreach (var bilde in bilder)
        {
            var filtype = Path.GetExtension(bilde.FileName).ToLowerInvariant();

            if (!TillatteFiltyper.Contains(filtype))
            {
                ModelState.AddModelError(nameof(RessursViewModel.Bilder), $"«{bilde.FileName}» er ikke et bilde. Bruk JPG, PNG eller WEBP.");
            }

            if (bilde.Length > MaksBildestorrelse)
            {
                ModelState.AddModelError(nameof(RessursViewModel.Bilder), $"«{bilde.FileName}» er større enn 5 MB.");
            }
        }
    }

    /// <summary>
    /// Lagrer ett bilde i bildemappen og returnerer filnavnet det fikk.
    /// </summary>
    private async Task<string> LagreBilde(IFormFile bilde)
    {
        // Lager mappen hvis den ikke finnes (gjør ingenting hvis den finnes fra før).
        Directory.CreateDirectory(_bildemappe);

        // Gir filen et nytt, tilfeldig navn (Guid). Da kan to brukere laste opp
        // "bilde.jpg" uten å overskrive hverandre, og ingen kan gjette filnavnet.
        var filnavn = Guid.NewGuid() + Path.GetExtension(bilde.FileName).ToLowerInvariant();
        var sti = Path.Combine(_bildemappe, filnavn);

        // "using" sørger for at filen lukkes når vi er ferdige med å skrive til den.
        using var fil = new FileStream(sti, FileMode.Create);
        await bilde.CopyToAsync(fil);

        return filnavn;
    }
}
