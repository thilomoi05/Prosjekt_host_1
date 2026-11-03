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
/// Tester "Endre ressurs" (RessursController.Rediger): at bare eieren og administratorer
/// kommer til, at endringene lagres, og reglene for status og bilder.
/// </summary>
public class RedigerRessursTester : IDisposable
{
    private readonly string _testmappe;
    private readonly InMemoryBehovRepository _behovRepository = new();
    private readonly InMemoryRessursRepository _ressursRepository = new();
    private readonly InMemoryTildelingRepository _tildelingRepository = new();

    public RedigerRessursTester()
    {
        _testmappe = Path.Combine(Path.GetTempPath(), "beredskapsportal-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_testmappe);
    }

    public void Dispose()
    {
        Directory.Delete(_testmappe, recursive: true);
    }

    /// <summary>Lager en RessursController der en bruker med gitt navn og rolle er logget inn.</summary>
    private RessursController LagController(string brukernavn, string rolle, IHendelseRepository? hendelser = null)
    {
        var tjeneste = new TildelingTjeneste(_behovRepository, _ressursRepository, _tildelingRepository,
            hendelser ?? new InMemoryHendelseRepository(), new InMemoryForesporselRepository());
        var controller = new RessursController(_ressursRepository, _behovRepository, _tildelingRepository,
            tjeneste, new FalsktMiljo(_testmappe));
        TestHjelpere.GiControllerInnlogging(controller);
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, brukernavn),
            new Claim(ClaimTypes.Role, rolle)
        }, "Test"));
        // TempData brukes til meldingen etter lagring.
        controller.TempData = new TempDataDictionary(controller.HttpContext, Substitute.For<ITempDataProvider>());
        return controller;
    }

    private Ressurs LeggTilRessurs(string registrertAv, RessursStatus status = RessursStatus.Godkjent) =>
        _ressursRepository.LeggTil(new Ressurs
        {
            Type = RessursType.Aggregat,
            Status = status,
            BeskrivelseAvKapasitet = "20 kVA",
            Adresse = "Kirkegata 5, 4610 Kristiansand",
            Tilgangsbeskrivelse = "Står i garasjen",
            Kontaktperson = "Ola",
            Telefon = "40000000",
            Bilder = new List<string> { "gammelt.jpg" },
            RegistrertAv = registrertAv,
            Breddegrad = 58.146,
            Lengdegrad = 7.995
        });

    private static RedigerRessursViewModel Endring(int id) => new()
    {
        Id = id,
        Type = RessursType.Transport,
        BeskrivelseAvKapasitet = "Lastebil med plass til 10 paller",
        Adresse = "Markens gate 1, 4611 Kristiansand",
        Tilgangsbeskrivelse = "Ring først",
        Kontaktperson = "Kari",
        Telefon = "41111111",
        Breddegrad = 58.147,
        Lengdegrad = 7.996
    };

    [Fact]
    public void RedigerGet_EierenFarSkjemaetFyltUt()
    {
        var ressurs = LeggTilRessurs("privat");

        var resultat = LagController("privat", Roller.PrivatAktor).Rediger(ressurs.Id);

        var modell = Assert.IsType<RedigerRessursViewModel>(Assert.IsType<ViewResult>(resultat).Model);
        Assert.Equal(ressurs.BeskrivelseAvKapasitet, modell.BeskrivelseAvKapasitet);
        Assert.Equal(ressurs.Breddegrad, modell.Breddegrad);
        Assert.Equal(new List<string> { "gammelt.jpg" }, modell.EksisterendeBilder);
    }

    [Fact]
    public void RedigerGet_UkjentIdGirIkkeFunnet()
    {
        var resultat = LagController("privat", Roller.PrivatAktor).Rediger(99999);

        Assert.IsType<NotFoundResult>(resultat);
    }

    [Fact]
    public void RedigerGet_AndreBrukereNektes()
    {
        var ressurs = LeggTilRessurs("noen-andre");

        var resultat = LagController("privat", Roller.PrivatAktor).Rediger(ressurs.Id);

        Assert.IsType<ForbidResult>(resultat);
    }

    [Fact]
    public async Task RedigerPost_AndreBrukereKanIkkeEndre()
    {
        // Arrange: en ressurs som tilhører noen andre
        var ressurs = LeggTilRessurs("noen-andre");

        // Act: en offentlig aktør prøver å sende endringen direkte (uten å gå via skjemaet)
        var resultat = await LagController("test", Roller.OffentligAktor).Rediger(ressurs.Id, Endring(ressurs.Id));

        // Assert: nektet, og ingenting er endret
        Assert.IsType<ForbidResult>(resultat);
        Assert.Equal("20 kVA", _ressursRepository.HentMedId(ressurs.Id)!.BeskrivelseAvKapasitet);
    }

    [Fact]
    public async Task RedigerPost_EierenLagrerEndringerOgRessursenMaGodkjennesPaNytt()
    {
        var ressurs = LeggTilRessurs("privat");

        var resultat = await LagController("privat", Roller.PrivatAktor).Rediger(ressurs.Id, Endring(ressurs.Id));

        var omdirigering = Assert.IsType<RedirectToActionResult>(resultat);
        Assert.Equal(nameof(RessursController.Detaljer), omdirigering.ActionName);

        var lagret = _ressursRepository.HentMedId(ressurs.Id)!;
        Assert.Equal(RessursType.Transport, lagret.Type);
        Assert.Equal("Lastebil med plass til 10 paller", lagret.BeskrivelseAvKapasitet);
        Assert.Equal("Markens gate 1, 4611 Kristiansand", lagret.Adresse);
        Assert.Equal(RessursStatus.Venter, lagret.Status);
        Assert.Equal("privat", lagret.RegistrertAv);                         // eieren er uendret
        Assert.Equal(new List<string> { "gammelt.jpg" }, lagret.Bilder);     // ingen nye bilder: de gamle beholdes
    }

    [Fact]
    public async Task RedigerPost_AdminEndrerUtenAtStatusenEndres()
    {
        var ressurs = LeggTilRessurs("privat");

        await LagController("admin", Roller.Admin).Rediger(ressurs.Id, Endring(ressurs.Id));

        Assert.Equal(RessursStatus.Godkjent, _ressursRepository.HentMedId(ressurs.Id)!.Status);
    }

    [Fact]
    public async Task RedigerPost_AvvistRessursSendesTilGodkjenningIgjen()
    {
        var ressurs = LeggTilRessurs("privat", RessursStatus.Avvist);
        _ressursRepository.SettStatus(ressurs.Id, RessursStatus.Avvist, "Mangler bilde av aggregatet");

        await LagController("privat", Roller.PrivatAktor).Rediger(ressurs.Id, Endring(ressurs.Id));

        var lagret = _ressursRepository.HentMedId(ressurs.Id)!;
        Assert.Equal(RessursStatus.Venter, lagret.Status);
        Assert.Null(lagret.Avvisningsgrunn);
    }

    [Fact]
    public async Task RedigerPost_NyeBilderErstatterDeGamle()
    {
        var ressurs = LeggTilRessurs("privat");
        var endring = Endring(ressurs.Id);
        endring.Bilder.Add(TestHjelpere.LagBilde("nytt.png"));

        await LagController("privat", Roller.PrivatAktor).Rediger(ressurs.Id, endring);

        var bilder = _ressursRepository.HentMedId(ressurs.Id)!.Bilder;
        Assert.Single(bilder);
        Assert.DoesNotContain("gammelt.jpg", bilder);
        Assert.EndsWith(".png", bilder[0]);
    }

    [Fact]
    public async Task RedigerPost_FilSomIkkeErBildeGirFeilmeldingOgIngenEndring()
    {
        var ressurs = LeggTilRessurs("privat");
        var endring = Endring(ressurs.Id);
        endring.Bilder.Add(TestHjelpere.LagBilde("virus.exe"));

        var resultat = await LagController("privat", Roller.PrivatAktor).Rediger(ressurs.Id, endring);

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("20 kVA", _ressursRepository.HentMedId(ressurs.Id)!.BeskrivelseAvKapasitet);
    }

    [Fact]
    public async Task RedigerPost_EndringenStarIHistorikken()
    {
        var ressurs = LeggTilRessurs("privat");
        var hendelser = new InMemoryHendelseRepository();
        var controller = LagController("privat", Roller.PrivatAktor, hendelser);

        await controller.Rediger(ressurs.Id, Endring(ressurs.Id));

        var hendelse = Assert.Single(hendelser.HentForRessurs(ressurs.Id));
        Assert.Equal("privat", hendelse.UtfortAv);
        Assert.Contains("endret", hendelse.Beskrivelse);
    }
}
