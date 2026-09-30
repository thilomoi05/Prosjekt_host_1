using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Microsoft.AspNetCore.Mvc;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester at controllerne sender riktige data til viewene etter endringene på forsiden.
/// </summary>
public class ControllerTester
{
    [Fact]
    public void HomeIndex_SenderRiktigeTallTilForsiden()
    {
        // Arrange: lag controlleren med ekte repositoryer, slik Program.cs gjør
        var behovRepository = new InMemoryBehovRepository();
        var ressursRepository = new InMemoryRessursRepository();
        var controller = new HomeController(behovRepository, ressursRepository);

        // Act: be om forsiden
        var resultat = controller.Index();

        // Assert: det er en side (ViewResult), og pakken har riktige tall
        var side = Assert.IsType<ViewResult>(resultat);
        var modell = Assert.IsType<ForsideViewModel>(side.Model);
        Assert.Equal(behovRepository.TellAktive(), modell.AntallAktiveBehov);
        Assert.Equal(behovRepository.TellAkutte(), modell.AntallAkutteBehov);
        Assert.Equal(ressursRepository.TellTilgjengelige(), modell.AntallTilgjengeligeRessurser);
    }

    [Fact]
    public void KontoRegistrer_ForhandsvelgerRollenFraLenken()
    {
        // Arrange
        var controller = new KontoController(new InMemoryBrukerRepository());

        // Act: samme som å åpne /Konto/Registrer?rolle=PrivatAktor
        var resultat = controller.Registrer(BrukerRolle.PrivatAktor);

        // Assert: skjemaet har Privat aktør forhåndsvalgt
        var side = Assert.IsType<ViewResult>(resultat);
        var modell = Assert.IsType<RegistrerBrukerViewModel>(side.Model);
        Assert.Equal(BrukerRolle.PrivatAktor, modell.Rolle);
    }
}

