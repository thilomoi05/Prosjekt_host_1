using Microsoft.AspNetCore.Identity;

namespace Beredskapsportal.Sikkerhet;

/// <summary>
/// Identity gir feilmeldinger på engelsk (f.eks. "Passwords must have at least one digit").
/// Denne klassen bytter dem ut med norske, slik at de passer med resten av portalen.
/// Registreres i Program.cs med AddErrorDescriber.
/// Bare meldingene brukerne faktisk kan få ved registrering og på profilsiden er oversatt.
/// </summary>
public class NorskeIdentityFeilmeldinger : IdentityErrorDescriber
{
    public override IdentityError DuplicateUserName(string brukernavn) =>
        new() { Code = nameof(DuplicateUserName), Description = $"Brukernavnet «{brukernavn}» er allerede i bruk." };

    public override IdentityError DuplicateEmail(string epost) =>
        new() { Code = nameof(DuplicateEmail), Description = $"E-posten «{epost}» er allerede registrert." };

    public override IdentityError InvalidUserName(string? brukernavn) =>
        new() { Code = nameof(InvalidUserName), Description = "Brukernavnet kan bare inneholde bokstaver, tall og tegnene - . _ @ +, og ingen mellomrom." };

    public override IdentityError InvalidEmail(string? epost) =>
        new() { Code = nameof(InvalidEmail), Description = "E-posten ser ikke gyldig ut." };

    public override IdentityError PasswordTooShort(int lengde) =>
        new() { Code = nameof(PasswordTooShort), Description = $"Passordet må være på minst {lengde} tegn." };

    public override IdentityError PasswordRequiresDigit() =>
        new() { Code = nameof(PasswordRequiresDigit), Description = "Passordet må inneholde minst ett tall." };

    public override IdentityError PasswordRequiresLower() =>
        new() { Code = nameof(PasswordRequiresLower), Description = "Passordet må inneholde minst én liten bokstav." };

    public override IdentityError PasswordRequiresUpper() =>
        new() { Code = nameof(PasswordRequiresUpper), Description = "Passordet må inneholde minst én stor bokstav." };

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        new() { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Passordet må inneholde minst ett spesialtegn." };

    // Feil nåværende passord når brukeren endrer passordet sitt (Min profil → Endre passord).
    public override IdentityError PasswordMismatch() =>
        new() { Code = nameof(PasswordMismatch), Description = "Nåværende passord er feil." };

    // To har endret samme bruker samtidig. Kan skje fordi én enhet deler én bruker.
    public override IdentityError ConcurrencyFailure() =>
        new() { Code = nameof(ConcurrencyFailure), Description = "Profilen ble endret av noen andre samtidig. Last inn siden på nytt og prøv igjen." };

    public override IdentityError DefaultError() =>
        new() { Code = nameof(DefaultError), Description = "Noe gikk galt. Prøv igjen." };
}
