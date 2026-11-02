using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester at forsiden og registreringsskjemaet vises riktig.
/// </summary>
public class ControllerTester
{
    [Fact]
    public void HomeIndex_ViserForsiden()
    {
        // Arrange
        var controller = new HomeController();

        // Act: be om forsiden
        var resultat = controller.Index();

        // Assert: det er en side (ViewResult). Forsiden har bare fast tekst, så ingen modell.
        Assert.IsType<ViewResult>(resultat);
    }

    [Fact]
    public void KontoRegistrer_ForhandsvelgerRollenFraLenken()
    {
        // Arrange
        var brukerManager = IdentitetHjelpere.LagBrukerManager();
        var controller = new KontoController(brukerManager, IdentitetHjelpere.LagInnloggingManager(brukerManager));

        // Act: samme som å åpne /Konto/Registrer?rolle=PrivatAktor
        var resultat = controller.Registrer(BrukerRolle.PrivatAktor);

        // Assert: skjemaet har Privat aktør forhåndsvalgt
        var side = Assert.IsType<ViewResult>(resultat);
        var modell = Assert.IsType<RegistrerBrukerViewModel>(side.Model);
        Assert.Equal(BrukerRolle.PrivatAktor, modell.Rolle);
    }
}

