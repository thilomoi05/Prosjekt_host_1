using System.Security.Claims;
using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester detaljsidene for behov og ressurser: riktig side for gyldig id,
/// "ikke funnet" for ukjent id, og at private aktører bare ser sine egne ressurser.
/// </summary>
public class DetaljerTester
{
    private readonly InMemoryBehovRepository _behovRepository = new();
    private readonly InMemoryRessursRepository _ressursRepository = new();
    private readonly InMemoryTildelingRepository _tildelingRepository = new();

    private BehovController LagBehovController() =>
        new(_behovRepository, _ressursRepository, _tildelingRepository);

    /// <summary>Lager en RessursController der en bruker med gitt navn og rolle er logget inn.</summary>
    private RessursController LagRessursController(string brukernavn, BrukerRolle rolle)
    {
        var controller = new RessursController(_ressursRepository, _behovRepository, _tildelingRepository,
            new FalsktMiljo(Path.GetTempPath()));
        TestHjelpere.GiControllerInnlogging(controller);

        // Den innloggede brukeren: et navn og en rolle, slik innloggingscookien ville gitt.
        var identitet = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, brukernavn),
            new Claim(ClaimTypes.Role, rolle.ToString())
        }, "Test");
        controller.HttpContext.User = new ClaimsPrincipal(identitet);

        return controller;
    }

    /// <summary>Legger til en ressurs registrert av en gitt bruker, og returnerer den.</summary>
    private Ressurs LeggTilRessurs(string registrertAv) => _ressursRepository.LeggTil(new Ressurs
    {
        Type = RessursType.Aggregat,
        BeskrivelseAvKapasitet = "20 kVA",
        Adresse = "Kirkegata 5, 4610 Kristiansand",
        RegistrertAv = registrertAv,
        Breddegrad = 58.146,
        Lengdegrad = 7.995
    });

    [Fact]
    public void BehovDetaljer_GyldigIdViserBehovet()
    {
        var behov = _behovRepository.HentAlle()[0];

        var resultat = LagBehovController().Detaljer(behov.Id);

        var side = Assert.IsType<ViewResult>(resultat);
        var modell = Assert.IsType<BehovDetaljerViewModel>(side.Model);
        Assert.Equal(behov.Id, modell.Behov.Id);
    }

    [Fact]
    public void BehovDetaljer_UkjentIdGirIkkeFunnet()
    {
        var resultat = LagBehovController().Detaljer(99999);

        Assert.IsType<NotFoundResult>(resultat);
    }

    [Fact]
    public void BehovDetaljer_ViserRessurseneSomErTildelt()
    {
        // Arrange: en tildeling fra eksempeldataene
        var tildeling = _tildelingRepository.HentAlle()[0];

        // Act
        var resultat = LagBehovController().Detaljer(tildeling.BehovId);

        // Assert: ressursen i tildelingen er med på siden
        var modell = Assert.IsType<BehovDetaljerViewModel>(Assert.IsType<ViewResult>(resultat).Model);
        Assert.Contains(modell.TildelteRessurser, r => r.Id == tildeling.RessursId);
    }

    [Fact]
    public void RessursDetaljer_UkjentIdGirIkkeFunnet()
    {
        var controller = LagRessursController("test", BrukerRolle.OffentligAktor);

        var resultat = controller.Detaljer(99999);

        Assert.IsType<NotFoundResult>(resultat);
    }

    [Fact]
    public void RessursDetaljer_OffentligAktorSerAlleRessurser()
    {
        var ressurs = LeggTilRessurs("noen-andre");
        var controller = LagRessursController("test", BrukerRolle.OffentligAktor);

        var resultat = controller.Detaljer(ressurs.Id);

        Assert.IsType<ViewResult>(resultat);
    }

    [Fact]
    public void RessursDetaljer_PrivatAktorSerSinEgenRessurs()
    {
        var ressurs = LeggTilRessurs("privat");
        var controller = LagRessursController("privat", BrukerRolle.PrivatAktor);

        var resultat = controller.Detaljer(ressurs.Id);

        var modell = Assert.IsType<RessursDetaljerViewModel>(Assert.IsType<ViewResult>(resultat).Model);
        Assert.Equal(ressurs.Id, modell.Ressurs.Id);
        Assert.Null(modell.TildeltBehov);   // private får ikke vite hvilket behov
    }

    [Fact]
    public void RessursDetaljer_PrivatAktorKanIkkeSeAndresRessurs()
    {
        var ressurs = LeggTilRessurs("noen-andre");
        var controller = LagRessursController("privat", BrukerRolle.PrivatAktor);

        var resultat = controller.Detaljer(ressurs.Id);

        // Forbid() sender brukeren til oversikten (AccessDeniedPath i Program.cs).
        Assert.IsType<ForbidResult>(resultat);
    }
}
