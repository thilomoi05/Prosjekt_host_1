using System.Reflection;
using System.Text.Json;
using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester kart-endepunktene /Kart/Behov og /Kart/Ressurser, som kart.js henter punktene fra.
/// Svaret er JSON. Vi gjør det om til tekst og leser det tilbake, slik nettleseren gjør,
/// så testene sjekker de samme feltnavnene som kart.js bruker.
/// </summary>
public class KartControllerTester
{
    private readonly InMemoryBehovRepository _behovRepository = new();
    private readonly InMemoryRessursRepository _ressursRepository = new();
    private readonly InMemoryTildelingRepository _tildelingRepository = new();

    private KartController LagController()
    {
        var controller = new KartController(_behovRepository, _ressursRepository, _tildelingRepository)
        {
            // Ressurser() lager bildeadresser med Url.Action. I testene holder en falsk UrlHelper.
            Url = Substitute.For<IUrlHelper>()
        };
        return controller;
    }

    /// <summary>Gjør JSON-svaret om til en liste med punkter vi kan lese felt fra.</summary>
    private static List<JsonElement> LesPunkter(IActionResult resultat)
    {
        var json = JsonSerializer.Serialize(Assert.IsType<JsonResult>(resultat).Value);
        return JsonSerializer.Deserialize<List<JsonElement>>(json)!;
    }

    [Fact]
    public void Behov_GirEttPunktPerBehov()
    {
        var punkter = LesPunkter(LagController().Behov());

        Assert.Equal(_behovRepository.HentAlle().Count, punkter.Count);
    }

    [Fact]
    public void Behov_HarFelteneKartetTrenger()
    {
        // Arrange: et nytt behov med kjente verdier
        var behov = _behovRepository.LeggTil(new Behov
        {
            Type = BehovType.Oppvarming,
            Beskrivelse = "Varmeovner",
            GeografiskOmrade = "Søm",
            Prioritet = Prioritet.Hoy,
            Breddegrad = 58.147,
            Lengdegrad = 8.07
        });

        // Act
        var punkt = LesPunkter(LagController().Behov()).Single(p => p.GetProperty("id").GetInt32() == behov.Id);

        // Assert: samme feltnavn og verdier som kart.js leser
        Assert.Equal("Oppvarming", punkt.GetProperty("typeKode").GetString());
        Assert.Equal("Søm", punkt.GetProperty("omrade").GetString());
        Assert.Equal("hoy", punkt.GetProperty("prioritet").GetString());
        Assert.Equal(58.147, punkt.GetProperty("breddegrad").GetDouble());
        Assert.Equal(8.07, punkt.GetProperty("lengdegrad").GetDouble());
    }

    [Fact]
    public void Ressurser_ViserBareGodkjenteRessurser()
    {
        // Arrange: én ressurs som venter og én som er avvist
        var venter = _ressursRepository.LeggTil(new Ressurs { Status = RessursStatus.Venter, Breddegrad = 58.1, Lengdegrad = 8.0 });
        var avvist = _ressursRepository.LeggTil(new Ressurs { Status = RessursStatus.Avvist, Breddegrad = 58.1, Lengdegrad = 8.0 });

        // Act
        var ider = LesPunkter(LagController().Ressurser()).Select(p => p.GetProperty("id").GetInt32()).ToList();

        // Assert
        Assert.DoesNotContain(venter.Id, ider);
        Assert.DoesNotContain(avvist.Id, ider);
        var antallGodkjente = _ressursRepository.HentAlle().Count(r => r.Status == RessursStatus.Godkjent);
        Assert.Equal(antallGodkjente, ider.Count);
    }

    [Fact]
    public void Ressurser_MarkererTildelteRessurser()
    {
        // En tildeling fra eksempeldataene
        var tildeling = _tildelingRepository.HentAlle()[0];

        var punkter = LesPunkter(LagController().Ressurser());

        var tildelt = punkter.Single(p => p.GetProperty("id").GetInt32() == tildeling.RessursId);
        Assert.True(tildelt.GetProperty("tildelt").GetBoolean());
        Assert.Contains(punkter, p => !p.GetProperty("tildelt").GetBoolean());
    }

    [Theory]
    [InlineData(nameof(KartController.Behov))]
    [InlineData(nameof(KartController.Ressurser))]
    public void Endepunktene_SvarerBarePaGetOgKreverOffentligAktor(string metodenavn)
    {
        var metode = typeof(KartController).GetMethod(metodenavn)!;

        // Bare GET: kartet skal bare lese data, aldri endre.
        Assert.NotNull(metode.GetCustomAttribute<HttpGetAttribute>());

        // Tilgangen står på klassen og gjelder alle metodene: bare offentlige aktører.
        var tilgang = typeof(KartController).GetCustomAttribute<AuthorizeAttribute>()!;
        Assert.Equal(Roller.OffentligAktor, tilgang.Roles);
    }
}
