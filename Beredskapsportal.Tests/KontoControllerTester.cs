using System.Reflection;
using System.Security.Claims;
using Beredskapsportal.Controllers;
using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;
using Beredskapsportal.Services;
using Beredskapsportal.Sikkerhet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using NSubstitute;
// Både Identity og MVC har en klasse som heter SignInResult. Her mener vi Identity sin.
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester innlogging, registrering og endring av profilen i KontoController, som bruker ASP.NET Core Identity.
/// UserManager og SignInManager er falske (se IdentitetHjelpere.cs), så testene
/// bestemmer selv om passordet "stemmer".
/// </summary>
public class KontoControllerTester
{
    private readonly UserManager<Bruker> _brukerManager = IdentitetHjelpere.LagBrukerManager();
    private readonly SignInManager<Bruker> _innloggingManager;
    private readonly KontoController _controller;

    // Adminloggen. Ekte minnebasert lagring, så testene kan sjekke hva som ble logget.
    private readonly InMemoryHendelseRepository _hendelser = new();

    private readonly Bruker _testbruker = new() { UserName = "test", FulltNavn = "Test Testesen" };

    public KontoControllerTester()
    {
        _innloggingManager = IdentitetHjelpere.LagInnloggingManager(_brukerManager);
        _controller = new KontoController(_brukerManager, _innloggingManager, _hendelser)
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
    public async Task LoggInn_RiktigPassordLoggesIAdminloggen()
    {
        PassordSjekkGir(SignInResult.Success);

        await _controller.LoggInn(new LoggInnViewModel { Brukernavn = "test", Passord = "Riktig123" });

        var hendelse = Assert.Single(_hendelser.HentSiste(10));
        Assert.Equal(Kontohendelser.LoggetInn, hendelse.Beskrivelse);
        Assert.Equal("test", hendelse.UtfortAv);
        Assert.Null(hendelse.BehovId);     // gjelder en konto, ikke et behov eller en ressurs
        Assert.Null(hendelse.RessursId);
    }

    [Fact]
    public async Task LoggInn_FeilPassordLoggesSomAdvarsel()
    {
        PassordSjekkGir(SignInResult.Failed);

        await _controller.LoggInn(new LoggInnViewModel { Brukernavn = "test", Passord = "Feil123" });

        var hendelse = Assert.Single(_hendelser.HentSiste(10));
        Assert.Equal(Kontohendelser.FeilPassord, hendelse.Beskrivelse);
        Assert.Equal("test", hendelse.UtfortAv);
        Assert.True(Kontohendelser.ErAdvarsel(hendelse.Beskrivelse));
    }

    [Fact]
    public async Task LoggInn_UkjentBrukernavnLagrerIkkeDetSomBleSkrevet()
    {
        // Noen har skrevet passordet sitt i brukernavnfeltet ved en feil. Det skal ikke havne i loggen.
        _brukerManager.FindByNameAsync("MittPassord123").Returns((Bruker?)null);

        await _controller.LoggInn(new LoggInnViewModel { Brukernavn = "MittPassord123", Passord = "x" });

        var hendelse = Assert.Single(_hendelser.HentSiste(10));
        Assert.Equal(Kontohendelser.UkjentBrukernavn, hendelse.Beskrivelse);
        Assert.Equal(Kontohendelser.UkjentBruker, hendelse.UtfortAv);
        Assert.DoesNotContain("MittPassord123", hendelse.Beskrivelse);
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

    // ---------- Profil: endre navn, e-post og passord ----------

    /// <summary>Testbrukeren er logget inn (GetUserAsync finner den ut fra cookien).</summary>
    private void TestbrukerErInnlogget()
    {
        _testbruker.Email = "test@example.com";
        _brukerManager.GetUserAsync(Arg.Any<ClaimsPrincipal>()).Returns(_testbruker);
    }

    private static EndreProfilViewModel Profilskjema(string epost, string? passord = null) => new()
    {
        FulltNavn = "Heimevernet avdeling Kristiansand",
        Epost = epost,
        NavarendePassord = passord
    };

    private string FeilmeldingForPassordfeltet() =>
        _controller.ModelState[nameof(EndreProfilViewModel.NavarendePassord)]!.Errors[0].ErrorMessage;

    [Theory]
    [InlineData(nameof(KontoController.EndreProfil))]
    [InlineData(nameof(KontoController.EndrePassord))]
    public void Profilsidene_KreverInnlogging(string metodenavn)
    {
        // Både GET og POST skal ha [Authorize].
        var metoder = typeof(KontoController).GetMethods().Where(m => m.Name == metodenavn).ToList();

        Assert.Equal(2, metoder.Count);
        Assert.All(metoder, m => Assert.NotNull(m.GetCustomAttribute<AuthorizeAttribute>()));
    }

    [Fact]
    public async Task EndreProfil_Get_FyllerInnNavnOgEpost()
    {
        TestbrukerErInnlogget();

        var resultat = await _controller.EndreProfil();

        var modell = Assert.IsType<EndreProfilViewModel>(Assert.IsType<ViewResult>(resultat).Model);
        Assert.Equal("Test Testesen", modell.FulltNavn);
        Assert.Equal("test@example.com", modell.Epost);
    }

    [Fact]
    public async Task EndreProfil_NyttNavnLagresUtenPassordOgCookienFornyes()
    {
        TestbrukerErInnlogget();
        _brukerManager.UpdateAsync(_testbruker).Returns(IdentityResult.Success);

        var resultat = await _controller.EndreProfil(Profilskjema("test@example.com"));

        var videresending = Assert.IsType<RedirectToActionResult>(resultat);
        Assert.Equal(nameof(KontoController.Profil), videresending.ActionName);
        Assert.Equal("Heimevernet avdeling Kristiansand", _testbruker.FulltNavn);
        await _brukerManager.DidNotReceive().SetEmailAsync(Arg.Any<Bruker>(), Arg.Any<string?>());
        // Ny cookie, så menyen viser det nye navnet med en gang.
        await _innloggingManager.Received(1).RefreshSignInAsync(_testbruker);
    }

    [Fact]
    public async Task EndreProfil_SammeEpostMedStoreBokstaverRegnesIkkeSomNy()
    {
        TestbrukerErInnlogget();
        _brukerManager.UpdateAsync(_testbruker).Returns(IdentityResult.Success);

        var resultat = await _controller.EndreProfil(Profilskjema("TEST@example.com"));

        Assert.IsType<RedirectToActionResult>(resultat);
        await _brukerManager.DidNotReceive().CheckPasswordAsync(Arg.Any<Bruker>(), Arg.Any<string>());
        await _brukerManager.DidNotReceive().SetEmailAsync(Arg.Any<Bruker>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task EndreProfil_NyEpostUtenPassordAvvises()
    {
        TestbrukerErInnlogget();

        var resultat = await _controller.EndreProfil(Profilskjema("ny@example.com"));

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("Skriv inn nåværende passord for å endre e-posten.", FeilmeldingForPassordfeltet());
        await _brukerManager.DidNotReceive().SetEmailAsync(Arg.Any<Bruker>(), Arg.Any<string?>());
        await _brukerManager.DidNotReceive().UpdateAsync(Arg.Any<Bruker>());
    }

    [Fact]
    public async Task EndreProfil_NyEpostMedFeilPassordAvvises()
    {
        TestbrukerErInnlogget();
        _brukerManager.CheckPasswordAsync(_testbruker, "feil").Returns(false);

        var resultat = await _controller.EndreProfil(Profilskjema("ny@example.com", "feil"));

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("Nåværende passord er feil.", FeilmeldingForPassordfeltet());
        await _brukerManager.DidNotReceive().SetEmailAsync(Arg.Any<Bruker>(), Arg.Any<string?>());
        await _innloggingManager.DidNotReceive().RefreshSignInAsync(Arg.Any<Bruker>());
    }

    [Fact]
    public async Task EndreProfil_NyEpostMedRiktigPassordLagres()
    {
        TestbrukerErInnlogget();
        _brukerManager.CheckPasswordAsync(_testbruker, "Riktig123").Returns(true);
        _brukerManager.SetEmailAsync(_testbruker, "ny@example.com").Returns(IdentityResult.Success);

        var resultat = await _controller.EndreProfil(Profilskjema("ny@example.com", "Riktig123"));

        Assert.IsType<RedirectToActionResult>(resultat);
        await _brukerManager.Received(1).SetEmailAsync(_testbruker, "ny@example.com");
        await _innloggingManager.Received(1).RefreshSignInAsync(_testbruker);
    }

    [Fact]
    public async Task EndreProfil_EpostSomErIBrukGirFeilmelding()
    {
        TestbrukerErInnlogget();
        _brukerManager.CheckPasswordAsync(_testbruker, "Riktig123").Returns(true);
        var feil = new NorskeIdentityFeilmeldinger().DuplicateEmail("ny@example.com");
        _brukerManager.SetEmailAsync(_testbruker, "ny@example.com").Returns(IdentityResult.Failed(feil));

        var resultat = await _controller.EndreProfil(Profilskjema("ny@example.com", "Riktig123"));

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("E-posten «ny@example.com» er allerede registrert.", ForsteFeilmelding());
        await _innloggingManager.DidNotReceive().RefreshSignInAsync(Arg.Any<Bruker>());
    }

    [Fact]
    public async Task EndrePassord_RiktigPassordLagresOgCookienFornyes()
    {
        TestbrukerErInnlogget();
        _brukerManager.ChangePasswordAsync(_testbruker, "Gammelt123", "Nytt12345").Returns(IdentityResult.Success);

        var resultat = await _controller.EndrePassord(new EndrePassordViewModel
        {
            NavarendePassord = "Gammelt123",
            NyttPassord = "Nytt12345",
            BekreftNyttPassord = "Nytt12345"
        });

        var videresending = Assert.IsType<RedirectToActionResult>(resultat);
        Assert.Equal(nameof(KontoController.Profil), videresending.ActionName);
        // Uten ny cookie ville brukeren blitt logget ut, fordi passordbyttet gir nytt sikkerhetsstempel.
        await _innloggingManager.Received(1).RefreshSignInAsync(_testbruker);
    }

    [Fact]
    public async Task EndrePassord_FeilNavarendePassordGirFeilmelding()
    {
        TestbrukerErInnlogget();
        var feil = new NorskeIdentityFeilmeldinger().PasswordMismatch();
        _brukerManager.ChangePasswordAsync(_testbruker, "feil", "Nytt12345").Returns(IdentityResult.Failed(feil));

        var resultat = await _controller.EndrePassord(new EndrePassordViewModel
        {
            NavarendePassord = "feil",
            NyttPassord = "Nytt12345",
            BekreftNyttPassord = "Nytt12345"
        });

        Assert.IsType<ViewResult>(resultat);
        Assert.Equal("Nåværende passord er feil.", ForsteFeilmelding());
        await _innloggingManager.DidNotReceive().RefreshSignInAsync(Arg.Any<Bruker>());
    }
}
