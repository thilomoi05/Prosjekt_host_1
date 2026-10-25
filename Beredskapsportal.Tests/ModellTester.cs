using Beredskapsportal.Models;
using Beredskapsportal.Models.ViewModeller;

namespace Beredskapsportal.Tests;

/// <summary>
/// Tester valideringsreglene i registreringsskjemaet og visningsnavnet til behov.
/// Valideringsreglene ([Required] osv.) kjøres av ASP.NET før controlleren,
/// så her sjekker vi at reglene i seg selv er riktige.
/// </summary>
public class ModellTester
{
    private static RegistrerBrukerViewModel GyldigRegistrering() => new()
    {
        FulltNavn = "Kari Nordmann",
        Epost = "kari@example.com",
        Brukernavn = "kari",
        Passord = "passord123",
        BekreftPassord = "passord123"
    };

    [Fact]
    public void Registrering_PassordeneMaVaereLike()
    {
        // Arrange
        var modell = GyldigRegistrering();
        modell.BekreftPassord = "noe-annet";

        // Act
        var feil = TestHjelpere.Valider(modell);

        // Assert
        Assert.Contains(feil, f => f.ErrorMessage == "Passordene er ikke like.");
    }

    [Fact]
    public void Registrering_PassordetMaHaMinst6Tegn()
    {
        var modell = GyldigRegistrering();
        modell.Passord = "12345";
        modell.BekreftPassord = "12345";

        var feil = TestHjelpere.Valider(modell);

        Assert.Contains(feil, f => f.ErrorMessage == "Passordet må være på minst 6 tegn.");
    }

    [Fact]
    public void VisningsNavn_GirNorskTekstForBehovstypen()
    {
        var behov = new Behov { Type = BehovType.Nodstrom };

        Assert.Equal("Nødstrøm (aggregat)", behov.VisningsNavn);
    }
}
