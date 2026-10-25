namespace Beredskapsportal.Models;

/// <summary>
/// En registrert bruker av BeredskapsPortalen.
/// Passord lagres midlertidig i klartekst til vi får implementert ASP.NET Core Identity, som har trygg passord-hashing innebygd.
/// </summary>
public class Bruker
{
    public int Id { get; set; }

    public string FulltNavn { get; set; } = string.Empty;

    public string Epost { get; set; } = string.Empty;

    public string Brukernavn { get; set; } = string.Empty;

    // MIDLERTIDIG: passordet lagres i klartekst fram til innloggingen flyttes til
    // ASP.NET Core Identity, som har trygg passord-hashing innebygd.
    public string Passord { get; set; } = string.Empty;

    public BrukerRolle Rolle { get; set; }
}
