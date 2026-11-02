using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Håndterer innlogging, registrering, utlogging og profilsiden.
///
/// Bruker ASP.NET Core Identity (satt opp i Program.cs):
///  - UserManager:   lager brukere, hasher passord og gir roller
///  - SignInManager: sjekker passordet, teller feil forsøk og lager innloggingscookien
/// Vi skriver altså ingen passord- eller cookie-kode selv.
///
/// Anti-forgery-tokenet sjekkes på alle POST-metodene av filteret i Program.cs.
/// </summary>
public class KontoController : Controller
{
    private readonly UserManager<Bruker> _brukerManager;
    private readonly SignInManager<Bruker> _innloggingManager;

    public KontoController(UserManager<Bruker> brukerManager, SignInManager<Bruker> innloggingManager)
    {
        _brukerManager = brukerManager;
        _innloggingManager = innloggingManager;
    }

    // returnUrl er siden brukeren prøvde å åpne før de ble sendt til innlogging,
    // f.eks. /Kart. Den legges i skjemaet, så brukeren kommer tilbake dit etterpå.
    [HttpGet]
    public IActionResult LoggInn(string? returnUrl = null)
    {
        return View(new LoggInnViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
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
                ModelState.AddModelError(string.Empty, "For mange feil forsøk. Kontoen er låst i 5 minutter.");
                return View(modell);
            }

            if (resultat.Succeeded)
            {
                // Offentlige aktører som ikke er godkjent ennå, slipper ikke inn. Sjekkes etter
                // passordet, så meldingen bare vises til den som faktisk eier kontoen.
                if (bruker.VenterPaGodkjenning)
                {
                    ModelState.AddModelError(string.Empty, "Kontoen din venter på godkjenning fra en administrator.");
                    return View(modell);
                }

                // Lager innloggingscookien. isPersistent: false betyr at den slettes når nettleseren lukkes.
                await _innloggingManager.SignInAsync(bruker, isPersistent: false);
                return TilSidenBrukerKomFra(modell.ReturnUrl);
            }
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
            // Rollen OffentligAktor gis først når en administrator godkjenner kontoen
            // (kommer sammen med siden for Admin). Brukeren logges derfor ikke inn nå.
            TempData["Melding"] = "Takk! Kontoen er opprettet, men må godkjennes av en administrator før du kan logge inn.";
            return RedirectToAction("Index", "Home");
        }

        await _brukerManager.AddToRoleAsync(nyBruker, Roller.PrivatAktor);
        await _innloggingManager.SignInAsync(nyBruker, isPersistent: false);
        return RedirectToAction("Index", "Oversikt");
    }

    [HttpPost]
    public async Task<IActionResult> LoggUt()
    {
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
            // Brukeren er slettet fra databasen mens de var logget inn.
            await _innloggingManager.SignOutAsync();
            return RedirectToAction(nameof(LoggInn));
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
