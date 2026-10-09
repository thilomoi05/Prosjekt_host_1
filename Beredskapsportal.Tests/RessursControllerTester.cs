using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester reglene for bildeopplasting i RessursController, og at bildevisningen
/// ikke kan misbrukes til å hente andre filer.
/// Hver test får sin egen midlertidige mappe, som slettes etterpå (Dispose).
/// </summary>
public class RessursControllerTester : IDisposable
{
    private readonly string _testmappe;
    private readonly RessursController _controller;

    public RessursControllerTester()
    {
        _testmappe = Path.Combine(Path.GetTempPath(), "beredskapsportal-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_testmappe);
        _controller = new RessursController(new InMemoryRessursRepository(), new FalsktMiljo(_testmappe));
    }

    public void Dispose()
    {
        Directory.Delete(_testmappe, recursive: true);
    }

    private static RessursViewModel GyldigSkjema(params IFormFile[] bilder) => new()
    {
        Type = RessursType.Aggregat,
        BeskrivelseAvKapasitet = "20 kVA dieselaggregat",
        Adresse = "Kirkegata 5, 4610 Kristiansand",
        Tilgangsbeskrivelse = "Står i garasjen",
        Bilder = bilder.ToList(),
        PlasseringBekreftet = true,
        Kontaktperson = "Ola Nordmann",
        Telefon = "40000000",
        Breddegrad = 58.146,
        Lengdegrad = 7.995
    };

    private string ForsteBildefeil() =>
        _controller.ModelState[nameof(RessursViewModel.Bilder)]!.Errors[0].ErrorMessage;

    [Fact]
    public async Task Registrer_UtenBilderGirFeilmelding()
    {
        var resultat = await _controller.Registrer(GyldigSkjema());

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("Du må legge ved minst ett bilde av ressursen.", ForsteBildefeil());
    }

    [Fact]
    public async Task Registrer_FilSomIkkeErBildeGirFeilmelding()
    {
        var resultat = await _controller.Registrer(GyldigSkjema(TestHjelpere.LagBilde("virus.exe")));

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("«virus.exe» er ikke et bilde. Bruk JPG, PNG eller WEBP.", ForsteBildefeil());
    }

    [Fact]
    public void Bilde_KanIkkeBrukesTilAHenteFilerUtenforBildemappen()
    {
        // Arrange: lag en "hemmelig" fil i testmappen, utenfor bildemappen
        Directory.CreateDirectory(Path.Combine(_testmappe, "Opplastinger", "bilder"));
        File.WriteAllText(Path.Combine(_testmappe, "hemmelig.txt"), "skal ikke kunne lastes ned");

        // Act: prøv å nå den med "../" i id-en
        var resultat = _controller.Bilde("../../hemmelig.txt");

        // Assert
        Assert.IsType<NotFoundResult>(resultat);
    }
}
