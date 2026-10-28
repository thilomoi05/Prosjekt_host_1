using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Siden "Hva trengs": en oversikt over hvilket utstyr som trengs, og i hvilke områder.
/// Den viser IKKE hvem som har meldt behovet, eksakt plassering eller kontaktinfo,
/// så den kan vises for alle innloggede, også private aktører som vil vite hva de kan bidra med.
/// Adresse: /HvaTrengs
/// </summary>
[Authorize]
public class HvaTrengsController : Controller
{
    private readonly IBehovRepository _behovRepository;

    public HvaTrengsController(IBehovRepository behovRepository)
    {
        _behovRepository = behovRepository;
    }

    public IActionResult Index()
    {
        // Bare behov som venter, gruppert på type. GroupBy lager én gruppe per type.
        var utstyr = _behovRepository.HentAlle()
            .Where(b => b.Status == BehovStatus.Venter)
            .GroupBy(b => b.Type)
            .Select(gruppe => new UtstyrSomTrengs
            {
                Type = gruppe.Key,
                Navn = gruppe.First().VisningsNavn,
                Antall = gruppe.Count(),
                HoyestePrioritet = gruppe.Max(b => b.Prioritet),   // Hoy er størst i enumen
                Omrader = gruppe.Select(b => b.GeografiskOmrade).Distinct().OrderBy(o => o).ToList()
            })
            // Det som haster mest først, deretter det som trengs flest steder.
            .OrderByDescending(u => u.HoyestePrioritet)
            .ThenByDescending(u => u.Antall)
            .ToList();

        return View(utstyr);
    }
}
