using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester POST-håndteringen i RessursController.Registrer (lagring og avvisning),
/// reglene for bildeopplasting, og at bildevisningen ikke kan misbrukes til å hente andre filer.
/// Hver test får sin egen midlertidige mappe, som slettes etterpå (Dispose).
/// </summary>
public class RessursControllerTester : IDisposable
{
    private readonly string _testmappe;
    private readonly RessursController _controller;
    private readonly InMemoryRessursRepository _ressurser = new();

    public RessursControllerTester()
    {
        _testmappe = Path.Combine(Path.GetTempPath(), "beredskapsportal-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_testmappe);
        var behov = new InMemoryBehovRepository();
        var tildelinger = new InMemoryTildelingRepository();
        _controller = new RessursController(_ressurser, behov, tildelinger,
            TestHjelpere.LagTildelingTjeneste(behov, _ressurser, tildelinger), new FalsktMiljo(_testmappe))
        {
            // TempData brukes til meldingen etter at ressursen er lagret.
            TempData = new TempDataDictionary(new Microsoft.AspNetCore.Http.DefaultHttpContext(), Substitute.For<ITempDataProvider>())
        };
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
    public async Task Registrer_GyldigSkjemaLagrerRessursenSomVenterPaGodkjenning()
    {
        // Arrange
        var antallFor = _ressurser.HentAlle().Count;

        // Act
        var resultat = await _controller.Registrer(GyldigSkjema(TestHjelpere.LagBilde("aggregat.jpg")));

        // Assert: sendt til Oversikt, ressursen er lagret og venter på en administrator
        var omdirigering = Assert.IsType<RedirectToActionResult>(resultat);
        Assert.Equal("Oversikt", omdirigering.ControllerName);
        Assert.Equal(antallFor + 1, _ressurser.HentAlle().Count);

        var lagret = _ressurser.HentAlle().OrderByDescending(r => r.Id).First();
        Assert.Equal("20 kVA dieselaggregat", lagret.BeskrivelseAvKapasitet);
        Assert.Equal(RessursStatus.Venter, lagret.Status);
        Assert.Single(lagret.Bilder);
        // Bildet har fått et nytt, tilfeldig navn, og ligger i bildemappen.
        Assert.NotEqual("aggregat.jpg", lagret.Bilder[0]);
        Assert.True(File.Exists(Path.Combine(_testmappe, "Opplastinger", "bilder", lagret.Bilder[0])));
    }

    [Fact]
    public async Task Registrer_UgyldigSkjemaLagrerIkkeNoe()
    {
        // Arrange: later som ASP.NET fant en valideringsfeil i et tekstfelt
        _controller.ModelState.AddModelError(nameof(RessursViewModel.Telefon), "Du må oppgi et telefonnummer.");
        var antallFor = _ressurser.HentAlle().Count;

        // Act
        var resultat = await _controller.Registrer(GyldigSkjema(TestHjelpere.LagBilde("aggregat.jpg")));

        // Assert: skjemaet vises på nytt med det brukeren skrev, og ingenting er lagret
        var side = Assert.IsType<ViewResult>(resultat);
        Assert.Equal("20 kVA dieselaggregat", Assert.IsType<RessursViewModel>(side.Model).BeskrivelseAvKapasitet);
        Assert.Equal(antallFor, _ressurser.HentAlle().Count);
    }

    [Fact]
    public async Task Registrer_FilSomHeterJpgMenIkkeErEtBildeAvvises()
    {
        // Et program (eller noe annet) som har fått navnet "bilde.jpg"
        var falskBilde = TestHjelpere.LagBilde("bilde.jpg", ekteInnhold: false);

        var resultat = await _controller.Registrer(GyldigSkjema(falskBilde));

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("«bilde.jpg» er ikke et gyldig JPG-bilde.", ForsteBildefeil());
    }

    [Fact]
    public async Task Registrer_ForMangeBilderGirFeilmelding()
    {
        var bilder = Enumerable.Range(1, 6).Select(i => TestHjelpere.LagBilde($"bilde{i}.png")).ToArray();

        var resultat = await _controller.Registrer(GyldigSkjema(bilder));

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("Du kan legge ved maks 5 bilder.", ForsteBildefeil());
    }

    [Fact]
    public async Task Registrer_ForStortBildeGirFeilmelding()
    {
        var stortBilde = TestHjelpere.LagBilde("stort.jpg", storrelse: 5 * 1024 * 1024 + 1);

        var resultat = await _controller.Registrer(GyldigSkjema(stortBilde));

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("«stort.jpg» er større enn 5 MB.", ForsteBildefeil());
    }

    [Fact]
    public async Task Registrer_ManglendeLengdegradGirFeilmelding()
    {
        var skjema = GyldigSkjema(TestHjelpere.LagBilde("aggregat.webp"));
        skjema.Lengdegrad = null;

        var resultat = await _controller.Registrer(skjema);

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("Du må klikke i kartet for å markere plasseringen.",
            _controller.ModelState[nameof(RessursViewModel.Breddegrad)]!.Errors[0].ErrorMessage);
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
