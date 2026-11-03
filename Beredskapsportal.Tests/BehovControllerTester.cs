using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester POST-håndteringen i BehovController.Registrer: gyldige behov lagres, ugyldige avvises.
/// </summary>
public class BehovControllerTester
{
    private static BehovViewModel GyldigSkjema() => new()
    {
        Type = BehovType.Oppvarming,
        Beskrivelse = "Varmeovner til varmestue",
        GeografiskOmrade = "Søm",
        Prioritet = Prioritet.Hoy,
        Kontaktperson = "Kommunen",
        Telefon = "38070000",
        Breddegrad = 58.147,
        Lengdegrad = 8.07
    };

    private static BehovController LagController(InMemoryBehovRepository repository)
    {
        var ressurser = new InMemoryRessursRepository();
        var tildelinger = new InMemoryTildelingRepository();
        return new BehovController(repository, ressurser, tildelinger,
            TestHjelpere.LagTildelingTjeneste(repository, ressurser, tildelinger));
    }

    [Fact]
    public void Registrer_GyldigSkjemaLagrerBehovet()
    {
        // Arrange
        var repository = new InMemoryBehovRepository();
        var controller = LagController(repository);

        // Act
        var resultat = controller.Registrer(GyldigSkjema());

        // Assert: sendt tilbake til listen, og behovet ligger øverst med status Venter
        Assert.IsType<RedirectToActionResult>(resultat);
        var lagret = repository.HentSiste(1)[0];
        Assert.Equal("Søm", lagret.GeografiskOmrade);
        Assert.Equal(BehovStatus.Venter, lagret.Status);
    }

    [Fact]
    public void Registrer_UgyldigSkjemaLagrerIkkeNoe()
    {
        // Arrange: later som ASP.NET fant en valideringsfeil
        var repository = new InMemoryBehovRepository();
        var controller = LagController(repository);
        controller.ModelState.AddModelError("Beskrivelse", "Du må beskrive behovet.");
        var antallFor = repository.HentAlle().Count;

        // Act
        var resultat = controller.Registrer(GyldigSkjema());

        // Assert: skjemaet vises på nytt, og ingenting er lagret
        Assert.IsType<ViewResult>(resultat);
        Assert.Equal(antallFor, repository.HentAlle().Count);
    }

    [Fact]
    public void Registrer_TomtSkjemaViserFeilmeldingerOgLagrerIkkeNoe()
    {
        // Arrange: kjører de ekte valideringsreglene på et tomt skjema, slik ASP.NET gjør før controlleren
        var repository = new InMemoryBehovRepository();
        var controller = LagController(repository);
        var skjema = new BehovViewModel();
        foreach (var feil in TestHjelpere.Valider(skjema))
        {
            controller.ModelState.AddModelError(feil.MemberNames.FirstOrDefault() ?? string.Empty, feil.ErrorMessage!);
        }
        var antallFor = repository.HentAlle().Count;

        // Act
        var resultat = controller.Registrer(skjema);

        // Assert
        Assert.IsType<ViewResult>(resultat);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(antallFor, repository.HentAlle().Count);
    }

    [Fact]
    public void Registrer_ManglendeLengdegradGirFeilmelding()
    {
        var repository = new InMemoryBehovRepository();
        var controller = LagController(repository);
        var skjema = GyldigSkjema();
        skjema.Lengdegrad = null;

        var resultat = controller.Registrer(skjema);

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("Du må klikke i kartet for å markere plasseringen.",
            controller.ModelState[nameof(BehovViewModel.Breddegrad)]!.Errors[0].ErrorMessage);
    }

    [Fact]
    public void Registrer_TekstLagresUendretOgBrukesIkkeSomKode()
    {
        // Tekst som ser ut som SQL eller JavaScript skal lagres akkurat som brukeren skrev den,
        // ikke fjernes eller endres. At den aldri kjøres som kode, sørger EF Core (parametere, mot
        // SQL-injeksjon) og Razor (encoding, mot XSS) for. Det er testet manuelt i sikkerhetstesten.
        var repository = new InMemoryBehovRepository();
        var controller = LagController(repository);
        var skjema = GyldigSkjema();
        skjema.Beskrivelse = "'; DROP TABLE Behov; -- <script>alert(1)</script>";

        controller.Registrer(skjema);

        Assert.Equal("'; DROP TABLE Behov; -- <script>alert(1)</script>", repository.HentSiste(1)[0].Beskrivelse);
    }
}
