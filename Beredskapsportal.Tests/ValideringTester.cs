using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester valideringsreglene i skjemaene ([Required], [Phone], [EmailAddress], [StringLength]).
/// TestHjelpere.Valider kjører de samme reglene som ASP.NET kjører før controlleren får
/// skjemaet, så feilmeldingene her er de samme som brukeren ser under feltene.
/// </summary>
public class ValideringTester
{
    private static BehovViewModel GyldigBehov() => new()
    {
        Type = BehovType.Nodstrom,
        Beskrivelse = "Aggregat til sykehjem",
        GeografiskOmrade = "Lund",
        Prioritet = Prioritet.Hoy,
        Kontaktperson = "Kommunen",
        Telefon = "38070000",
        Breddegrad = 58.15,
        Lengdegrad = 8.0
    };

    private static RessursViewModel GyldigRessurs() => new()
    {
        Type = RessursType.Aggregat,
        BeskrivelseAvKapasitet = "20 kVA",
        Adresse = "Kirkegata 5, Kristiansand",
        Tilgangsbeskrivelse = "Står i garasjen",
        Kontaktperson = "Ola",
        Telefon = "40000000",
        Breddegrad = 58.15,
        Lengdegrad = 8.0
    };

    private static List<string> Feilmeldinger(object modell) =>
        TestHjelpere.Valider(modell).Select(f => f.ErrorMessage!).ToList();

    // ---------- Behov ----------

    [Fact]
    public void Behov_GyldigSkjemaHarIngenFeil()
    {
        Assert.Empty(Feilmeldinger(GyldigBehov()));
    }

    [Fact]
    public void Behov_TomtSkjemaGirFeilForAllePakrevdeFelt()
    {
        var feil = Feilmeldinger(new BehovViewModel());

        Assert.Contains("Du må velge type behov.", feil);
        Assert.Contains("Du må beskrive behovet.", feil);
        Assert.Contains("Du må angi geografisk område.", feil);
        Assert.Contains("Du må oppgi en kontaktperson.", feil);
        Assert.Contains("Du må oppgi et telefonnummer.", feil);
        Assert.Contains("Du må klikke i kartet for å markere plasseringen.", feil);
        // Plasseringsfeilen skal bare komme én gang (feil funnet i testingen i Oppgave 1).
        Assert.Single(feil, f => f == "Du må klikke i kartet for å markere plasseringen.");
    }

    [Theory]
    [InlineData("ikke et nummer")]
    [InlineData("abc123")]
    public void Behov_UgyldigTelefonnummerGirFeil(string telefon)
    {
        var skjema = GyldigBehov();
        skjema.Telefon = telefon;

        Assert.Contains("Telefonnummeret ser ikke gyldig ut.", Feilmeldinger(skjema));
    }

    [Fact]
    public void Behov_ForLangBeskrivelseGirFeil()
    {
        var skjema = GyldigBehov();
        skjema.Beskrivelse = new string('a', 2001);

        Assert.Contains("Beskrivelsen kan være på maks 2000 tegn.", Feilmeldinger(skjema));
    }

    // ---------- Ressurs ----------

    [Fact]
    public void Ressurs_GyldigSkjemaHarIngenFeil()
    {
        Assert.Empty(Feilmeldinger(GyldigRessurs()));
    }

    [Fact]
    public void Ressurs_TomtSkjemaGirFeilForAllePakrevdeFelt()
    {
        var feil = Feilmeldinger(new RessursViewModel());

        Assert.Contains("Du må velge type ressurs.", feil);
        Assert.Contains("Du må beskrive kapasiteten.", feil);
        Assert.Contains("Du må skrive inn adressen der ressursen er.", feil);
        Assert.Contains("Du må beskrive hvordan man får tak i ressursen.", feil);
        Assert.Contains("Du må oppgi en kontaktperson.", feil);
        Assert.Contains("Du må oppgi et telefonnummer.", feil);
        Assert.Contains("Du må klikke i kartet for å markere plasseringen.", feil);
    }

    [Fact]
    public void Ressurs_ForLangtTelefonnummerGirFeil()
    {
        // Kolonnen Telefon i databasen har plass til 30 tegn. Uten denne regelen
        // ville et lengre nummer gitt en feil fra databasen (500) i stedet for en melding.
        var skjema = GyldigRessurs();
        skjema.Telefon = new string('4', 31);

        Assert.Contains("Telefonnummeret kan være på maks 30 tegn.", Feilmeldinger(skjema));
    }

    [Fact]
    public void Ressurs_ForLangAdresseGirFeil()
    {
        var skjema = GyldigRessurs();
        skjema.Adresse = new string('a', 301);

        Assert.Contains("Adressen kan være på maks 300 tegn.", Feilmeldinger(skjema));
    }

    // ---------- Innlogging og registrering ----------

    [Fact]
    public void LoggInn_TomtSkjemaGirFeilForBeggeFelt()
    {
        var feil = Feilmeldinger(new LoggInnViewModel());

        Assert.Contains("Du må skrive inn brukernavn.", feil);
        Assert.Contains("Du må skrive inn passord.", feil);
    }

    [Fact]
    public void Registrering_UgyldigEpostGirFeil()
    {
        var skjema = new RegistrerBrukerViewModel
        {
            FulltNavn = "Kari Nordmann",
            Epost = "ikke-en-epost",
            Brukernavn = "kari",
            Passord = "Passord123",
            BekreftPassord = "Passord123"
        };

        Assert.Contains("E-posten ser ikke gyldig ut.", Feilmeldinger(skjema));
    }

    [Fact]
    public void Registrering_ForLangtBrukernavnGirFeil()
    {
        // Brukernavnet lagres i RegistrertAv på behov og ressurser, som har plass til 100 tegn.
        var skjema = new RegistrerBrukerViewModel
        {
            FulltNavn = "Kari Nordmann",
            Epost = "kari@example.no",
            Brukernavn = new string('k', 101),
            Passord = "Passord123",
            BekreftPassord = "Passord123"
        };

        Assert.Contains("Brukernavnet kan være på maks 100 tegn.", Feilmeldinger(skjema));
    }
}
