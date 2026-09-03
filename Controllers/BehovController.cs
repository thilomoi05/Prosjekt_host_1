using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Håndterer listevisning og registrering av behov.
/// </summary>
[Authorize]
public class BehovController : Controller
{
    private readonly IBehovRepository _behovRepository;

    public BehovController(IBehovRepository behovRepository)
    {
        _behovRepository = behovRepository;
    }

    public IActionResult Index()
    {
        var alleBehov = _behovRepository.HentAlle();
        return View(alleBehov);
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
            Status = BehovStatus.Ny,
            Kontaktperson = modell.Kontaktperson,
            Telefon = modell.Telefon,
            Dato = DateOnly.FromDateTime(DateTime.Now)
        };

        _behovRepository.LeggTil(nyttBehov);

        return RedirectToAction(nameof(Index));
    }
}
