using System.Security.Claims;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Håndterer innlogging, registrering og utlogging av brukere.
/// Selve autentiseringen er cookie-basert (satt opp i Program.cs).
/// </summary>
public class KontoController : Controller
{
    private readonly IBrukerRepository _brukerRepository;

    // Repository-et injiseres av ASP.NET Core sin innebygde DI-container
    // (registrert i Program.cs), i stedet for at Controller-en oppretter det selv.
    // Dette gjør klassen enklere å teste og å bytte datalagring for.
    public KontoController(IBrukerRepository brukerRepository)
    {
        _brukerRepository = brukerRepository;
    }

    [HttpGet]
    public IActionResult LoggInn()
    {
        return View(new LoggInnViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoggInn(LoggInnViewModel modell)
    {
        if (!ModelState.IsValid)
        {
            return View(modell);
        }

        var bruker = _brukerRepository.FinnVedBrukernavn(modell.Brukernavn);
        var passordStemmer = bruker is not null
            && PassordHasher.VerifiserPassord(modell.Passord, bruker.PassordHash, bruker.PassordSalt);

        if (!passordStemmer || bruker is null)
        {
            ModelState.AddModelError(string.Empty, "Feil brukernavn eller passord.");
            return View(modell);
        }

        await LoggBrukerInn(bruker);
        return RedirectToAction("Index", "Oversikt");
    }

    [HttpGet]
    public IActionResult Registrer()
    {
        return View(new RegistrerBrukerViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrer(RegistrerBrukerViewModel modell)
    {
        if (_brukerRepository.BrukernavnErOpptatt(modell.Brukernavn))
        {
            ModelState.AddModelError(nameof(modell.Brukernavn), "Brukernavnet er allerede i bruk.");
        }

        if (!ModelState.IsValid)
        {
            return View(modell);
        }

        var (hash, salt) = PassordHasher.HashPassord(modell.Passord);
        var nyBruker = new Bruker
        {
            FulltNavn = modell.FulltNavn,
            Epost = modell.Epost,
            Brukernavn = modell.Brukernavn,
            PassordHash = hash,
            PassordSalt = salt,
            Rolle = modell.Rolle
        };

        _brukerRepository.LeggTil(nyBruker);

        await LoggBrukerInn(nyBruker);
        return RedirectToAction("Index", "Oversikt");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoggUt()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Oppretter en signert innloggingscookie for den gitte brukeren.
    /// Navn og rolle legges inn som "claims" slik at resten av applikasjonen
    /// (f.eks. Layout-siden) kan lese dem via User.Identity uten et nytt oppslag.
    /// </summary>
    private async Task LoggBrukerInn(Bruker bruker)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, bruker.Brukernavn),
            new(ClaimTypes.GivenName, bruker.FulltNavn),
            new(ClaimTypes.Role, bruker.Rolle.ToString())
        };

        var identitet = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identitet));
    }
}
