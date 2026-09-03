using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Håndterer registrering av ressurser som private aktører/bedrifter tilbyr.
/// </summary>
[Authorize]
public class RessursController : Controller
{
    private readonly IRessursRepository _ressursRepository;

    public RessursController(IRessursRepository ressursRepository)
    {
        _ressursRepository = ressursRepository;
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
        return View(new RessursViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Registrer(RessursViewModel modell)
    {
        if (!ModelState.IsValid)
        {
            return View(modell);
        }

        var nyRessurs = new Ressurs
        {
            Type = modell.Type!.Value,
            BeskrivelseAvKapasitet = modell.BeskrivelseAvKapasitet,
            GeografiskBase = modell.GeografiskBase,
            TilgjengeligFra = modell.TilgjengeligFra,
            TilgjengeligTil = modell.TilgjengeligTil,
            Kontaktperson = modell.Kontaktperson,
            Telefon = modell.Telefon
        };

        _ressursRepository.LeggTil(nyRessurs);

        return RedirectToAction("Index", "Oversikt");
    }
}
