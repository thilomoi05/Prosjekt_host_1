using System.Security.Claims;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Beredskapsportal.Sikkerhet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Håndterer registrering, detaljsiden, endring og tilbaketrekking av ressurser som bedrifter,
/// entreprenører og privatpersoner tilbyr, inkludert opplasting og visning av bilder.
///
/// Hvem som ser en ressurs (KanSe under):
///  - Administratorer ser alle ressurser, også de som venter på godkjenning.
///  - Eieren ser alltid sine egne.
///  - Offentlige aktører ser bare godkjente ressurser.
/// Nye ressurser venter på godkjenning fra en administrator (se AdminController).
/// </summary>
[Authorize]
public class RessursController : Controller
{
    // Regler for bildeopplasting. Samlet her, så de er lette å finne og endre.
    private const int MaksAntallBilder = 5;
    private const long MaksBildestorrelse = 5 * 1024 * 1024;   // 5 MB per bilde
    private static readonly string[] TillatteFiltyper = { ".jpg", ".jpeg", ".png", ".webp" };

    // Største skjema vi tar imot: 5 bilder à 5 MB pluss tekstfeltene. Større forespørsler
    // avvises av serveren før controlleren kjører, så ingen kan fylle opp minnet eller disken.
    private const long MaksSkjemastorrelse = 26 * 1024 * 1024;

    private readonly IRessursRepository _ressursRepository;
    private readonly IBehovRepository _behovRepository;
    private readonly ITildelingRepository _tildelingRepository;
    private readonly TildelingTjeneste _tildelingTjeneste;

    // Mappen der bildene lagres. Den ligger bevisst UTENFOR wwwroot:
    // alt i wwwroot kan alle laste ned, men bildene skal bare autoriserte se.
    private readonly string _bildemappe;

    // IWebHostEnvironment gir oss blant annet stien til prosjektmappen (ContentRootPath).
    public RessursController(IRessursRepository ressursRepository, IBehovRepository behovRepository,
        ITildelingRepository tildelingRepository, TildelingTjeneste tildelingTjeneste, IWebHostEnvironment miljo)
    {
        _ressursRepository = ressursRepository;
        _behovRepository = behovRepository;
        _tildelingRepository = tildelingRepository;
        _tildelingTjeneste = tildelingTjeneste;
        _bildemappe = Path.Combine(miljo.ContentRootPath, "Opplastinger", "bilder");
    }

    // Lista over ressurser: "Ressurser" for offentlige aktører og admin, "Mine ressurser" for private.
    // Viser bare det brukeren har lov til å se, med samme regel som detaljsiden (KanSe):
    // administratorer ser alle, offentlige aktører ser godkjente og sine egne, private bare sine egne.
    // Hvilke kolonner hver rolle ser, står i Views/Ressurs/Index.cshtml.
    public IActionResult Index()
    {
        // Id-ene til ressursene som er satt inn på et behov, så viewet kan vise dem som tildelt.
        var tildelteRessursIder = _tildelingRepository.HentAlle()
            .Select(t => t.RessursId)
            .ToHashSet();

        // Én rad per ressurs, sortert på status så lista ikke ser rotete ut: tilgjengelige øverst,
        // så tildelte, så de som venter på godkjenning, og avviste nederst. Innenfor hver status
        // står de nyeste først. tabellfilter.js beholder statusrekkefølgen når den sorterer på dato
        // (data-gruppe i viewet), og private aktører har ingen datokolonne, så der står denne rekkefølgen.
        var rader = _ressursRepository.HentAlle()
            .Where(KanSe)
            .Select(r => new RessursRad { Ressurs = r, ErTildelt = tildelteRessursIder.Contains(r.Id) })
            .OrderBy(rad => Statusrekkefolge(rad))
            .ThenByDescending(rad => rad.Ressurs.Id)
            .ToList();

        return View(rader);
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
    // Anti-forgery-tokenet sjekkes av filteret i Program.cs (gjelder alle POST-metoder).
    [HttpPost]
    [RequestSizeLimit(MaksSkjemastorrelse)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaksSkjemastorrelse)]
    public async Task<IActionResult> Registrer(RessursViewModel modell)
    {
        // Sjekker bildene i tillegg til de vanlige [Required]-reglene i view-modellen.
        modell.Bilder ??= new List<IFormFile>();
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

        // Ressurser fra en administrator godkjennes med en gang. Alle andre venter på godkjenning,
        // så falske innsendinger ikke vises før en administrator har sett dem.
        var erAdmin = User?.IsInRole(Roller.Admin) == true;

        var nyRessurs = new Ressurs
        {
            Type = modell.Type!.Value,
            Status = erAdmin ? RessursStatus.Godkjent : RessursStatus.Venter,
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
        _tildelingTjeneste.Logg(erAdmin ? "Ressursen ble registrert og godkjent av en administrator" : "Ressursen ble sendt inn",
            Brukernavn(), ressursId: nyRessurs.Id);

        // Meldingen vises øverst på neste side (se _Layout.cshtml). TempData husker den
        // akkurat så lenge at den overlever omdirigeringen til oversikten.
        TempData["Melding"] = erAdmin
            ? "Ressursen er registrert."
            : "Takk! Ressursen er sendt inn, og blir synlig når en administrator har godkjent den.";

        return RedirectToAction("Index", "Oversikt");
    }

    /// <summary>
    /// Detaljsiden for én ressurs, f.eks. /Ressurs/Detaljer/7.
    /// Hvem som får se den, står i KanSe. Andre sendes til oversikten
    /// (Forbid() + AccessDeniedPath i Program.cs).
    /// </summary>
    [HttpGet]
    public IActionResult Detaljer(int id)
    {
        var ressurs = _ressursRepository.HentMedId(id);
        if (ressurs is null)
        {
            return NotFound();   // ukjent id: "Fant ikke siden"
        }

        if (!KanSe(ressurs))
        {
            return Forbid();
        }

        var erOffentligAktor = ErOffentligAktor();

        // Er ressursen satt inn på et behov? I så fall finnes det en Tildeling med denne ressursen.
        var tildeling = _tildelingRepository.HentAlle().FirstOrDefault(t => t.RessursId == id);

        var kanForesporres = erOffentligAktor && tildeling is null && ressurs.Status == RessursStatus.Godkjent;

        var modell = new RessursDetaljerViewModel
        {
            Ressurs = ressurs,
            ErTildelt = tildeling is not null,
            // Bare offentlige aktører får vite hvilket behov ressursen er tildelt.
            TildeltBehov = tildeling is not null && erOffentligAktor
                ? _behovRepository.HentMedId(tildeling.BehovId)
                : null,
            ErOffentligAktor = erOffentligAktor,
            KanTrekkeTilbake = ErEierEllerAdmin(ressurs),
            Historikk = _tildelingTjeneste.HistorikkForRessurs(id),
            // En offentlig aktør kan forespørre en ledig, godkjent ressurs til et av sine egne behov.
            KanForesporres = kanForesporres,
            MineBehov = kanForesporres
                ? _behovRepository.HentAlle().Where(b => b.RegistrertAv == Brukernavn()).ToList()
                : new List<Behov>()
        };

        return View(modell);
    }

    /// <summary>
    /// Skjemaet for å endre en ressurs, f.eks. /Ressurs/Rediger/7.
    /// Bare eieren og administratorer. Feltene er fylt inn med det som er lagret nå.
    /// </summary>
    [HttpGet]
    public IActionResult Rediger(int id)
    {
        var ressurs = _ressursRepository.HentMedId(id);
        if (ressurs is null)
        {
            return NotFound();
        }
        if (!ErEierEllerAdmin(ressurs))
        {
            return Forbid();
        }

        var modell = new RedigerRessursViewModel
        {
            Id = ressurs.Id,
            Type = ressurs.Type,
            BeskrivelseAvKapasitet = ressurs.BeskrivelseAvKapasitet,
            Adresse = ressurs.Adresse,
            Tilgangsbeskrivelse = ressurs.Tilgangsbeskrivelse,
            TilgjengeligFra = ressurs.TilgjengeligFra,
            TilgjengeligTil = ressurs.TilgjengeligTil,
            Kontaktperson = ressurs.Kontaktperson,
            Telefon = ressurs.Telefon,
            Breddegrad = ressurs.Breddegrad,
            Lengdegrad = ressurs.Lengdegrad,
            EksisterendeBilder = ressurs.Bilder.ToList(),
            Status = ressurs.Status
        };
        return View(modell);
    }

    /// <summary>
    /// Lagrer endringene fra skjemaet. Tilgangen sjekkes på nytt her, fordi en POST
    /// kan sendes uten å gå via siden over (f.eks. med et eget verktøy).
    ///
    /// Regler for status etter en endring:
    ///  - Administratorer: statusen er uendret.
    ///  - Eieren: ressursen må godkjennes på nytt (Venter), så ingen kan bytte ut innholdet
    ///    i en godkjent ressurs uten at en administrator ser det. Unntak: er ressursen
    ///    allerede tildelt et behov, beholdes statusen, så tildelingen ikke blir ugyldig.
    ///    Endringen står uansett i historikken.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(MaksSkjemastorrelse)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaksSkjemastorrelse)]
    public async Task<IActionResult> Rediger(int id, RedigerRessursViewModel modell)
    {
        var ressurs = _ressursRepository.HentMedId(id);
        if (ressurs is null)
        {
            return NotFound();
        }
        if (!ErEierEllerAdmin(ressurs))
        {
            return Forbid();
        }

        // Id og bildene som vises, hentes alltid fra databasen, ikke fra skjemaet.
        modell.Id = ressurs.Id;
        modell.EksisterendeBilder = ressurs.Bilder.ToList();
        modell.Status = ressurs.Status;

        // Nye bilder er frivillig ved endring. Sjekkes bare hvis brukeren har valgt noen.
        modell.Bilder ??= new List<IFormFile>();
        if (modell.Bilder.Count > 0)
        {
            SjekkBilder(modell.Bilder);
        }

        if (modell.Breddegrad is not null && modell.Lengdegrad is null)
        {
            ModelState.AddModelError(nameof(modell.Breddegrad), "Du må klikke i kartet for å markere plasseringen.");
        }

        if (!ModelState.IsValid)
        {
            return View(modell);
        }

        // Nye bilder erstatter de gamle. De gamle slettes fra disken etter at endringen er lagret.
        var gamleBilder = new List<string>();
        var bilder = ressurs.Bilder.ToList();
        if (modell.Bilder.Count > 0)
        {
            gamleBilder = bilder;
            bilder = new List<string>();
            foreach (var bilde in modell.Bilder)
            {
                bilder.Add(await LagreBilde(bilde));
            }
        }

        var erAdmin = User?.IsInRole(Roller.Admin) == true;
        var erTildelt = _tildelingRepository.HentAlle().Any(t => t.RessursId == id);
        var maGodkjennesPaNytt = !erAdmin && !erTildelt;

        var endret = new Ressurs
        {
            Id = ressurs.Id,
            Type = modell.Type!.Value,
            Status = maGodkjennesPaNytt ? RessursStatus.Venter : ressurs.Status,
            Avvisningsgrunn = maGodkjennesPaNytt ? null : ressurs.Avvisningsgrunn,
            BeskrivelseAvKapasitet = modell.BeskrivelseAvKapasitet,
            Adresse = modell.Adresse,
            Tilgangsbeskrivelse = modell.Tilgangsbeskrivelse,
            Bilder = bilder,
            TilgjengeligFra = modell.TilgjengeligFra,
            TilgjengeligTil = modell.TilgjengeligTil,
            Kontaktperson = modell.Kontaktperson,
            Telefon = modell.Telefon,
            RegistrertAv = ressurs.RegistrertAv,   // eieren kan aldri endres
            Breddegrad = modell.Breddegrad!.Value,
            Lengdegrad = modell.Lengdegrad!.Value
        };

        _ressursRepository.Oppdater(endret);
        SlettBilder(gamleBilder);

        _tildelingTjeneste.Logg(maGodkjennesPaNytt
                ? "Ressursen ble endret og venter på ny godkjenning"
                : "Ressursen ble endret",
            Brukernavn(), ressursId: id);

        TempData["Melding"] = maGodkjennesPaNytt
            ? "Endringene er lagret. Ressursen blir synlig igjen når en administrator har godkjent den."
            : "Endringene er lagret.";

        return RedirectToAction(nameof(Detaljer), new { id });
    }

    /// <summary>
    /// "Er du sikker?"-siden før en ressurs trekkes tilbake, f.eks. /Ressurs/TrekkTilbake/7.
    /// Bare eieren og administratorer.
    /// </summary>
    [HttpGet]
    public IActionResult TrekkTilbake(int id)
    {
        var ressurs = _ressursRepository.HentMedId(id);
        if (ressurs is null)
        {
            return NotFound();
        }
        if (!ErEierEllerAdmin(ressurs))
        {
            return Forbid();
        }

        var modell = new RessursDetaljerViewModel
        {
            Ressurs = ressurs,
            ErTildelt = _tildelingRepository.HentAlle().Any(t => t.RessursId == id)
        };
        return View(modell);
    }

    /// <summary>
    /// Trekker tilbake (sletter) ressursen og bildene av den. Var den tildelt et behov,
    /// fjernes tildelingen (se TildelingTjeneste).
    /// "ActionName" gjør at denne metoden svarer på POST til samme adresse som siden over.
    /// </summary>
    [HttpPost]
    [ActionName(nameof(TrekkTilbake))]
    public IActionResult TrekkTilbakeBekreftet(int id)
    {
        var ressurs = _ressursRepository.HentMedId(id);
        if (ressurs is null)
        {
            return NotFound();
        }
        if (!ErEierEllerAdmin(ressurs))
        {
            return Forbid();
        }

        _tildelingTjeneste.TrekkTilbakeRessurs(id, Brukernavn());

        // Bildene slettes også fra disken, siden ingen kan se dem lenger.
        SlettBilder(ressurs.Bilder);

        TempData["Melding"] = "Ressursen er trukket tilbake.";
        return RedirectToAction("Index", "Oversikt");
    }

    /// <summary>
    /// Sender ett bilde til nettleseren, f.eks. /Ressurs/Bilde/3f2a...jpg.
    /// Samme regel som for detaljsiden: den som får se ressursen, får se bildene (KanSe).
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

        // Finner ressursen bildet hører til, og sjekker at brukeren får se den.
        var ressurs = _ressursRepository.HentAlle().FirstOrDefault(r => r.Bilder.Contains(filnavn));
        if (ressurs is null || !KanSe(ressurs))
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

    // Plassen til en ressurs i lista: 0 tilgjengelig, 1 tildelt, 2 venter på godkjenning, 3 avvist.
    // Samme rekkefølge som valgene i statusfilteret i Views/Ressurs/Index.cshtml.
    private static int Statusrekkefolge(RessursRad rad) => rad.Ressurs.Status switch
    {
        RessursStatus.Avvist => 3,
        RessursStatus.Venter => 2,
        _ when rad.ErTildelt => 1,
        _ => 0
    };

    // Hjelpere for tilgangssjekkene over. "?." gjør at de også virker i tester uten innlogget bruker.
    private bool ErOffentligAktor() => User?.IsInRole(Roller.OffentligAktor) == true;

    private string Brukernavn() => User?.Identity?.Name ?? string.Empty;

    private bool ErEierEllerAdmin(Ressurs ressurs) =>
        User?.IsInRole(Roller.Admin) == true
        || (!string.IsNullOrEmpty(ressurs.RegistrertAv) && ressurs.RegistrertAv == Brukernavn());

    /// <summary>
    /// Hvem som får se en ressurs: administratorer og eieren alltid,
    /// offentlige aktører bare når den er godkjent.
    /// </summary>
    private bool KanSe(Ressurs ressurs) =>
        ErEierEllerAdmin(ressurs) || (ErOffentligAktor() && ressurs.Status == RessursStatus.Godkjent);

    /// <summary>
    /// Sjekker at brukeren har lastet opp 1-5 bilder, at de er bildefiler (både endelse
    /// og innhold, se Sikkerhet/Bildesjekk.cs), og at ingen er for store. Feil legges i ModelState og vises i skjemaet.
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
            else
            {
                // Endelsen er riktig, men er innholdet et bilde? Et program kan hete "bilde.jpg".
                // Bildesjekk leser de første bytene i filen og sjekker at de passer med endelsen.
                using var innhold = bilde.OpenReadStream();
                if (!Bildesjekk.InnholdPasserMedFiltype(innhold, filtype))
                {
                    ModelState.AddModelError(nameof(RessursViewModel.Bilder), $"«{bilde.FileName}» er ikke et gyldig {filtype.TrimStart('.').ToUpperInvariant()}-bilde.");
                }
            }

            if (bilde.Length > MaksBildestorrelse)
            {
                ModelState.AddModelError(nameof(RessursViewModel.Bilder), $"«{bilde.FileName}» er større enn 5 MB.");
            }
        }
    }

    /// <summary>
    /// Sletter bildefilene fra disken. Brukes når en ressurs trekkes tilbake,
    /// og når eieren erstatter bildene. Path.GetFileName hindrer at noe utenfor bildemappen slettes.
    /// </summary>
    private void SlettBilder(IEnumerable<string> filnavnListe)
    {
        foreach (var filnavn in filnavnListe)
        {
            var sti = Path.Combine(_bildemappe, Path.GetFileName(filnavn));
            if (System.IO.File.Exists(sti))
            {
                System.IO.File.Delete(sti);
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
