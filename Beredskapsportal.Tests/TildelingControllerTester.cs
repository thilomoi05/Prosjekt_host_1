using Beredskapsportal.Controllers;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester siden "Alle tildelte ressurser": én rad per tildeling, med riktig behov og ressurs.
/// </summary>
public class TildelingControllerTester
{
    private readonly InMemoryBehovRepository _behovRepository = new();
    private readonly InMemoryRessursRepository _ressursRepository = new();
    private readonly InMemoryTildelingRepository _tildelingRepository = new();

    private List<TildelingRad> HentRader()
    {
        var controller = new TildelingController(_behovRepository, _ressursRepository, _tildelingRepository);
        var resultat = Assert.IsType<ViewResult>(controller.Index());
        return Assert.IsType<List<TildelingRad>>(resultat.Model);
    }

    [Fact]
    public void Index_GirEnRadPerTildeling()
    {
        var rader = HentRader();

        Assert.Equal(_tildelingRepository.HentAlle().Count, rader.Count);
    }

    [Fact]
    public void Index_HverRadHarRiktigBehovOgRessurs()
    {
        var rader = HentRader();

        // Behovet og ressursen i raden er de samme som tildelingen peker på.
        foreach (var rad in rader)
        {
            Assert.Equal(rad.Tildeling.BehovId, rad.Behov.Id);
            Assert.Equal(rad.Tildeling.RessursId, rad.Ressurs.Id);
        }
    }

    [Fact]
    public void Index_NyesteTildelingKommerForst()
    {
        var rader = HentRader();

        var nyeste = _tildelingRepository.HentAlle().Max(t => t.Tidspunkt);
        Assert.Equal(nyeste, rader[0].Tildeling.Tidspunkt);
    }
}
