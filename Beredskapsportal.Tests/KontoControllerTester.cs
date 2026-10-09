using Beredskapsportal.Controllers;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester innlogging og registrering i KontoController.
/// Innloggingscookien erstattes av FalskInnlogging (se TestHjelpere.cs).
/// </summary>
public class KontoControllerTester
{
    [Fact]
    public async Task LoggInn_RiktigPassordSenderBrukerenTilOversikt()
    {
        // Arrange
        var controller = new KontoController(new InMemoryBrukerRepository());
        var innlogging = TestHjelpere.GiControllerInnlogging(controller);

        // Act: logg inn med testbrukeren
        var resultat = await controller.LoggInn(new LoggInnViewModel { Brukernavn = "test", Passord = "test" });

        // Assert: brukeren sendes videre til Oversikt og er logget inn
        var videresending = Assert.IsType<RedirectToActionResult>(resultat);
        Assert.Equal("Oversikt", videresending.ControllerName);
        Assert.Equal("test", innlogging.InnloggetBruker?.Identity?.Name);
    }

    [Fact]
    public async Task LoggInn_FeilPassordGirFeilmelding()
    {
        // Arrange
        var controller = new KontoController(new InMemoryBrukerRepository());
        var innlogging = TestHjelpere.GiControllerInnlogging(controller);

        // Act
        var resultat = await controller.LoggInn(new LoggInnViewModel { Brukernavn = "test", Passord = "feil" });

        // Assert: skjemaet vises på nytt med feilmelding, og ingen er logget inn
        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("Feil brukernavn eller passord.", controller.ModelState[string.Empty]!.Errors[0].ErrorMessage);
        Assert.Null(innlogging.InnloggetBruker);
    }

    [Fact]
    public async Task Registrer_OpptattBrukernavnGirFeilmelding()
    {
        // Arrange
        var controller = new KontoController(new InMemoryBrukerRepository());
        var innlogging = TestHjelpere.GiControllerInnlogging(controller);

        // Act: "test" er allerede tatt av testbrukeren
        var resultat = await controller.Registrer(new RegistrerBrukerViewModel { Brukernavn = "test", Passord = "passord123" });

        // Assert
        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("Brukernavnet er allerede i bruk.",
            controller.ModelState[nameof(RegistrerBrukerViewModel.Brukernavn)]!.Errors[0].ErrorMessage);
        Assert.Null(innlogging.InnloggetBruker);
    }
}
