using System.Security.Claims;
using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester lista over ressurser (/Ressurs): at hver rolle bare ser de ressursene
/// den har lov til å se, og at tildelte ressurser blir merket.
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
    private static List<RessursRad> HentRader(RessursController controller)
    {
        var resultat = Assert.IsType<ViewResult>(controller.Index());
        return Assert.IsAssignableFrom<IEnumerable<RessursRad>>(resultat.Model).ToList();
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
        Assert.All(rader, r => Assert.Equal("kari", r.Ressurs.RegistrertAv));
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
    public void Index_OffentligAktorSerSinEgenRessursSomVenter()
    {
        // Arrange: en offentlig aktør har selv tilbudt en ressurs som ikke er godkjent ennå
        var egen = LeggTilRessurs("hv-kristiansand", RessursStatus.Venter);

        // Act
        var rader = HentRader(LagController("hv-kristiansand", Roller.OffentligAktor));

        // Assert: egne ressurser vises alltid, også før de er godkjent (samme regel som detaljsiden)
        Assert.Contains(rader, r => r.Ressurs.Id == egen.Id);
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
    public void Index_TildelteRessurserBlirMerket()
    {
        // Arrange: én ledig og én som er satt inn på et behov
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
        Assert.False(rader.Single(r => r.Ressurs.Id == tilgjengelig.Id).ErTildelt);
        Assert.True(rader.Single(r => r.Ressurs.Id == tildelt.Id).ErTildelt);
    }

    [Fact]
    public void Index_SortererTilgjengeligeForstOgAvvisteSist()
    {
        // Arrange: én av hver status, lagt inn i "feil" rekkefølge
        var avvist = LeggTilRessurs("ola", RessursStatus.Avvist);
        var venter = LeggTilRessurs("ola", RessursStatus.Venter);
        var tildelt = LeggTilRessurs("ola", RessursStatus.Godkjent);
        var tilgjengelig = LeggTilRessurs("ola", RessursStatus.Godkjent);
        _tildelingRepository.LeggTil(new Tildeling
        {
            BehovId = _behovRepository.HentAlle()[0].Id,
            RessursId = tildelt.Id,
            Tidspunkt = DateTime.Now,
            TildeltAv = "admin"
        });

        // Act
        var rader = HentRader(LagController("admin", Roller.Admin, Roller.OffentligAktor));
        int Plass(Ressurs ressurs) => rader.FindIndex(r => r.Ressurs.Id == ressurs.Id);

        // Assert: tilgjengelig, så tildelt, så venter på godkjenning, og avvist nederst
        Assert.True(Plass(tilgjengelig) < Plass(tildelt));
        Assert.True(Plass(tildelt) < Plass(venter));
        Assert.True(Plass(venter) < Plass(avvist));
        Assert.Equal(avvist.Id, rader[^1].Ressurs.Id);
    }
}
