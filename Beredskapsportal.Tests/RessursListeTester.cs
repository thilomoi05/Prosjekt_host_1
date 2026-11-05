using System.Security.Claims;
using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester lista over ressurser (/Ressurs): at hver rolle bare ser de ressursene
/// den har lov til å se, og at status (tilgjengelig, tildelt, venter) blir riktig.
/// </summary>
public class RessursListeTester
{
    private readonly InMemoryBehovRepository _behovRepository = new();
    private readonly InMemoryRessursRepository _ressursRepository = new();
    private readonly InMemoryTildelingRepository _tildelingRepository = new();

    /// <summary>Lager en RessursController der en bruker med gitt navn og rolle(r) er logget inn.</summary>
    private RessursController LagController(string brukernavn, params string[] roller)
    {
        var controller = new RessursController(_ressursRepository, _behovRepository, _tildelingRepository,
            TestHjelpere.LagTildelingTjeneste(_behovRepository, _ressursRepository, _tildelingRepository),
            new FalsktMiljo(Path.GetTempPath()));
        TestHjelpere.GiControllerInnlogging(controller);

        var claims = new List<Claim> { new(ClaimTypes.Name, brukernavn) };
        claims.AddRange(roller.Select(rolle => new Claim(ClaimTypes.Role, rolle)));
        controller.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        return controller;
    }

    /// <summary>Legger til en ressurs registrert av en gitt bruker, og returnerer den.</summary>
    private Ressurs LeggTilRessurs(string registrertAv, RessursStatus status) => _ressursRepository.LeggTil(new Ressurs
    {
        Type = RessursType.Aggregat,
        Status = status,
        BeskrivelseAvKapasitet = "20 kVA",
        Adresse = "Kirkegata 5, 4610 Kristiansand",
        RegistrertAv = registrertAv,
        Breddegrad = 58.146,
        Lengdegrad = 7.995
    });

    /// <summary>Kjører Index() og henter ut radene viewet får.</summary>
    private static List<RessursListeRad> HentRader(RessursController controller)
    {
        var resultat = Assert.IsType<ViewResult>(controller.Index());
        return Assert.IsAssignableFrom<IEnumerable<RessursListeRad>>(resultat.Model).ToList();
    }

    [Fact]
    public void Index_PrivatAktorSerBareSineEgne()
    {
        // Arrange
        var egen = LeggTilRessurs("kari", RessursStatus.Venter);
        var andres = LeggTilRessurs("ola", RessursStatus.Godkjent);

        // Act
        var rader = HentRader(LagController("kari", Roller.PrivatAktor));

        // Assert: Kari ser sin egen (selv om den venter), men ikke Ola sin
        Assert.Contains(rader, r => r.Ressurs.Id == egen.Id);
        Assert.DoesNotContain(rader, r => r.Ressurs.Id == andres.Id);
        Assert.All(rader, r => Assert.True(r.ErMin));
    }

    [Fact]
    public void Index_OffentligAktorSerGodkjenteMenIkkeVentendeEllerAvviste()
    {
        // Arrange
        var godkjent = LeggTilRessurs("ola", RessursStatus.Godkjent);
        var venter = LeggTilRessurs("ola", RessursStatus.Venter);
        var avvist = LeggTilRessurs("ola", RessursStatus.Avvist);

        // Act
        var rader = HentRader(LagController("hv-kristiansand", Roller.OffentligAktor));

        // Assert
        Assert.Contains(rader, r => r.Ressurs.Id == godkjent.Id);
        Assert.DoesNotContain(rader, r => r.Ressurs.Id == venter.Id);
        Assert.DoesNotContain(rader, r => r.Ressurs.Id == avvist.Id);
    }

    [Fact]
    public void Index_AdminSerAlleRessurser()
    {
        // Arrange
        var venter = LeggTilRessurs("ola", RessursStatus.Venter);
        var avvist = LeggTilRessurs("kari", RessursStatus.Avvist);

        // Act
        var rader = HentRader(LagController("admin", Roller.Admin, Roller.OffentligAktor));

        // Assert: admin ser alle, også de som venter og er avvist
        Assert.Equal(_ressursRepository.HentAlle().Count, rader.Count);
        Assert.Contains(rader, r => r.Ressurs.Id == venter.Id);
        Assert.Contains(rader, r => r.Ressurs.Id == avvist.Id);
    }

    [Fact]
    public void Index_GodkjentRessursErTilgjengeligEllerTildelt()
    {
        // Arrange: én tilgjengelig og én som er satt inn på et behov
        var tilgjengelig = LeggTilRessurs("ola", RessursStatus.Godkjent);
        var tildelt = LeggTilRessurs("ola", RessursStatus.Godkjent);
        _tildelingRepository.LeggTil(new Tildeling
        {
            BehovId = _behovRepository.HentAlle()[0].Id,
            RessursId = tildelt.Id,
            Tidspunkt = DateTime.Now,
            TildeltAv = "admin"
        });

        // Act
        var rader = HentRader(LagController("admin", Roller.Admin, Roller.OffentligAktor));

        // Assert
        Assert.Equal("tilgjengelig", rader.Single(r => r.Ressurs.Id == tilgjengelig.Id).StatusKode);
        Assert.Equal("tildelt", rader.Single(r => r.Ressurs.Id == tildelt.Id).StatusKode);
    }

    [Theory]
    [InlineData(RessursStatus.Venter, false, "venter", "Venter på godkjenning")]
    [InlineData(RessursStatus.Avvist, false, "avvist", "Avvist")]
    [InlineData(RessursStatus.Godkjent, false, "tilgjengelig", "Tilgjengelig")]
    [InlineData(RessursStatus.Godkjent, true, "tildelt", "Tildelt et behov")]
    public void RessursListeRad_GirRiktigStatus(RessursStatus status, bool erTildelt, string kode, string tekst)
    {
        var rad = new RessursListeRad { Ressurs = new Ressurs { Status = status }, ErTildelt = erTildelt };

        Assert.Equal(kode, rad.StatusKode);
        Assert.Equal(tekst, rad.StatusTekst);
    }
}
