using System.Security.Claims;
using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester "Endre behov" (BehovController.Rediger): at bare den som meldte behovet og
/// administratorer kommer til, at endringene lagres, og at status, dato og eier ikke endres.
/// </summary>
public class RedigerBehovTester
{
    private readonly InMemoryBehovRepository _behovRepository = new();
    private readonly InMemoryRessursRepository _ressursRepository = new();
    private readonly InMemoryTildelingRepository _tildelingRepository = new();
    private readonly InMemoryHendelseRepository _hendelser = new();

    /// <summary>Lager en BehovController der en bruker med gitt navn og rolle er logget inn.</summary>
    private BehovController LagController(string brukernavn, string rolle)
    {
        var tjeneste = new TildelingTjeneste(_behovRepository, _ressursRepository, _tildelingRepository,
            _hendelser, new InMemoryForesporselRepository());
        var controller = new BehovController(_behovRepository, _ressursRepository, _tildelingRepository, tjeneste);
        TestHjelpere.GiControllerInnlogging(controller);
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, brukernavn),
            new Claim(ClaimTypes.Role, rolle)
        }, "Test"));
        controller.TempData = new TempDataDictionary(controller.HttpContext, Substitute.For<ITempDataProvider>());
        return controller;
    }

    private Behov LeggTilBehov(string registrertAv) => _behovRepository.LeggTil(new Behov
    {
        Type = BehovType.Nodstrom,
        Beskrivelse = "Aggregat til sykehjem",
        GeografiskOmrade = "Lund",
        Prioritet = Prioritet.Moderat,
        Status = BehovStatus.Tildelt,
        Kontaktperson = "Kommunen",
        Telefon = "38070000",
        Dato = new DateOnly(2026, 11, 1),
        RegistrertAv = registrertAv,
        Breddegrad = 58.15,
        Lengdegrad = 8.0
    });

    private static BehovViewModel Endring() => new()
    {
        Type = BehovType.Oppvarming,
        Beskrivelse = "Varmeovner til varmestue",
        GeografiskOmrade = "Søm",
        Prioritet = Prioritet.Hoy,
        Kontaktperson = "Beredskapsleder",
        Telefon = "41111111",
        Breddegrad = 58.147,
        Lengdegrad = 8.07
    };

    [Fact]
    public void RedigerGet_DenSomMeldteFarSkjemaetFyltUt()
    {
        var behov = LeggTilBehov("test");

        var resultat = LagController("test", Roller.OffentligAktor).Rediger(behov.Id);

        var modell = Assert.IsType<BehovViewModel>(Assert.IsType<ViewResult>(resultat).Model);
        Assert.Equal("Aggregat til sykehjem", modell.Beskrivelse);
        Assert.Equal(Prioritet.Moderat, modell.Prioritet);
        Assert.Equal(58.15, modell.Breddegrad);
    }

    [Fact]
    public void RedigerGet_UkjentIdGirIkkeFunnet()
    {
        var resultat = LagController("test", Roller.OffentligAktor).Rediger(99999);

        Assert.IsType<NotFoundResult>(resultat);
    }

    [Fact]
    public void RedigerGet_AndreOffentligeAktorerNektes()
    {
        var behov = LeggTilBehov("noen-andre");

        var resultat = LagController("test", Roller.OffentligAktor).Rediger(behov.Id);

        Assert.IsType<ForbidResult>(resultat);
    }

    [Fact]
    public void RedigerPost_AndreOffentligeAktorerKanIkkeEndre()
    {
        var behov = LeggTilBehov("noen-andre");

        var resultat = LagController("test", Roller.OffentligAktor).Rediger(behov.Id, Endring());

        Assert.IsType<ForbidResult>(resultat);
        Assert.Equal("Aggregat til sykehjem", _behovRepository.HentMedId(behov.Id)!.Beskrivelse);
    }

    [Fact]
    public void RedigerPost_DenSomMeldteLagrerEndringene()
    {
        // Arrange
        var behov = LeggTilBehov("test");

        // Act
        var resultat = LagController("test", Roller.OffentligAktor).Rediger(behov.Id, Endring());

        // Assert: sendt til detaljsiden, og feltene er endret
        var omdirigering = Assert.IsType<RedirectToActionResult>(resultat);
        Assert.Equal(nameof(BehovController.Detaljer), omdirigering.ActionName);

        var lagret = _behovRepository.HentMedId(behov.Id)!;
        Assert.Equal(BehovType.Oppvarming, lagret.Type);
        Assert.Equal("Varmeovner til varmestue", lagret.Beskrivelse);
        Assert.Equal("Søm", lagret.GeografiskOmrade);
        Assert.Equal(Prioritet.Hoy, lagret.Prioritet);
        Assert.Equal(58.147, lagret.Breddegrad);
    }

    [Fact]
    public void RedigerPost_StatusDatoOgEierEndresIkke()
    {
        var behov = LeggTilBehov("test");

        LagController("test", Roller.OffentligAktor).Rediger(behov.Id, Endring());

        var lagret = _behovRepository.HentMedId(behov.Id)!;
        Assert.Equal(BehovStatus.Tildelt, lagret.Status);           // styres av tildelingene
        Assert.Equal(new DateOnly(2026, 11, 1), lagret.Dato);      // når det ble meldt
        Assert.Equal("test", lagret.RegistrertAv);                  // hvem som meldte det
    }

    [Fact]
    public void RedigerPost_AdminKanEndreAndresBehov()
    {
        var behov = LeggTilBehov("test");

        var resultat = LagController("admin", Roller.Admin).Rediger(behov.Id, Endring());

        Assert.IsType<RedirectToActionResult>(resultat);
        Assert.Equal("Varmeovner til varmestue", _behovRepository.HentMedId(behov.Id)!.Beskrivelse);
    }

    [Fact]
    public void RedigerPost_UgyldigSkjemaLagrerIkkeNoe()
    {
        var behov = LeggTilBehov("test");
        var controller = LagController("test", Roller.OffentligAktor);
        controller.ModelState.AddModelError(nameof(BehovViewModel.Telefon), "Du må oppgi et telefonnummer.");

        var resultat = controller.Rediger(behov.Id, Endring());

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("Aggregat til sykehjem", _behovRepository.HentMedId(behov.Id)!.Beskrivelse);
    }

    [Fact]
    public void RedigerPost_EndretPrioritetStarIHistorikken()
    {
        var behov = LeggTilBehov("test");

        LagController("test", Roller.OffentligAktor).Rediger(behov.Id, Endring());

        var hendelse = Assert.Single(_hendelser.HentForBehov(behov.Id));
        Assert.Equal("test", hendelse.UtfortAv);
        Assert.Equal("Behovet ble endret, prioritet moderat → høy", hendelse.Beskrivelse);
    }

    [Fact]
    public void RedigerPost_UendretPrioritetGirEnkelLinjeIHistorikken()
    {
        var behov = LeggTilBehov("test");
        var endring = Endring();
        endring.Prioritet = Prioritet.Moderat;

        LagController("test", Roller.OffentligAktor).Rediger(behov.Id, endring);

        Assert.Equal("Behovet ble endret", Assert.Single(_hendelser.HentForBehov(behov.Id)).Beskrivelse);
    }
}
