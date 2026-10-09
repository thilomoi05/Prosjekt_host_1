using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester at BehovController lagrer gyldige behov og avviser ugyldige.
/// </summary>
public class BehovControllerTester
{
    private static BehovViewModel GyldigSkjema() => new()
    {
        Type = BehovType.Oppvarming,
        Beskrivelse = "Varmeovner til varmestue",
        GeografiskOmrade = "Søm",
        Prioritet = Prioritet.Akutt,
        Kontaktperson = "Kommunen",
        Telefon = "38070000",
        Breddegrad = 58.147,
        Lengdegrad = 8.07
    };

    [Fact]
    public void Registrer_GyldigSkjemaLagrerBehovet()
    {
        // Arrange
        var repository = new InMemoryBehovRepository();
        var controller = new BehovController(repository);

        // Act
        var resultat = controller.Registrer(GyldigSkjema());

        // Assert: sendt tilbake til listen, og behovet ligger øverst med status Ny
        Assert.IsType<RedirectToActionResult>(resultat);
        var lagret = repository.HentSiste(1)[0];
        Assert.Equal("Søm", lagret.GeografiskOmrade);
        Assert.Equal(BehovStatus.Ny, lagret.Status);
    }

    [Fact]
    public void Registrer_UgyldigSkjemaLagrerIkkeNoe()
    {
        // Arrange: later som ASP.NET fant en valideringsfeil
        var repository = new InMemoryBehovRepository();
        var controller = new BehovController(repository);
        controller.ModelState.AddModelError("Beskrivelse", "Du må beskrive behovet.");
        var antallFor = repository.HentAlle().Count;

        // Act
        var resultat = controller.Registrer(GyldigSkjema());

        // Assert: skjemaet vises på nytt, og ingenting er lagret
        Assert.IsType<ViewResult>(resultat);
        Assert.Equal(antallFor, repository.HentAlle().Count);
    }
}
