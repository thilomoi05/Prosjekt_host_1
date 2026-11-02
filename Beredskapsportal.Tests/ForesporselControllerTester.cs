using System.Security.Claims;
using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester at bare den som meldte behovet, kan finne og forespørre ressurser til det.
/// </summary>
public class ForesporselControllerTester
{
    private readonly InMemoryBehovRepository _behov = new();
    private readonly InMemoryRessursRepository _ressurser = new();
    private readonly InMemoryTildelingRepository _tildelinger = new();

    /// <summary>Lager controlleren med en innlogget offentlig aktør.</summary>
    private ForesporselController LagController(string brukernavn)
    {
        var controller = new ForesporselController(_behov,
            TestHjelpere.LagTildelingTjeneste(_behov, _ressurser, _tildelinger));
        TestHjelpere.GiControllerInnlogging(controller);
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, brukernavn),
            new Claim(ClaimTypes.Role, Roller.OffentligAktor)
        }, "Test"));
        return controller;
    }

    [Fact]
    public void FinnRessurs_EgetBehovViserForslag()
    {
        // Eksempelbehovene er meldt av "test".
        var behov = _behov.HentAlle()[0];

        var resultat = LagController("test").FinnRessurs(behov.Id);

        Assert.IsType<ViewResult>(resultat);
    }

    [Fact]
    public void FinnRessurs_AndresBehovNektes()
    {
        var behov = _behov.HentAlle()[0];

        var resultat = LagController("en-annen-kommune").FinnRessurs(behov.Id);

        Assert.IsType<ForbidResult>(resultat);
    }

    [Fact]
    public void Foresporr_AndresBehovNektes()
    {
        var behov = _behov.HentAlle()[0];

        var resultat = LagController("en-annen-kommune").Foresporr(behov.Id, 1, null);

        Assert.IsType<ForbidResult>(resultat);
    }
}
