using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Beredskapsportal.Sikkerhet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Håndterer innlogging, registrering, utlogging og profilsiden,
/// der brukeren kan endre navn, e-post og passord.
///
/// Bruker ASP.NET Core Identity (satt opp i Program.cs):
///  - UserManager:   lager brukere, hasher passord og gir roller
///  - SignInManager: sjekker passordet, teller feil forsøk og lager innloggingscookien
/// Vi skriver altså ingen passord- eller cookie-kode selv.
///
/// Anti-forgery-tokenet sjekkes på alle POST-metodene av filteret i Program.cs.
/// POST til LoggInn, Registrer, EndreProfil og EndrePassord har i tillegg en fartsgrense
/// (Sikkerhet/Fartsgrenser.cs), så ingen kan prøve tusenvis av passord eller lage tusenvis av kontoer.
///
/// Brukernavnet kan ikke endres: behov, ressurser og forespørsler er koblet til eieren
/// med brukernavnet (RegistrertAv, ForespurtAv). Én bruker deles av en hel enhet,
/// f.eks. Heimevernet avdeling Kristiansand.
/// </summary>
public class KontoController : Controller
{
    private readonly UserManager<Bruker> _brukerManager;
    private readonly SignInManager<Bruker> _innloggingManager;

    // Adminloggen: innlogginger, feilede forsøk, registreringer osv. (se Logg nederst).
    private readonly IHendelseRepository _hendelseRepository;

    public KontoController(UserManager<Bruker> brukerManager, SignInManager<Bruker> innloggingManager,
        IHendelseRepository hendelseRepository)
    {
        _brukerManager = brukerManager;
        _innloggingManager = innloggingManager;
        _hendelseRepository = hendelseRepository;
    }

    // returnUrl er siden brukeren prøvde å åpne før de ble sendt til innlogging,
    // f.eks. /Kart. Den legges i skjemaet, så brukeren kommer tilbake dit etterpå.
    [HttpGet]
    public IActionResult LoggInn(string? returnUrl = null)
    {
        return View(new LoggInnViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [EnableRateLimiting(Fartsgrenser.Innlogging)]
    public async Task<IActionResult> LoggInn(LoggInnViewModel modell)
    {
        if (!ModelState.IsValid)
        {
            return View(modell);
        }

        var bruker = await _brukerManager.FindByNameAsync(modell.Brukernavn);

        if (bruker is not null)
        {
            // Sjekker passordet mot hashen i databasen. lockoutOnFailure: true teller feil
            // forsøk, og låser kontoen i 5 minutter etter 5 feil på rad (se Program.cs).
            var resultat = await _innloggingManager.CheckPasswordSignInAsync(bruker, modell.Passord, lockoutOnFailure: true);

            if (resultat.IsLockedOut)
            {
                Logg(Kontohendelser.KontoLast, bruker.UserName);
                ModelState.AddModelError(string.Empty, "For mange feil forsøk. Kontoen er låst i 5 minutter.");
                return View(modell);
            }

            if (resultat.Succeeded)
            {
                // Offentlige aktører som ikke er godkjent ennå, slipper ikke inn. Sjekkes etter
                // passordet, så meldingen bare vises til den som faktisk eier kontoen.
                if (bruker.VenterPaGodkjenning)
                {
                    Logg(Kontohendelser.VenterPaGodkjenning, bruker.UserName);
                    ModelState.AddModelError(string.Empty, "Kontoen din venter på godkjenning fra en administrator.");
                    return View(modell);
                }

                // Lager innloggingscookien. isPersistent: false betyr at den slettes når nettleseren lukkes.
                await _innloggingManager.SignInAsync(bruker, isPersistent: false);
                Logg(Kontohendelser.LoggetInn, bruker.UserName);
                return TilSidenBrukerKomFra(modell.ReturnUrl);
            }

            Logg(Kontohendelser.FeilPassord, bruker.UserName);
        }
        else
        {
            // Brukernavnet finnes ikke. Det som ble skrevet inn, lagres ikke (se Kontohendelser.UkjentBruker).
            Logg(Kontohendelser.UkjentBrukernavn, null);
        }

        // Samme melding for ukjent brukernavn og feil passord, så ingen kan bruke
        // innloggingen til å finne ut hvilke brukernavn som finnes.
        ModelState.AddModelError(string.Empty, "Feil brukernavn eller passord.");
        return View(modell);
    }

    [HttpGet]
    public IActionResult Registrer(BrukerRolle rolle = BrukerRolle.OffentligAktor)
    {
        // Rollen kan komme fra lenken, f.eks. /Konto/Registrer?rolle=PrivatAktor,
        // så riktig rolle er valgt fra start (brukes av knappene på forsiden).
        return View(new RegistrerBrukerViewModel { Rolle = rolle });
    }

    [HttpPost]
    [EnableRateLimiting(Fartsgrenser.Innlogging)]
    public async Task<IActionResult> Registrer(RegistrerBrukerViewModel modell)
    {
        // Sikkerhetsnett: bare de to rollene i BrukerRolle kan velges. Admin finnes ikke der,
        // og et tall som ikke er en gyldig rolle (sendt inn med vilje) avvises her.
        if (!Enum.IsDefined(modell.Rolle))
        {
            ModelState.AddModelError(nameof(modell.Rolle), "Velg en gyldig rolle.");
        }

        if (!ModelState.IsValid)
        {
            return View(modell);
        }

        var erOffentligAktor = modell.Rolle == BrukerRolle.OffentligAktor;
        var nyBruker = new Bruker
        {
            UserName = modell.Brukernavn,
            Email = modell.Epost,
            FulltNavn = modell.FulltNavn,
            // Offentlige aktører må godkjennes av en administrator før de kan logge inn.
            VenterPaGodkjenning = erOffentligAktor
        };

        // CreateAsync sjekker passordreglene og om brukernavn/e-post er ledig,
        // hasher passordet og lagrer brukeren.
        var resultat = await _brukerManager.CreateAsync(nyBruker, modell.Passord);
        if (!resultat.Succeeded)
        {
            // Feilmeldingene er på norsk (se Sikkerhet/NorskeIdentityFeilmeldinger.cs).
            foreach (var feil in resultat.Errors)
            {
                ModelState.AddModelError(string.Empty, feil.Description);
            }
            return View(modell);
        }

        if (erOffentligAktor)
        {
            Logg(Kontohendelser.RegistrertOffentlig, nyBruker.UserName);

            // Rollen OffentligAktor gis først når en administrator godkjenner kontoen
            // (kommer sammen med siden for Admin). Brukeren logges derfor ikke inn nå.
            TempData["Melding"] = "Takk! Kontoen er opprettet, men må godkjennes av en administrator før du kan logge inn.";
            return RedirectToAction("Index", "Home");
        }

        await _brukerManager.AddToRoleAsync(nyBruker, Roller.PrivatAktor);
        await _innloggingManager.SignInAsync(nyBruker, isPersistent: false);
        Logg(Kontohendelser.RegistrertPrivat, nyBruker.UserName);
        return RedirectToAction("Index", "Oversikt");
    }

    [HttpPost]
    public async Task<IActionResult> LoggUt()
    {
        // Logges før utloggingen, mens vi fortsatt vet hvem brukeren er.
        if (User?.Identity?.IsAuthenticated == true)
        {
            Logg(Kontohendelser.LoggetUt, User.Identity.Name);
        }

        await _innloggingManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profil()
    {
        // GetUserAsync finner brukeren som er logget inn, ut fra innloggingscookien.
        var bruker = await _brukerManager.GetUserAsync(User);
        if (bruker is null)
        {
            return await LoggUtSlettetBruker();
        }

        var modell = new ProfilViewModel
        {
            FulltNavn = bruker.FulltNavn,
            Epost = bruker.Email ?? string.Empty,
            Brukernavn = bruker.UserName ?? string.Empty,
            RolleTekst = await RolleTekst(bruker)
        };

        return View(modell);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> EndreProfil()
    {
        var bruker = await _brukerManager.GetUserAsync(User);
        if (bruker is null)
        {
            return await LoggUtSlettetBruker();
        }

        // Skjemaet fylles ut med det som er lagret nå.
        return View(new EndreProfilViewModel
        {
            FulltNavn = bruker.FulltNavn,
            Epost = bruker.Email ?? string.Empty
        });
    }

    /// <summary>
    /// Lagrer nytt navn og/eller ny e-post for brukeren som er logget inn.
    /// Brukeren hentes alltid fra innloggingscookien, aldri fra skjemaet,
    /// så ingen kan endre profilen til en annen bruker.
    /// </summary>
    [Authorize]
    [HttpPost]
    [EnableRateLimiting(Fartsgrenser.Innlogging)]
    public async Task<IActionResult> EndreProfil(EndreProfilViewModel modell)
    {
        var bruker = await _brukerManager.GetUserAsync(User);
        if (bruker is null)
        {
            return await LoggUtSlettetBruker();
        }

        if (!ModelState.IsValid)
        {
            return View(modell);
        }

        var nyEpost = modell.Epost.Trim();
        // Store og små bokstaver teller ikke: Ola@uia.no og ola@uia.no er samme e-post.
        var epostErEndret = !string.Equals(nyEpost, bruker.Email, StringComparison.OrdinalIgnoreCase);

        // Ny e-post krever passordet. Navnet kan endres uten.
        if (epostErEndret)
        {
            if (string.IsNullOrEmpty(modell.NavarendePassord))
            {
                ModelState.AddModelError(nameof(modell.NavarendePassord), "Skriv inn nåværende passord for å endre e-posten.");
                return View(modell);
            }

            if (!await _brukerManager.CheckPasswordAsync(bruker, modell.NavarendePassord))
            {
                ModelState.AddModelError(nameof(modell.NavarendePassord), "Nåværende passord er feil.");
                return View(modell);
            }
        }

        bruker.FulltNavn = modell.FulltNavn.Trim();

        // SetEmailAsync sjekker at e-posten er gyldig og ikke brukes av en annen bruker
        // (RequireUniqueEmail i Program.cs), og lagrer hele brukeren, også det nye navnet.
        // Er bare navnet endret, holder det med UpdateAsync.
        var resultat = epostErEndret
            ? await _brukerManager.SetEmailAsync(bruker, nyEpost)
            : await _brukerManager.UpdateAsync(bruker);

        if (!resultat.Succeeded)
        {
            // Feilmeldingene er på norsk (se Sikkerhet/NorskeIdentityFeilmeldinger.cs).
            foreach (var feil in resultat.Errors)
            {
                ModelState.AddModelError(string.Empty, feil.Description);
            }
            return View(modell);
        }

        // Navnet ligger i innloggingscookien (BrukerClaimsFabrikk), og menyen viser det derfra.
        // RefreshSignInAsync lager cookien på nytt, så det nye navnet vises med en gang.
        await _innloggingManager.RefreshSignInAsync(bruker);

        TempData["Melding"] = "Profilen er oppdatert.";
        return RedirectToAction(nameof(Profil));
    }

    [Authorize]
    [HttpGet]
    public IActionResult EndrePassord()
    {
        return View(new EndrePassordViewModel());
    }

    /// <summary>Bytter passordet til brukeren som er logget inn.</summary>
    [Authorize]
    [HttpPost]
    [EnableRateLimiting(Fartsgrenser.Innlogging)]
    public async Task<IActionResult> EndrePassord(EndrePassordViewModel modell)
    {
        var bruker = await _brukerManager.GetUserAsync(User);
        if (bruker is null)
        {
            return await LoggUtSlettetBruker();
        }

        if (!ModelState.IsValid)
        {
            return View(modell);
        }

        // ChangePasswordAsync sjekker det nåværende passordet og passordreglene,
        // og lagrer bare hashen av det nye passordet.
        var resultat = await _brukerManager.ChangePasswordAsync(bruker, modell.NavarendePassord, modell.NyttPassord);
        if (!resultat.Succeeded)
        {
            foreach (var feil in resultat.Errors)
            {
                ModelState.AddModelError(string.Empty, feil.Description);
            }
            return View(modell);
        }

        // Nytt passord gir brukeren et nytt "sikkerhetsstempel". Andre som er logget inn
        // på samme bruker (samme enhet, en annen PC), blir da logget ut innen 30 minutter.
        // RefreshSignInAsync gir denne nettleseren en ny cookie, så den som byttet passordet
        // forblir innlogget.
        await _innloggingManager.RefreshSignInAsync(bruker);
        Logg(Kontohendelser.PassordEndret, bruker.UserName);

        TempData["Melding"] = "Passordet er endret.";
        return RedirectToAction(nameof(Profil));
    }

    /// <summary>
    /// Skriver én linje i adminloggen (/Admin/Logg). Kontohendelser gjelder ikke et behov
    /// eller en ressurs, så BehovId og RessursId står tomme. Det er slik loggen ser at
    /// hendelsen hører til kategorien Konto (se LoggRad.Kategori).
    /// </summary>
    private void Logg(string beskrivelse, string? brukernavn)
    {
        _hendelseRepository.LeggTil(new Hendelse
        {
            Tidspunkt = DateTime.Now,
            UtfortAv = string.IsNullOrEmpty(brukernavn) ? Kontohendelser.UkjentBruker : brukernavn,
            Beskrivelse = beskrivelse
        });
    }

    /// <summary>
    /// Brukeren er slettet fra databasen mens de var logget inn (f.eks. avvist av Admin).
    /// Da logges de ut og sendes til innloggingen.
    /// </summary>
    private async Task<IActionResult> LoggUtSlettetBruker()
    {
        await _innloggingManager.SignOutAsync();
        return RedirectToAction(nameof(LoggInn));
    }

    /// <summary>Teksten for rollen som vises på profilsiden.</summary>
    private async Task<string> RolleTekst(Bruker bruker)
    {
        var roller = await _brukerManager.GetRolesAsync(bruker);
        if (roller.Contains(Roller.Admin))
        {
            return "Administrator";
        }
        return roller.Contains(Roller.OffentligAktor) ? "Offentlig aktør" : "Privat aktør";
    }

    /// <summary>
    /// Sender brukeren tilbake til siden de prøvde å åpne, men bare hvis den ligger på
    /// vår egen side. Url.IsLocalUrl hindrer at en lenke som
    /// /Konto/LoggInn?returnUrl=https://falsk-side.no sender brukeren videre til en annen side
    /// etter innlogging ("open redirect").
    /// </summary>
    private IActionResult TilSidenBrukerKomFra(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        return RedirectToAction("Index", "Oversikt");
    }
}
