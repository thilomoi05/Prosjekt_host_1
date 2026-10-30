using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Controllers;

/// <summary>
/// Viser dashbordet innloggede brukere møter.
///  - Offentlige aktører: nøkkeltall, behov som venter, og hvilke ressurser som er tildelt hvilke behov.
///  - Private aktører: bare sine egne ressurser, og om de er tildelt.
/// [Authorize] gjør at uinnloggede besøkende automatisk sendes til innloggingssiden.
/// </summary>
[Authorize]
public class OversiktController : Controller
{
    private readonly IBehovRepository _behovRepository;
    private readonly IRessursRepository _ressursRepository;
    private readonly ITildelingRepository _tildelingRepository;

    public OversiktController(IBehovRepository behovRepository, IRessursRepository ressursRepository,
        ITildelingRepository tildelingRepository)
    {
        _behovRepository = behovRepository;
        _ressursRepository = ressursRepository;
        _tildelingRepository = tildelingRepository;
    }

    public IActionResult Index()
    {
        var alleBehov = _behovRepository.HentAlle();
        var alleRessurser = _ressursRepository.HentAlle();
        var tildelinger = _tildelingRepository.HentAlle();

        // Id-ene til alle ressurser som er tildelt et behov.
        var tildelteRessursIder = tildelinger.Select(t => t.RessursId).ToHashSet();

        // Behov som venter på en ressurs, nyeste først.
        var ventende = alleBehov
            .Where(b => b.Status == BehovStatus.Venter)
            .OrderByDescending(b => b.Dato)
            .ToList();

        var modell = new OversiktViewModel
        {
            AntallAktiveBehov = _behovRepository.TellAktive(),
            AntallHoy = _behovRepository.TellVentende(Prioritet.Hoy),
            AntallModerat = _behovRepository.TellVentende(Prioritet.Moderat),
            AntallLav = _behovRepository.TellVentende(Prioritet.Lav),
            AntallTildelteBehov = _behovRepository.TellTildelte(),

            // En ressurs som er tildelt et behov, er ikke lenger tilgjengelig.
            AntallTilgjengeligeRessurser = alleRessurser.Count(r => !tildelteRessursIder.Contains(r.Id)),

            KreverOppmerksomhet = ventende.Where(b => b.Prioritet == Prioritet.Hoy).Take(5).ToList(),
            SisteBehov = ventende.Take(5).ToList()
        };

        if (User.IsInRole(nameof(BrukerRolle.OffentligAktor)))
        {
            // Offentlige aktører: de fem behovene som sist fikk en ressurs, med alle ressursene de har fått.
            // Resten finnes på siden "Alle tildelte ressurser" (TildelingController).
            modell.TildelteBehov = alleBehov
                .Where(b => b.Status == BehovStatus.Tildelt)
                // Sorterer på den nyeste tildelingen til hvert behov. DefaultIfEmpty gjør at
                // et behov uten tildelinger (skal ikke skje) havner sist i stedet for å gi feil.
                .OrderByDescending(b => tildelinger.Where(t => t.BehovId == b.Id)
                    .Select(t => t.Tidspunkt).DefaultIfEmpty().Max())
                .Take(5)
                .Select(b => new TildeltBehov
                {
                    Behov = b,
                    Ressurser = tildelinger
                        .Where(t => t.BehovId == b.Id)
                        .Select(t => alleRessurser.FirstOrDefault(r => r.Id == t.RessursId))
                        .OfType<Ressurs>()          // hopper over ressurser som ikke finnes
                        .ToList()
                })
                .ToList();
        }
        else
        {
            // Private aktører: bare egne ressurser, og om de er tildelt (ikke hvilket behov).
            var brukernavn = User.Identity?.Name ?? string.Empty;
            modell.MineRessurser = alleRessurser
                .Where(r => r.RegistrertAv == brukernavn)
                .Select(r => new MinRessurs { Ressurs = r, ErTildelt = tildelteRessursIder.Contains(r.Id) })
                .ToList();
        }

        return View(modell);
    }
}
