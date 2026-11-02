using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;
// Både Identity og MVC har en klasse som heter SignInResult. Her mener vi Identity sin.
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester innlogging og registrering i KontoController, som bruker ASP.NET Core Identity.
/// UserManager og SignInManager er falske (se IdentitetHjelpere.cs), så testene
/// bestemmer selv om passordet "stemmer".
/// </summary>
public class KontoControllerTester
{
    private readonly UserManager<Bruker> _brukerManager = IdentitetHjelpere.LagBrukerManager();
    private readonly SignInManager<Bruker> _innloggingManager;
    private readonly KontoController _controller;

    private readonly Bruker _testbruker = new() { UserName = "test", FulltNavn = "Test Testesen" };

    public KontoControllerTester()
    {
        _innloggingManager = IdentitetHjelpere.LagInnloggingManager(_brukerManager);
        _controller = new KontoController(_brukerManager, _innloggingManager)
        {
            // TempData brukes til meldingen etter registrering som offentlig aktør.
            TempData = new TempDataDictionary(new DefaultHttpContext(), Substitute.For<ITempDataProvider>())
        };
    }

    /// <summary>Bestemmer hva passordsjekken svarer for testbrukeren.</summary>
    private void PassordSjekkGir(SignInResult resultat)
    {
        _brukerManager.FindByNameAsync("test").Returns(_testbruker);
        _innloggingManager.CheckPasswordSignInAsync(_testbruker, Arg.Any<string>(), Arg.Is(true)).Returns(resultat);
    }

    private string ForsteFeilmelding() => _controller.ModelState[string.Empty]!.Errors[0].ErrorMessage;

    private static RegistrerBrukerViewModel Registrering(BrukerRolle rolle) => new()
    {
        FulltNavn = "Kari Nordmann",
        Epost = "kari@example.com",
        Brukernavn = "kari",
        Passord = "Passord123",
        BekreftPassord = "Passord123",
        Rolle = rolle
    };

    [Fact]
    public void LoggInn_Get_LeggerReturnUrlISkjemaet()
    {
        var resultat = _controller.LoggInn("/Kart");

        var modell = Assert.IsType<LoggInnViewModel>(Assert.IsType<ViewResult>(resultat).Model);
        Assert.Equal("/Kart", modell.ReturnUrl);
    }

    [Fact]
    public async Task LoggInn_RiktigPassordSenderBrukerenTilOversikt()
    {
        PassordSjekkGir(SignInResult.Success);

        var resultat = await _controller.LoggInn(new LoggInnViewModel { Brukernavn = "test", Passord = "Riktig123" });

        var videresending = Assert.IsType<RedirectToActionResult>(resultat);
        Assert.Equal("Oversikt", videresending.ControllerName);
        await _innloggingManager.Received(1).SignInAsync(_testbruker, Arg.Is(false), Arg.Any<string?>());
    }

    [Fact]
    public async Task LoggInn_FeilPassordGirFeilmelding()
    {
        PassordSjekkGir(SignInResult.Failed);

        var resultat = await _controller.LoggInn(new LoggInnViewModel { Brukernavn = "test", Passord = "feil" });

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("Feil brukernavn eller passord.", ForsteFeilmelding());
        await _innloggingManager.DidNotReceive().SignInAsync(Arg.Any<Bruker>(), Arg.Any<bool>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task LoggInn_UkjentBrukernavnGirSammeFeilmeldingSomFeilPassord()
    {
        // Ingen bruker heter "finnesikke". Meldingen skal ikke avsløre det.
        _brukerManager.FindByNameAsync("finnesikke").Returns((Bruker?)null);

        var resultat = await _controller.LoggInn(new LoggInnViewModel { Brukernavn = "finnesikke", Passord = "hva-som-helst" });

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("Feil brukernavn eller passord.", ForsteFeilmelding());
    }

    [Fact]
    public async Task LoggInn_LastKontoGirEgenMelding()
    {
        PassordSjekkGir(SignInResult.LockedOut);

        var resultat = await _controller.LoggInn(new LoggInnViewModel { Brukernavn = "test", Passord = "feil" });

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("For mange feil forsøk. Kontoen er låst i 5 minutter.", ForsteFeilmelding());
    }

    [Fact]
    public async Task LoggInn_OffentligAktorSomIkkeErGodkjentSlippesIkkeInn()
    {
        _testbruker.VenterPaGodkjenning = true;
        PassordSjekkGir(SignInResult.Success);

        var resultat = await _controller.LoggInn(new LoggInnViewModel { Brukernavn = "test", Passord = "Riktig123" });

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("Kontoen din venter på godkjenning fra en administrator.", ForsteFeilmelding());
        await _innloggingManager.DidNotReceive().SignInAsync(Arg.Any<Bruker>(), Arg.Any<bool>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Registrer_PrivatAktorFarRollenOgLoggesInn()
    {
        _brukerManager.CreateAsync(Arg.Any<Bruker>(), "Passord123").Returns(IdentityResult.Success);

        var resultat = await _controller.Registrer(Registrering(BrukerRolle.PrivatAktor));

        var videresending = Assert.IsType<RedirectToActionResult>(resultat);
        Assert.Equal("Oversikt", videresending.ControllerName);
        await _brukerManager.Received(1).AddToRoleAsync(Arg.Is<Bruker>(b => b.UserName == "kari"), Roller.PrivatAktor);
        await _innloggingManager.Received(1).SignInAsync(Arg.Any<Bruker>(), Arg.Is(false), Arg.Any<string?>());
    }

    [Fact]
    public async Task Registrer_OffentligAktorMaGodkjennesForInnlogging()
    {
        _brukerManager.CreateAsync(Arg.Any<Bruker>(), "Passord123").Returns(IdentityResult.Success);

        var resultat = await _controller.Registrer(Registrering(BrukerRolle.OffentligAktor));

        // Brukeren lagres som "venter på godkjenning", får ingen rolle og logges ikke inn.
        var videresending = Assert.IsType<RedirectToActionResult>(resultat);
        Assert.Equal("Home", videresending.ControllerName);
        await _brukerManager.Received(1).CreateAsync(Arg.Is<Bruker>(b => b.VenterPaGodkjenning), "Passord123");
        await _brukerManager.DidNotReceive().AddToRoleAsync(Arg.Any<Bruker>(), Arg.Any<string>());
        await _innloggingManager.DidNotReceive().SignInAsync(Arg.Any<Bruker>(), Arg.Any<bool>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task Registrer_UgyldigRolleAvvises()
    {
        // Et tall som ikke er en rolle, f.eks. sendt inn med vilje for å prøve å bli Admin.
        var resultat = await _controller.Registrer(Registrering((BrukerRolle)99));

        Assert.IsType<ViewResult>(resultat);
        await _brukerManager.DidNotReceive().CreateAsync(Arg.Any<Bruker>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Registrer_FeilFraIdentityVisesISkjemaet()
    {
        var feil = new IdentityError { Code = "DuplicateUserName", Description = "Brukernavnet «kari» er allerede i bruk." };
        _brukerManager.CreateAsync(Arg.Any<Bruker>(), "Passord123").Returns(IdentityResult.Failed(feil));

        var resultat = await _controller.Registrer(Registrering(BrukerRolle.PrivatAktor));

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("Brukernavnet «kari» er allerede i bruk.", ForsteFeilmelding());
    }
}
