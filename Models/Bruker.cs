using Microsoft.AspNetCore.Identity;

namespace Beredskapsportal.Models;

/// <summary>
/// En registrert bruker av Beredt.
///
/// Arver fra IdentityUser (ASP.NET Core Identity). Derfra får brukeren blant annet:
///  - UserName og Email (brukernavn og e-post)
///  - PasswordHash: passordet lagres aldri, bare en hash laget med PBKDF2 og et tilfeldig salt
///  - AccessFailedCount og LockoutEnd: brukes til å låse kontoen etter for mange feil passord
/// Rollene (OffentligAktor, PrivatAktor, Admin) ligger i Identity sine egne tabeller
/// (AspNetRoles og AspNetUserRoles), ikke her. Se Roller.cs.
/// Her legger vi bare til det som er vårt eget.
/// </summary>
public class Bruker : IdentityUser
{
    // Vises i menyen og på forsiden ("Velkommen tilbake, Test").
    public string FulltNavn { get; set; } = string.Empty;

    // True for offentlige aktører som har registrert seg selv, men ikke er godkjent ennå.
    // De kan ikke logge inn før en administrator har godkjent dem (kommer sammen med
    // tildelingssiden for Admin). Hindrer at hvem som helst kan registrere seg som
    // offentlig aktør og se private ressurser.
    public bool VenterPaGodkjenning { get; set; }
}
