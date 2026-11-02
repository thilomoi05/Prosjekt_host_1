using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Viser alle tildelinger: hvilke ressurser som er satt inn på hvilke behov.
/// Oversikten viser bare de fem siste, og lenker hit ("Se alle tildelinger").
/// Bare offentlige aktører har tilgang, samme regel som for behov og kart.
/// </summary>
[Authorize(Roles = nameof(BrukerRolle.OffentligAktor))]
public class TildelingController : Controller
{
    private readonly IBehovRepository _behovRepository;
    private readonly IRessursRepository _ressursRepository;
    private readonly ITildelingRepository _tildelingRepository;

    public TildelingController(IBehovRepository behovRepository, IRessursRepository ressursRepository,
        ITildelingRepository tildelingRepository)
    {
        _behovRepository = behovRepository;
        _ressursRepository = ressursRepository;
        _tildelingRepository = tildelingRepository;
    }

    public IActionResult Index()
    {
        // ToDictionary gjør det raskt å slå opp et behov eller en ressurs på id.
        var behovEtterId = _behovRepository.HentAlle().ToDictionary(b => b.Id);
        var ressurserEtterId = _ressursRepository.HentAlle().ToDictionary(r => r.Id);

        // Én rad per tildeling, nyeste først. Tildelinger der behovet eller ressursen
        // ikke finnes lenger, hoppes over (Where), så siden ikke krasjer.
        var rader = _tildelingRepository.HentAlle()
            .Where(t => behovEtterId.ContainsKey(t.BehovId) && ressurserEtterId.ContainsKey(t.RessursId))
            .OrderByDescending(t => t.Tidspunkt)
            .Select(t => new TildelingRad
            {
                Behov = behovEtterId[t.BehovId],
                Ressurs = ressurserEtterId[t.RessursId],
                Tildeling = t
            })
            .ToList();

        return View(rader);
    }
}
