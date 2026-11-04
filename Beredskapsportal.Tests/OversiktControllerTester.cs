using System.Security.Claims;
using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester panelet "Mine meldte behov" på Oversikt.
/// </summary>
public class OversiktControllerTester
{
    private readonly InMemoryBehovRepository _behov = new();
    private readonly InMemoryRessursRepository _ressurser = new();
    private readonly InMemoryTildelingRepository _tildelinger = new();
    private readonly TildelingTjeneste _tjeneste;

    public OversiktControllerTester()
    {
        _tjeneste = TestHjelpere.LagTildelingTjeneste(_behov, _ressurser, _tildelinger);
    }

    /// <summary>Åpner Oversikt som en innlogget offentlig aktør og returnerer modellen.</summary>
    private OversiktViewModel ApneOversikt(string brukernavn)
    {
        var controller = new OversiktController(_behov, _ressurser, _tildelinger, _tjeneste);
        TestHjelpere.GiControllerInnlogging(controller);
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, brukernavn),
            new Claim(ClaimTypes.Role, Roller.OffentligAktor)
        }, "Test"));

        var resultat = Assert.IsType<ViewResult>(controller.Index());
        return Assert.IsType<OversiktViewModel>(resultat.Model);
    }

    [Fact]
    public void MineMeldteBehov_ViserBareEgneBehovOgMaksFem()
    {
        var modell = ApneOversikt("test");

        Assert.NotEmpty(modell.MineMeldteBehov);
        Assert.True(modell.MineMeldteBehov.Count <= 5);
        Assert.All(modell.MineMeldteBehov, rad => Assert.Equal("test", rad.Behov.RegistrertAv));
    }

    [Fact]
    public void MineMeldteBehov_TellerForesporslerSomVenter()
    {
        // En forespørsel fra "test" til det nyeste behovet sitt.
        var behov = _behov.HentAlle().Where(b => b.RegistrertAv == "test")
            .OrderByDescending(b => b.Dato).ThenByDescending(b => b.Id).First();
        var ressurs = _tjeneste.LedigeRessurser().First();
        _tjeneste.Foresporr(behov.Id, ressurs.Id, null, "test");

        var modell = ApneOversikt("test");

        var rad = modell.MineMeldteBehov.Single(r => r.Behov.Id == behov.Id);
        Assert.Equal(1, rad.VentendeForesporsler);
    }

    [Fact]
    public void MineMeldteBehov_TomForBrukerUtenBehov()
    {
        var modell = ApneOversikt("ny-kommune");

        Assert.Empty(modell.MineMeldteBehov);
    }
}
